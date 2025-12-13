namespace StoreSimulator.UI.ViewModels

open System
open System.Collections.ObjectModel
open System.Windows.Input

open Catalog
open Cart
open CartJson
open BackupManager
open PriceCalculator

// Product view model with SelectedQty for the UI (now with mutable Stock and IsSoldOut)
type ProductVm(p: Product) =
    inherit ViewModelBase()
    let mutable stockLocal = p.Stock
    member val Id = p.Id
    member val Name = p.Name
    member val Category = p.Category
    member val Price = p.Price
    member val SelectedQty = 1 with get, set

    member this.Stock
        with get() = stockLocal
        and set(v) =
            stockLocal <- v
            this.RaisePropertyChanged("Stock")
            this.RaisePropertyChanged("IsSoldOut")

    member this.IsSoldOut
        with get() = stockLocal <= 0

// CartItem view model (now with IsSoldOut and ItemErrorMessage)
type CartItemVm(item: CartItem, remainingStock:int) =
    inherit ViewModelBase()
    let mutable qty = item.Quantity
    let mutable errorLocal = ""
    let mutable soldOutLocal = (remainingStock <= 0)

    member val ProductId = item.ProductId
    member val ProductName = item.ProductName
    member val UnitPrice = item.UnitPrice

    member this.Quantity
        with get() = qty
        and set(v) =
            qty <- v
            this.RaisePropertyChanged("Quantity")
            this.RaisePropertyChanged("LineTotal")

    member this.LineTotal
        with get() = this.UnitPrice * decimal this.Quantity

    member this.IsSoldOut
        with get() = soldOutLocal
        and set(v) =
            soldOutLocal <- v
            this.RaisePropertyChanged("IsSoldOut")

    member this.ItemErrorMessage
        with get() = errorLocal
        and set(v) =
            errorLocal <- v
            this.RaisePropertyChanged("ItemErrorMessage")

// MainWindowViewModel
type MainWindowViewModel() as this =
    inherit ViewModelBase()

    // -----------------------
    // local let-bindings
    // -----------------------
    let products = ObservableCollection<ProductVm>()
    let filtered = ObservableCollection<ProductVm>()
    let cartItems = ObservableCollection<CartItemVm>()
    let mutable cartState : Cart = Cart.empty

    let mutable searchTextLocal = ""
    let mutable selectedCategoryLocal = "All"

    // discount locals
    let mutable discountPercentageLocal = 0M
    let mutable discountAmountLocal = 0M
    let mutable discountVisibleLocal = false

    // (optional) retained top-level error if you still want it (not used for per-item errors)
    let mutable errorMessageLocal = ""

    let categoriesLocal = ObservableCollection<string>()

    // -----------------------
    // ctor (do block)
    // -----------------------
    do
        let catalog = loadCatalog()

        getAllProducts catalog
        |> List.iter (fun p -> products.Add(ProductVm(p)))

        let cats =
            products
            |> Seq.map (fun p -> p.Category)
            |> Seq.distinct
            |> Seq.sort
            |> Seq.toList

        categoriesLocal.Clear()
        categoriesLocal.Add("All")
        cats |> List.iter categoriesLocal.Add

        // initial filtered (copy all)
        products |> Seq.iter filtered.Add

        // initialize discount and error values safely
        discountPercentageLocal <- 0M
        discountAmountLocal <- 0M
        discountVisibleLocal <- false
        errorMessageLocal <- ""

    // -----------------------
    // properties
    // -----------------------
    member val SelectedCartItem : CartItemVm option = None with get, set

    member _.Catalog = products
    member _.FilteredProducts = filtered
    member _.CartItems = cartItems
    member _.Categories = categoriesLocal

    member this.SearchText
        with get() = searchTextLocal
        and set(v) =
            searchTextLocal <- v
            this.RaisePropertyChanged("SearchText")

    member this.SelectedCategory
        with get() = selectedCategoryLocal
        and set(v) =
            selectedCategoryLocal <- v
            this.RaisePropertyChanged("SelectedCategory")

    // Top-level ErrorMessage (kept for compatibility; per-item errors are used for cart items)
    member _.ErrorMessage
        with get() = errorMessageLocal
        and set(v) =
            errorMessageLocal <- v
            this.RaisePropertyChanged("ErrorMessage")

    // -----------------------
    // filter
    // -----------------------
    member private this.RefreshFilter() =
        let q =
            if isNull this.SearchText then ""
            else this.SearchText.Trim().ToLowerInvariant()

        let cat = if isNull this.SelectedCategory then "All" else this.SelectedCategory

        filtered.Clear()

        products
        |> Seq.filter (fun p ->
            let matchesCat = (cat = "All") || (String.Equals(p.Category, cat, StringComparison.OrdinalIgnoreCase))
            let matchesSearch = (String.IsNullOrWhiteSpace(q)) || p.Name.ToLowerInvariant().Contains(q)
            matchesCat && matchesSearch)
        |> Seq.iter filtered.Add

        this.RaisePropertyChanged("FilteredProducts")

    // -----------------------
    // helper: update productvm stock according to catalog and cartState
    // -----------------------
    member private this.UpdateProductVmStocks() =
        // reload catalog (the source of truth for base stock)
        let catalog = loadCatalog()
        // for each productvm compute remaining = baseStock - qtyInCart
        products |> Seq.iter (fun pvm ->
            let baseStock =
                match Map.tryFind pvm.Id catalog with
                | Some prod -> prod.Stock
                | None -> pvm.Stock
            let qtyInCart =
                match Map.tryFind pvm.Id cartState with
                | Some ci -> ci.Quantity
                | None -> 0
            let remaining = baseStock - qtyInCart
            // ensure remaining not negative
            let remainingSafe = if remaining < 0 then 0 else remaining
            pvm.Stock <- remainingSafe
        )

    // -----------------------
    // SyncCart
    // -----------------------
    member private this.SyncCart() =
        cartItems.Clear()

        let catalog = loadCatalog()

        // compute remaining for each product (based on catalog stock minus qty in cart)
        let stockMap =
            products
            |> Seq.map (fun pvm ->
                let baseStock =
                    match Map.tryFind pvm.Id catalog with
                    | Some pr -> pr.Stock
                    | None -> pvm.Stock
                let qtyInCart =
                    match Map.tryFind pvm.Id cartState with
                    | Some it -> it.Quantity
                    | None -> 0
                let remaining = max 0 (baseStock - qtyInCart)
                // update productvm stock shown in catalog
                pvm.Stock <- remaining
                (pvm.Id, remaining)
            )
            |> Map.ofSeq

        // create CartItemVm with remaining stock info
        cartState
        |> toList
        |> List.iter (fun ci ->
            let remaining =
                match Map.tryFind ci.ProductId stockMap with
                | Some r -> r
                | None -> 0
            cartItems.Add(CartItemVm(ci, remaining))
        )

        // compute discount totals
        let (subTotal, perc, amount, finalTotal) = calculateCheckoutTotalWithDetails cartState
        discountPercentageLocal <- perc
        discountAmountLocal <- amount
        discountVisibleLocal <- (perc > 0M)

        // clear top-level error (per-item errors remain)
        this.ErrorMessage <- ""

        this.RaisePropertyChanged("Total")
        this.RaisePropertyChanged("DiscountPercentage")
        this.RaisePropertyChanged("DiscountAmount")
        this.RaisePropertyChanged("DiscountVisible")

    // totals
    member _.Total
        with get() =
            let (_, _, _, final) = calculateCheckoutTotalWithDetails cartState
            final

    member _.DiscountPercentage = discountPercentageLocal
    member _.DiscountAmount = discountAmountLocal
    member _.DiscountVisible = discountVisibleLocal

    // find product
    member private this.TryFindProductVm(id:int) =
        products |> Seq.tryFind (fun p -> p.Id = id)

    // helper to set per-item error message if that item exists in cartItems
    member private this.SetCartItemError (productId:int) (msg:string) =
        cartItems
        |> Seq.tryFind (fun ci -> ci.ProductId = productId)
        |> Option.iter (fun ci -> ci.ItemErrorMessage <- msg)

    // -----------------------
    // Commands
    // -----------------------
    member _.SearchCommand =
        DelegateCommand(fun _ -> this.RefreshFilter()) :> ICommand

    member _.ClearSearchCommand =
        DelegateCommand(fun _ ->
            this.SearchText <- ""
            this.SelectedCategory <- "All"
            this.RefreshFilter()) :> ICommand

    member _.AddToCartCommand =
        DelegateCommand(fun param ->
            match param with
            | :? int as pid ->
                match this.TryFindProductVm(pid) with
                | Some pvm ->
                    let qty = if pvm.SelectedQty < 1 then 1 else pvm.SelectedQty
                    let catalog = loadCatalog()
                    // addItem returns Result<Cart,string>
                    match addItem catalog pid qty cartState with
                    | Ok newCart ->
                        cartState <- newCart
                        this.SyncCart()
                    | Error err ->
                        // set error on the cart item (if present) so it appears near "Sold out"
                        this.SetCartItemError pid err
                | None -> ()
            | _ -> ()) :> ICommand

    member _.RemoveFromCartCommand =
        DelegateCommand(fun param ->
            match param with
            | :? int as pid ->
                cartState <- removeFromCart pid cartState
                this.SyncCart()
            | _ -> ()) :> ICommand

    member _.IncreaseQtyCommand =
        DelegateCommand(fun param ->
            match param with
            | :? int as pid ->
                let current =
                    match Map.tryFind pid cartState with
                    | Some it -> it.Quantity
                    | None -> 0
                let catalog = loadCatalog()
                match updateQuantity catalog pid (current + 1) cartState with
                | Ok newCart ->
                    cartState <- newCart
                    this.SyncCart()
                | Error err ->
                    // attach per-item error message
                    this.SetCartItemError pid err
            | _ -> ()) :> ICommand

    member _.DecreaseQtyCommand =
        DelegateCommand(fun param ->
            match param with
            | :? int as pid ->
                let current =
                    match Map.tryFind pid cartState with
                    | Some it -> it.Quantity
                    | None -> 0
                let catalog = loadCatalog()
                match updateQuantity catalog pid (current - 1) cartState with
                | Ok newCart ->
                    cartState <- newCart
                    this.SyncCart()
                | Error err ->
                    this.SetCartItemError pid err
            | _ -> ()) :> ICommand

    member _.ClearCartCommand =
        DelegateCommand(fun _ ->
            cartState <- Cart.empty
            this.SyncCart()) :> ICommand

    member _.SaveCartCommand =
        DelegateCommand(fun _ ->
            saveCartToFile "cart.json" cartState) :> ICommand

    member _.LoadCartCommand =
        DelegateCommand(fun _ ->
            if cartFileExists "cart.json" then
                cartState <- loadCartFromFile "cart.json"
                this.SyncCart()
            else
                this.ErrorMessage <- "No cart.json found") :> ICommand

    member _.BackupCommand =
        DelegateCommand(fun _ -> createBackup "cart.json" |> ignore) :> ICommand

    member _.RestoreBackupCommand =
        DelegateCommand(fun _ ->
            match tryGetLatestBackup() with
            | Some latest ->
                restoreFromBackup latest "cart.json"
                cartState <- loadCartFromFile "cart.json"
                this.SyncCart()
            | None -> this.ErrorMessage <- "No backups found") :> ICommand
