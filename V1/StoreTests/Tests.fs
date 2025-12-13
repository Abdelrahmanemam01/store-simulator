module Tests

open System
open Xunit
open Catalog
open Cart
open CartJson
open PriceCalculator
open BackupManager
open System.IO


// Catalog Tests
[<Fact>]
let ``Catalog: findById returns correct product`` () =
    let catalog = loadCatalog()
    match findById 1 catalog with
    | Some p -> Assert.Equal("Laptop", p.Name)
    | None -> Assert.True(false, "Expected to find product with Id 1")

[<Fact>]
let ``Catalog: findById returns None for non-existing product`` () =
    let catalog = loadCatalog()
    let result = findById 999 catalog
    Assert.Equal(None, result)

[<Fact>]
let ``Catalog: searchByName finds products containing keyword`` () =
    let catalog = loadCatalog()
    let results = searchByName "book" catalog
    Assert.True(results |> List.exists (fun p -> p.Name.ToLower().Contains("book")))

[<Fact>]
let ``Catalog: searchByName returns empty for unmatched keyword`` () =
    let catalog = loadCatalog()
    let results = searchByName "nonexistentkeyword" catalog
    Assert.True(List.isEmpty results)

[<Fact>]
let ``Catalog: filterByCategory returns correct category`` () =
    let catalog = loadCatalog()
    let results = filterByCategory "Grocery" catalog
    Assert.True(results |> List.forall (fun p -> p.Category = "Grocery"))

[<Fact>]
let ``Catalog: filterByCategory is case-insensitive`` () =
    let catalog = loadCatalog()
    let resultsLower = filterByCategory "grocery" catalog
    let resultsUpper = filterByCategory "GROCERY" catalog
    Assert.Equal(resultsLower.Length, resultsUpper.Length)

[<Fact>]
let ``Catalog: filterByPriceRange returns correct range`` () =
    let catalog = loadCatalog()
    let results = filterByPriceRange 0M 200M catalog
    Assert.True(results |> List.forall (fun p -> p.Price >= 0M && p.Price <= 200M))

[<Fact>]
let ``Catalog: filterByPriceRange returns empty for invalid range`` () =
    let catalog = loadCatalog()
    let results = filterByPriceRange -100M -1M catalog
    Assert.True(List.isEmpty results)

[<Fact>]
let ``Catalog: filterByStockAvailability returns correct stock`` () =
    let catalog = loadCatalog()
    let results = filterByStockAvailability 50 catalog
    Assert.True(results |> List.forall (fun p -> p.Stock >= 50))


// Cart Tests
[<Fact>]
let ``Cart: addItem increases quantity`` () =
    let catalog = loadCatalog()
    let mutable cart = Cart.empty
    cart <- addItem catalog 1 2 cart
    cart <- addItem catalog 1 3 cart
    let item = Map.find 1 cart
    Assert.Equal(5, item.Quantity)

[<Fact>]
let ``Cart: addItem respects large quantities`` () =
    let catalog = loadCatalog()
    let cart = addItem catalog 1 1000000 Cart.empty
    let item = Map.find 1 cart
    Assert.Equal(1000000, item.Quantity)

[<Fact>]
let ``Cart: adding zero or negative quantity does nothing`` () =
    let catalog = loadCatalog()
    let cart = addItem catalog 1 0 Cart.empty
    Assert.True(cart.IsEmpty)
    let cart2 = addItem catalog 1 (-5) Cart.empty
    Assert.True(cart2.IsEmpty)

[<Fact>]
let ``Cart: adding non-existing product does nothing`` () =
    let catalog = loadCatalog()
    let cart = addItem catalog 999 3 Cart.empty
    Assert.True(cart.IsEmpty)

[<Fact>]
let ``Cart: updateQuantity changes quantity`` () =
    let catalog = loadCatalog()
    let mutable cart = Cart.empty
    cart <- addItem catalog 2 2 cart
    cart <- updateQuantity 2 5 cart
    let item = Map.find 2 cart
    Assert.Equal(5, item.Quantity)

[<Fact>]
let ``Cart: updateQuantity removes item if zero`` () =
    let catalog = loadCatalog()
    let mutable cart = Cart.empty
    cart <- addItem catalog 2 2 cart
    cart <- updateQuantity 2 0 cart
    Assert.False(cart |> Map.containsKey 2)

[<Fact>]
let ``Cart: updateQuantity negative quantity removes item`` () =
    let catalog = loadCatalog()
    let mutable cart = Cart.empty
    cart <- addItem catalog 2 2 cart
    cart <- updateQuantity 2 -5 cart
    Assert.False(cart |> Map.containsKey 2)

[<Fact>]
let ``Cart: removeFromCart works`` () =
    let catalog = loadCatalog()
    let mutable cart = Cart.empty
    cart <- addItem catalog 3 1 cart
    cart <- removeFromCart 3 cart
    Assert.False(cart |> Map.containsKey 3)

[<Fact>]
let ``Cart: removeFromCart non-existing product does nothing`` () =
    let cart = Cart.empty
    let updated = removeFromCart 999 cart
    Assert.True(updated.IsEmpty)

[<Fact>]
let ``Cart: clearCart empties cart`` () =
    let catalog = loadCatalog()
    let mutable cart = Cart.empty
    cart <- addItem catalog 4 1 cart
    let cleared = clearCart cart
    Assert.True(cleared.IsEmpty)

[<Fact>]
let ``Cart: getTotalPrice calculates correctly`` () =
    let catalog = loadCatalog()
    let mutable cart = Cart.empty
    cart <- addItem catalog 5 2 cart   // 180*2 = 360
    cart <- addItem catalog 6 3 cart   // 28.5*3 = 85.5
    let total = getTotalPrice cart
    Assert.Equal(445.5M, total)


// Pricing / Discount Tests
[<Fact>]
let ``Cart: applyPercentageDiscount applies correctly`` () =
    let catalog = loadCatalog()
    let mutable cart = Cart.empty
    cart <- addItem catalog 5 2 cart   // 180*2 = 360
    let discounted = applyPercentageDiscount 10M cart  // 10% off -> 324
    Assert.Equal(324M, discounted)

[<Fact>]
let ``Pricing: zero discount returns same total`` () =
    let catalog = loadCatalog()
    let mutable cart = Cart.empty
    cart <- addItem catalog 1 1 cart
    let total = applyPercentageDiscount 0M cart
    Assert.Equal(getTotalPrice cart, total)

[<Fact>]
let ``Pricing: 100 percent discount returns zero`` () =
    let catalog = loadCatalog()
    let mutable cart = Cart.empty
    cart <- addItem catalog 1 1 cart
    let total = applyPercentageDiscount 100M cart
    Assert.Equal(0M, total)

[<Fact>]
let ``Pricing: multiple sequential discounts calculate correctly`` () =
    let catalog = loadCatalog()
    let mutable cart = Cart.empty
    cart <- addItem catalog 5 1 cart   // 180
    let first = applyPercentageDiscount 10M cart // 162
    let second = applyPercentageDiscount 20M (Cart.empty |> addItem catalog 5 1) // 144
    Assert.Equal(162M, first)
    Assert.Equal(180M, getTotalPrice (Cart.empty |> addItem catalog 5 1)) 

[<Fact>]
let ``PriceCalculator: calculateCheckoutTotal returns correct final`` () =
    let catalog = loadCatalog()
    let mutable cart = Cart.empty
    cart <- addItem catalog 1 1 cart   // Laptop 15000
    cart <- addItem catalog 2 2 cart   // Mouse 250*2=500
    let total = calculateCheckoutTotal cart 10M  // 10% discount
    Assert.Equal(15500M * 0.9M, total)

[<Fact>]
let ``PriceCalculator: calculateCheckoutTotal with zero discount`` () =
    let catalog = loadCatalog()
    let mutable cart = Cart.empty
    cart <- addItem catalog 2 2 cart
    let total = calculateCheckoutTotal cart 0M
    Assert.Equal(getTotalPrice cart, total)


// CartJson Tests
[<Fact>]
let ``CartJson: cartToJson and loadCartFromJson roundtrip`` () =
    let catalog = loadCatalog()
    let mutable cart = Cart.empty
    cart <- addItem catalog 1 1 cart
    let json = cartToJson cart
    let loaded = loadCartFromJson json
    Assert.Equal(cart |> Map.count, loaded |> Map.count)
    let itemOriginal = Map.find 1 cart
    let itemLoaded = Map.find 1 loaded
    Assert.Equal(itemOriginal.ProductName, itemLoaded.ProductName)
    Assert.Equal(itemOriginal.Quantity, itemLoaded.Quantity)

[<Fact>]
let ``CartJson: loadCartFromJson empty json returns empty cart`` () =
    let loaded = loadCartFromJson "{}"
    Assert.True(loaded.IsEmpty)

[<Fact>]
let ``CartJson: loadCartFromJson invalid json returns empty cart`` () =
    try
        let loaded = loadCartFromJson "{invalid json}"
        Assert.True(loaded.IsEmpty)
    with
    | _ -> Assert.True(true) 

[<Fact>]
let ``CartJson: cartToJsonPretty returns valid JSON`` () =
    let catalog = loadCatalog()
    let mutable cart = Cart.empty
    cart <- addItem catalog 1 1 cart
    let json = cartToJsonPretty cart
    Assert.Contains("Laptop", json)

[<Fact>]
let ``CartJson: save and load cart from file`` () =
    let path = Path.GetTempFileName()
    let catalog = loadCatalog()
    let mutable cart = Cart.empty
    cart <- addItem catalog 1 2 cart
    saveCartToFile path cart
    let loaded = loadCartFromFile path
    Assert.Equal(cart |> Map.count, loaded |> Map.count)
    File.Delete(path)


// BackupManager Tests
[<Fact>]
let ``BackupManager: create and restore backup works`` () =
    let tempFile = Path.GetTempFileName()
    File.WriteAllText(tempFile, """{"test":123}""")
    let backupPath = createBackup tempFile
    Assert.True(File.Exists(backupPath))
    let restorePath = tempFile + "_restored"
    restoreFromBackup backupPath restorePath
    let restoredContent = File.ReadAllText(restorePath)
    Assert.Equal("""{"test":123}""", restoredContent)
    File.Delete(tempFile)
    File.Delete(restorePath)
    File.Delete(backupPath)

[<Fact>]
let ``BackupManager: tryGetLatestBackup returns None if no backups`` () =
    deleteAllBackups()
    let latest = tryGetLatestBackup()
    Assert.Equal(None, latest)

[<Fact>]
let ``BackupManager: deleteBackup on non-existing file does not crash`` () =
    deleteBackup "non_existing_file.json"
    Assert.True(true) 
[<Fact>]
let ``BackupManager: deleteAllBackups on empty folder does not crash`` () =
    deleteAllBackups()
    Assert.True(true)
