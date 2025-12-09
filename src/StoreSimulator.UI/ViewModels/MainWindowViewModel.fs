namespace StoreSimulator.UI.ViewModels

open System
open System.Collections.ObjectModel
open System.Windows.Input

open Catalog
open Cart
open CartJson
open BackupManager

// Product view model with SelectedQty for the UI
type ProductVm(p: Product) =
    inherit ViewModelBase()
    member val Id = p.Id
    member val Name = p.Name
    member val Category = p.Category
    member val Price = p.Price
    member val Stock = p.Stock
    member val SelectedQty = 1 with get, set

// CartItem view model (UI)
type CartItemVm(item: CartItem) =
    inherit ViewModelBase()
    let mutable qty = item.Quantity
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

// MainWindowViewModel
type MainWindowViewModel() as this =
    inherit ViewModelBase()

    // -----------------------
    // local let-bindings (must be before members)
    // -----------------------
    let products = ObservableCollection<ProductVm>()
    let filtered = ObservableCollection<ProductVm>()
    let cartItems = ObservableCollection<CartItemVm>()
    let mutable cartState : Cart = Cart.empty

    // local backing for simple properties (we'll expose them as members)
    let mutable searchTextLocal = ""
    let mutable selectedCategoryLocal = "All"

    // categories collection (local)
    let categoriesLocal = ObservableCollection<string>()

    // -----------------------
    // do: populate initial data (safe to use the local lets)
    // -----------------------
    do
        let catalog = loadCatalog()
        getAllProducts catalog
        |> List.iter (fun p -> products.Add(ProductVm(p)))

        // build categories list: "All" + distinct categories
        let cats =
            products
            |> Seq.map (fun (p: ProductVm) -> p.Category)
            |> Seq.distinct
            |> Seq.sort
            |> Seq.toList

        categoriesLocal.Clear()
        categoriesLocal.Add("All")
        cats |> List.iter (fun c -> categoriesLocal.Add(c))

        // initial filtered (copy all products)
        products |> Seq.iter (fun p -> filtered.Add(p))

    // -----------------------
    // members (after let/do)
    // -----------------------

    // simple exposed properties
    member val SelectedCartItem : CartItemVm option = None with get, set

    member _.Catalog = products
    member _.FilteredProducts = filtered
    member _.CartItems = cartItems

    // expose categories collection
    member _.Categories = categoriesLocal

    // SearchText and SelectedCategory as properties that update locals
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

    // Refresh filter function
    member private this.RefreshFilter() =
        let q = if isNull this.SearchText then "" else this.SearchText.Trim().ToLowerInvariant()
        let cat = if isNull this.SelectedCategory then "All" else this.SelectedCategory
        filtered.Clear()
        products
        |> Seq.filter (fun p ->
            let matchesCat = (cat = "All") || (String.Equals(p.Category, cat, StringComparison.OrdinalIgnoreCase))
            let matchesSearch = (String.IsNullOrWhiteSpace(q)) || p.Name.ToLowerInvariant().Contains(q)
            matchesCat && matchesSearch
        )
        |> Seq.iter (fun p -> filtered.Add(p))
        this.RaisePropertyChanged("FilteredProducts")

    // sync UI cart view with internal map
    member private this.SyncCart() =
        cartItems.Clear()
        cartState
        |> toList
        |> List.iter (fun ci -> cartItems.Add(CartItemVm(ci)))
        this.RaisePropertyChanged("Total")

    // total price property
    member _.Total
        with get() = getTotalPrice cartState

    // helper: find productvm
    member private this.TryFindProductVm(productId:int) =
        products |> Seq.tryFind (fun p -> p.Id = productId)

    // ===== Commands (fixed member val initializers with parentheses) =====
    member val SearchCommand : ICommand = (DelegateCommand(fun _ -> this.RefreshFilter()) :> ICommand) with get, set
    member val ClearSearchCommand : ICommand = (DelegateCommand(fun _ ->
        this.SearchText <- ""
        this.SelectedCategory <- "All"
        this.RefreshFilter()
    ) :> ICommand) with get, set

    member _.AddToCartCommand =
        DelegateCommand(fun param ->
            match param with
            | :? int as pid ->
                printfn "[VM] AddToCartCommand invoked for id=%d" pid
                match this.TryFindProductVm(pid) with
                | Some pvm ->
                    let qty = if pvm.SelectedQty < 1 then 1 else pvm.SelectedQty
                    let catalog = loadCatalog()
                    cartState <- addItem catalog pid qty cartState
                    this.SyncCart()
                    printfn "[VM] Cart items after add: %d" (cartState |> Map.count)
                | None -> printfn "[VM] AddToCart: product not found id=%d" pid
            | _ -> ()
        ) :> ICommand

    member _.RemoveFromCartCommand =
        DelegateCommand(fun param ->
            match param with
            | :? int as pid ->
                printfn "[VM] RemoveFromCartCommand invoked for id=%d" pid
                cartState <- removeFromCart pid cartState
                this.SyncCart()
            | _ -> ()
        ) :> ICommand

    member _.IncreaseQtyCommand =
        DelegateCommand(fun param ->
            match param with
            | :? int as pid ->
                let currentQty =
                    match Map.tryFind pid cartState with
                    | Some it -> it.Quantity
                    | None -> 0
                cartState <- updateQuantity pid (currentQty + 1) cartState
                this.SyncCart()
            | _ -> ()
        ) :> ICommand

    member _.DecreaseQtyCommand =
        DelegateCommand(fun param ->
            match param with
            | :? int as pid ->
                let currentQty =
                    match Map.tryFind pid cartState with
                    | Some it -> it.Quantity
                    | None -> 0
                let newQty = currentQty - 1
                cartState <- updateQuantity pid newQty cartState
                this.SyncCart()
            | _ -> ()
        ) :> ICommand

    member _.ClearCartCommand =
        DelegateCommand(fun _ ->
            printfn "[VM] ClearCartCommand invoked"
            cartState <- Cart.empty
            this.SyncCart()
            printfn "[VM] Cart cleared."
        ) :> ICommand

    member _.SaveCartCommand =
        DelegateCommand(fun _ ->
            try
                printfn "[VM] SaveCartCommand invoked. items=%d" (cartState |> Map.count)
                // always attempt to save (serializes empty cart too)
                saveCartToFile "cart.json" cartState
                printfn "[VM] Saved cart.json successfully."
            with ex ->
                printfn "[VM] SaveCartCommand failed: %A" ex
        ) :> ICommand

    member _.LoadCartCommand =
        DelegateCommand(fun _ ->
            try
                printfn "[VM] LoadCartCommand invoked. cart.json exists=%b" (cartFileExists "cart.json")
                if cartFileExists "cart.json" then
                    cartState <- loadCartFromFile "cart.json"
                    this.SyncCart()
                    printfn "[VM] Loaded cart.json. items=%d" (cartState |> Map.count)
                else
                    printfn "[VM] No cart.json to load."
            with ex ->
                printfn "[VM] LoadCartCommand failed: %A" ex
        ) :> ICommand

    member _.BackupCommand =
        DelegateCommand(fun _ ->
            try
                printfn "[VM] BackupCommand invoked. cart.json exists=%b" (cartFileExists "cart.json")
                if cartFileExists "cart.json" then
                    let backupPath = createBackup "cart.json"
                    printfn "[VM] Backup created: %s" backupPath
                else
                    // if no cart.json, still create a saved empty cart first then backup
                    saveCartToFile "cart.json" cartState
                    let backupPath = createBackup "cart.json"
                    printfn "[VM] cart.json was missing — saved current (maybe empty) cart then backed up: %s" backupPath
            with ex ->
                printfn "[VM] BackupCommand failed: %A" ex
        ) :> ICommand

    member _.RestoreBackupCommand =
        DelegateCommand(fun _ ->
            try
                printfn "[VM] RestoreBackupCommand invoked."

                // use helper from BackupManager to pick the newest backup
                match tryGetLatestBackup() with
                | Some latest ->
                    printfn "[VM] Restoring from: %s" latest
                    restoreFromBackup latest "cart.json"

                    // reload cart from the restored file and sync UI
                    cartState <- loadCartFromFile "cart.json"
                    this.SyncCart()
                    printfn "[VM] Restore completed. items=%d" (cartState |> Map.count)
                | None ->
                    printfn "[VM] No backups found to restore."
            with ex ->
                printfn "[VM] RestoreBackupCommand failed: %A" ex
        ) :> ICommand
