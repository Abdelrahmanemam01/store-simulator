module Program

open Catalog
open Cart
open Database.DatabaseService
open System.IO


[<EntryPoint>]
let main argv = 
    // Initialize database first
    initializeDatabase()
    let catalog = loadCatalog()

    printfn "       === All Products (%d) ===" (catalog.Count)
    getAllProducts catalog 
          |> List.iter (fun p -> printfn "%d: %s (%s) - Price: %M - Stock: %d" p.Id p.Name p.Category p.Price p.Stock)


    printfn "\n=== CART TEST ==="

    // let the cart mutable just for testing
    let mutable cart = Cart.empty

    // helper: apply a Result<Cart,string> to the mutable cart (or print error)
    let applyResultToCart label result =
        match result with
        | Ok newCart ->
            cart <- newCart
        | Error err ->
            printfn "[Program] %s failed: %s" label err

    // Use the Result-returning functions and update cart only on Ok
    Cart.addItem catalog 1 1 cart |> applyResultToCart "addItem (id=1, qty=1)"    // Laptop ×1
    Cart.addItem catalog 2 2 cart |> applyResultToCart "addItem (id=2, qty=2)"    // Mouse ×2
    Cart.addItem catalog 8 3 cart |> applyResultToCart "addItem (id=8, qty=3)"    // Sugar ×3

    printfn "\n--- Items in cart ---"
    Cart.toList cart |> List.iter (fun i ->
        printfn "ProductId:%d  %s  Qty:%d  Unit:%M" 
            i.ProductId i.ProductName i.Quantity i.UnitPrice
    )

    printfn "\nTotal = %M" (Cart.getTotalPrice cart)

    printfn "\n--- Note: Cart operations now use database ---"
    printfn "For database operations, use the UI application."

    printfn "\n=== Find By Id ==="
    match findById 100 catalog with
    | Some p -> printfn "Found: %d - %s" p.Id p.Name
    | None -> printfn "Not Found"


    printfn "\n=== Filter By Category ==="
    filterByCategory "Grocery" catalog
    |> List.iter (fun p -> printfn "%d: %s - %M" p.Id p.Name p.Price)


    printfn "\n=== Search by Name ==="
    searchByName "book" catalog
    |> List.iter (fun p -> printfn "%d: %s - %M" p.Id p.Name p.Price)

    printfn "\n=== Filter: Price Range (0 - 200) ==="
    filterByPriceRange 0M 200M catalog
    |> List.iter (fun p -> printfn "%d: %s - %M" p.Id p.Name p.Price)

    printfn "\n=== Filter: Stock >= 20 ==="
    filterByStockAvailability 20 catalog
    |> List.iter (fun p -> printfn "%d: %s - %M (Stock: %d)" p.Id p.Name p.Price p.Stock)

    printfn "\n=== Filter: Electronics with Price <= 1000 ==="
    filterByCategoryAndMaxPrice "Electronics" 1000M catalog
    |> List.iter (fun p -> printfn "%d: %s - %M" p.Id p.Name p.Price)

    printfn "\n=== Database Status ==="
    printfn "Database initialized. All data is stored in SQLite database (store.db)."
    printfn "Use the UI application to manage carts with authentication."


    0
