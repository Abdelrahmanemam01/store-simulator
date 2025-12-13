namespace StoreSimulator.UI.ViewModels

open System
open System.Collections.ObjectModel
open System.Windows.Input

open Catalog
open Cart
open Database.DatabaseService
open PriceCalculator
open CartJson
open BackupManager
open System.IO

// Product view model with SelectedQty for the UI (now with mutable Stock and IsSoldOut)
type ProductVm(p: Product) =
    inherit ViewModelBase()
    let mutable stockLocal = p.Stock
    let mutable errorMessageLocal = ""
    let mutable selectedQtyStr = "1"
    member val Id = p.Id
    member val Name = p.Name
    member val Category = p.Category
    member val Price = p.Price

    member this.SelectedQty
        with get() = selectedQtyStr
        and set(v) =
            selectedQtyStr <- v
            this.RaisePropertyChanged("SelectedQty")
            // Clear error when user types
            if not (String.IsNullOrEmpty this.ErrorMessage) then
                this.ErrorMessage <- ""

    // Helper to parse SelectedQty string to int, returns None if invalid
    member this.TryParseQty() =
        match System.Int32.TryParse(selectedQtyStr) with
        | (true, qty) when qty > 0 -> Some qty
        | _ -> None

    member this.Stock
        with get() = stockLocal
        and set(v) =
            stockLocal <- v
            this.RaisePropertyChanged("Stock")
            this.RaisePropertyChanged("IsSoldOut")

    member this.IsSoldOut
        with get() = stockLocal <= 0

    member this.ErrorMessage
        with get() = errorMessageLocal
        and set(v) =
            errorMessageLocal <- v
            this.RaisePropertyChanged("ErrorMessage")

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
type MainWindowViewModel(userId: int) as this =
    inherit ViewModelBase()

    // -----------------------
    // local let-bindings
    // -----------------------
    let products = ObservableCollection<ProductVm>()
    let filtered = ObservableCollection<ProductVm>()
    let cartItems = ObservableCollection<CartItemVm>()
    let mutable cartState : Cart = Cart.empty
    let currentUserId = userId

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
        // Initialize database
        Database.DatabaseService.initializeDatabase()
        
        // Load cart from database
        cartState <- loadCartFromDb currentUserId
        
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
        
        // Initialize cart display (without raising property changed events during init)
        cartItems.Clear()
        let catalog = loadCatalog()
        
        // Compute stock map
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
                pvm.Stock <- remaining
                (pvm.Id, remaining)
            )
            |> Map.ofSeq
        
        // Add cart items
        cartState
        |> toList
        |> List.iter (fun ci ->
            let remaining =
                match Map.tryFind ci.ProductId stockMap with
                | Some r -> r
                | None -> 0
            cartItems.Add(CartItemVm(ci, remaining))
        )
        
        // Calculate totals (but don't raise property changed yet)
        let (_, perc, amount, _) = calculateCheckoutTotalWithDetails cartState
        discountPercentageLocal <- perc
        discountAmountLocal <- amount
        discountVisibleLocal <- (perc > 0M)

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

    // helper to set error message on ProductVm (for left side catalog display)
    member private this.SetProductError (productId:int) (msg:string) =
        products
        |> Seq.tryFind (fun p -> p.Id = productId)
        |> Option.iter (fun p -> p.ErrorMessage <- msg)
        
    // helper to clear error message on ProductVm
    member private this.ClearProductError (productId:int) =
        products
        |> Seq.tryFind (fun p -> p.Id = productId)
        |> Option.iter (fun p -> p.ErrorMessage <- "")

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
            try
                match param with
                | :? int as pid ->
                    match this.TryFindProductVm(pid) with
                    | Some pvm ->
                        // Clear any previous error
                        this.ClearProductError pid
                        
                        // Parse quantity and handle InvalidCastException
                        match pvm.TryParseQty() with
                        | None ->
                            // Invalid or empty quantity - show out of stock message
                            this.SetProductError pid "⚠ Out of stock"
                        | Some qty ->
                            let catalog = loadCatalog()
                            // addItem returns Result<Cart,string>
                            match addItem catalog pid qty cartState with
                            | Ok newCart ->
                                cartState <- newCart
                                saveCartToDb currentUserId cartState
                                this.SyncCart()
                                // Clear error on success
                                this.ClearProductError pid
                            | Error err ->
                                // Only show out of stock message
                                this.SetProductError pid "⚠ Out of stock"
                                // Also set error on the cart item (if present) so it appears near "Sold out"
                                this.SetCartItemError pid err
                    | None -> ()
                | _ -> ()
            with
            | :? System.InvalidCastException as ex ->
                // Handle InvalidCastException - show out of stock message
                match param with
                | :? int as pid ->
                    this.SetProductError pid "⚠ Out of stock"
                | _ -> ()
            | ex ->
                // Handle any other exceptions
                match param with
                | :? int as pid ->
                    this.SetProductError pid "⚠ Out of stock"
                | _ -> ()) :> ICommand

    member _.RemoveFromCartCommand =
        DelegateCommand(fun param ->
            match param with
            | :? int as pid ->
                cartState <- removeFromCart pid cartState
                saveCartToDb currentUserId cartState
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
                    saveCartToDb currentUserId cartState
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
                    saveCartToDb currentUserId cartState
                    this.SyncCart()
                | Error err ->
                    this.SetCartItemError pid err
            | _ -> ()) :> ICommand

    member _.ClearCartCommand =
        DelegateCommand(fun _ ->
            try
                let itemCount = cartState |> Map.count
                
                if itemCount = 0 then
                    this.ErrorMessage <- "⚠ Cart is already empty. Nothing to clear."
                    System.Console.WriteLine("ClearCart: Cart is already empty")
                else
                    let itemText = if itemCount = 1 then "item" else "items"
                    
                    // Clear cart
                    cartState <- Cart.empty
                    System.Console.WriteLine($"Cart cleared: Removed {itemCount} {itemText}")
                    
                    // Save empty cart to database
                    saveCartToDb currentUserId cartState
                    System.Console.WriteLine($"Empty cart saved to database: UserId={currentUserId}")
                    
                    // Sync UI
                    this.SyncCart()
                    
                    this.ErrorMessage <- $"✓ Cart cleared successfully!\n\nRemoved {itemCount} {itemText}.\nCart is now empty and saved to database."
            with
            | ex -> 
                this.ErrorMessage <- $"✗ Failed to clear cart: {ex.Message}\n\nPlease try again."
                System.Console.WriteLine($"ClearCart error: {ex.Message}\n{ex.StackTrace}")) :> ICommand

    member _.SaveCartCommand =
        DelegateCommand(fun _ ->
            try
                let itemCount = cartState |> Map.count
                let total = this.Total
                
                if itemCount = 0 then
                    this.ErrorMessage <- "⚠ Cart is empty. Nothing to save."
                    System.Console.WriteLine("SaveCart: Cart is empty")
                else
                    // Save to database
                    saveCartToDb currentUserId cartState
                    System.Console.WriteLine($"Cart saved to database: UserId={currentUserId}, Items={itemCount}, Total={total}")
                    
                    // Sync UI to reflect any changes
                    this.SyncCart()
                    
                    // Show success message with details
                    let itemText = if itemCount = 1 then "item" else "items"
                    this.ErrorMessage <- $"✓ Cart saved to database successfully!\n\n{itemCount} {itemText} | Total: {total:C}\nYour cart is now persisted in the database."
            with
            | ex -> 
                this.ErrorMessage <- $"✗ Failed to save cart: {ex.Message}\n\nPlease try again."
                System.Console.WriteLine($"SaveCart error: {ex.Message}\n{ex.StackTrace}")) :> ICommand

    member _.LoadCartCommand =
        DelegateCommand(fun _ ->
            try
                System.Console.WriteLine($"Loading cart from database: UserId={currentUserId}")
                
                // Load from database
                let loadedCart = loadCartFromDb currentUserId
                let itemCount = loadedCart |> Map.count
                System.Console.WriteLine($"Loaded {itemCount} items from database")
                
                // Update cart state
                cartState <- loadedCart
                
                // Sync UI
                this.SyncCart()
                
                if itemCount > 0 then
                    let total = this.Total
                    let itemText = if itemCount = 1 then "item" else "items"
                    this.ErrorMessage <- $"✓ Cart loaded from database successfully!\n\n{itemCount} {itemText} | Total: {total:C}\nYour saved cart has been restored."
                    System.Console.WriteLine($"Cart loaded: UserId={currentUserId}, Items={itemCount}, Total={total}")
                else
                    this.ErrorMessage <- "✓ Cart loaded from database.\n\nCart is empty (no saved items)."
                    System.Console.WriteLine($"Cart loaded: UserId={currentUserId}, Cart is empty")
            with
            | ex -> 
                this.ErrorMessage <- $"✗ Failed to load cart: {ex.Message}\n\nPlease try again."
                System.Console.WriteLine($"LoadCart error: {ex.Message}\n{ex.StackTrace}")) :> ICommand

    member _.BackupCommand =
        DelegateCommand(fun _ ->
            try
                let itemCount = cartState |> Map.count
                let total = this.Total
                
                if itemCount = 0 then
                    this.ErrorMessage <- "⚠ Cart is empty. Nothing to backup."
                    System.Console.WriteLine("Backup: Cart is empty")
                else
                    // Save cart to JSON file first
                    let cartFilePath = "cart.json"
                    saveCartToFile cartFilePath cartState
                    System.Console.WriteLine($"Cart saved to JSON: {cartFilePath}")
                    
                    // Create backup using BackupManager
                    let backupPath = createBackup cartFilePath
                    System.Console.WriteLine($"Backup created: {backupPath}")
                    
                    // Also save to database
                    saveCartToDb currentUserId cartState
                    System.Console.WriteLine($"Cart saved to database: UserId={currentUserId}, Items={itemCount}, Total={total}")
                    
                    // Sync UI
                    this.SyncCart()
                    
                    let backupFileName = Path.GetFileName(backupPath)
                    let itemText = if itemCount = 1 then "item" else "items"
                    this.ErrorMessage <- $"✓ Cart backed up successfully!\n\n{itemCount} {itemText} | Total: {total:C}\nBackup file: {backupFileName}\nDatabase: Saved ✓"
                    System.Console.WriteLine($"Backup complete: {backupPath}, Database: saved")
            with
            | ex -> 
                this.ErrorMessage <- $"✗ Backup failed: {ex.Message}\n\nPlease try again."
                System.Console.WriteLine($"Backup error: {ex.Message}\n{ex.StackTrace}")) :> ICommand

    member _.RestoreBackupCommand =
        DelegateCommand(fun _ ->
            try
                match tryGetLatestBackup() with
                | Some backupPath ->
                    System.Console.WriteLine($"Restoring from backup: {backupPath}")
                    
                    // Restore from backup to cart.json file
                    let cartFilePath = "cart.json"
                    restoreFromBackup backupPath cartFilePath
                    System.Console.WriteLine($"Backup restored to: {cartFilePath}")
                    
                    // Load the restored cart
                    let restoredCart = loadCartFromFile cartFilePath
                    let itemCount = restoredCart |> Map.count
                    System.Console.WriteLine($"Loaded {itemCount} items from backup")
                    
                    // Update cart state
                    cartState <- restoredCart
                    
                    // Save to database
                    saveCartToDb currentUserId cartState
                    System.Console.WriteLine($"Restored cart saved to database: UserId={currentUserId}, Items={itemCount}")
                    
                    // Sync UI
                    this.SyncCart()
                    
                    if itemCount > 0 then
                        let total = this.Total
                        let backupFileName = Path.GetFileName(backupPath)
                        let itemText = if itemCount = 1 then "item" else "items"
                        this.ErrorMessage <- $"✓ Cart restored from backup successfully!\n\n{itemCount} {itemText} | Total: {total:C}\nBackup file: {backupFileName}\nDatabase: Saved ✓"
                        System.Console.WriteLine($"Restore complete: {itemCount} items, Total: {total}, from {backupPath}")
                    else
                        let backupFileName = Path.GetFileName(backupPath)
                        this.ErrorMessage <- $"✓ Cart restored from backup (empty cart).\n\nBackup file: {backupFileName}\nDatabase: Saved ✓"
                        System.Console.WriteLine($"Restore complete: Empty cart from {backupPath}")
                | None ->
                    this.ErrorMessage <- "✗ No backup found to restore.\n\nPlease create a backup first using the Backup button."
                    System.Console.WriteLine("Restore failed: No backup found")
            with
            | ex -> 
                this.ErrorMessage <- $"✗ Failed to restore: {ex.Message}\n\nPlease check if backup file exists."
                System.Console.WriteLine($"Restore error: {ex.Message}\n{ex.StackTrace}")) :> ICommand
