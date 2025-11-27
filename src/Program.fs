open Catalog
open Cart
open CartJson

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
            i.ProductId i.ProductName i.Quantity i.Unitprice
    )

    printfn "\nTotal = %M" (Cart.getTotalPrice cart)

    printfn "\n--- Saving cart.json ---"
    saveCartToFile "cart.json" cart
    printfn "JSON Saved successfully!"



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

    0
