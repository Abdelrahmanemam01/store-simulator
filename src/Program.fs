module Program

open Catalog
open Cart
open CartJson
open BackupManager
open System.IO


[<EntryPoint>]
let main argv = 
    let catalog = loadCatalog()

    printfn "       === All Products (%d) ===" (catalog.Count)
    getAllProducts catalog 
          |> List.iter (fun p -> printfn "%d: %s (%s) - Price: %M - Stock: %d" p.Id p.Name p.Category p.Price p.Stock)


    printfn "\n=== CART TEST ==="

   // let the cart mutable just for testing
    let mutable cart = Cart.empty

    cart <- Cart.addItem catalog 1 1 cart    // Laptop ×1
    cart <- Cart.addItem catalog 2 2 cart    // Mouse ×2
    cart <- Cart.addItem catalog 8 3 cart    // Sugar ×3

    printfn "\n--- Items in cart ---"
    Cart.toList cart |> List.iter (fun i ->
        printfn "ProductId:%d  %s  Qty:%d  Unit:%M" 
            i.ProductId i.ProductName i.Quantity i.UnitPrice
    )

    printfn "\nTotal = %M" (Cart.getTotalPrice cart)

    printfn "\n--- Saving cart.json ---"
    saveCartWithBackup "cart.json" cart
    printfn "Cart saved and backup created successfully!"




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

    printfn "\n=== LIST BACKUPS ==="
    let backups = listBackups()

    if backups.IsEmpty then
        printfn "No backups found."
    else
        backups |> List.iteri (fun idx b -> printfn "%d) %s" idx b)

    printfn "\n=== RESTORE TEST ==="
    if not backups.IsEmpty then
        let latest = backups |> List.last   
        printfn "Restoring from: %s" latest

        printfn "\n--- CONTENT OF LATEST BACKUP ---"
        printBackupPretty latest
        printfn "-------------------------------"


        restoreFromBackup latest "cart.json"
        printfn "Restore done! cart.json has been replaced."


    0
