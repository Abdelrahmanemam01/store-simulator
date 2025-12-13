module Database.DatabaseService

open System
open System.Linq
open Microsoft.EntityFrameworkCore
open Database.StoreDbContext
open Database.Models
open Cart

// Initialize database and seed products if needed
let initializeDatabase () =
    use context = StoreDbContext.CreateContext()
    context.Database.EnsureCreated() |> ignore
    
    // Seed products if database is empty
    let hasProducts = 
        try
            context.Products.Count() > 0
        with
        | _ -> false
    
    if not hasProducts then
        let sampleProducts: (int * string * string * decimal * int) list = [
            (1, "Laptop", "Electronics", 15000M, 10)
            (2, "Mouse", "Electronics", 250M, 50)
            (3, "Keyboard", "Electronics", 400M, 30)
            (4, "Monitor", "Electronics", 2200M, 12)
            (5, "USB Flash 32GB", "Electronics", 180M, 60)
            (6, "Fresh Milk", "Grocery", 28.5M, 100)
            (7, "Tea Pack", "Grocery", 35M, 70)
            (8, "Sugar 1kg", "Grocery", 25M, 90)
            (9, "Pasta 500g", "Grocery", 19M, 80)
            (10, "Olive Oil", "Grocery", 120M, 40)
            (11, "Shampoo", "Cosmetics", 70M, 30)
            (12, "Face Wash", "Cosmetics", 55M, 25)
            (13, "Hand Cream", "Cosmetics", 45M, 18)
            (14, "Perfume", "Cosmetics", 300M, 10)
            (15, "Book: Algorithms", "Books", 180M, 20)
            (16, "Book: Clean Code", "Books", 250M, 15)
            (17, "Book: Deep Learning", "Books", 350M, 8)
            (18, "Notebook A5", "Books", 15M, 100)
            (19, "Hammer", "Tools", 140M, 25)
            (20, "Screwdriver", "Tools", 80M, 30)
            (21, "Drill", "Tools", 900M, 10)
        ]
        
        sampleProducts |> List.iter (fun (id, name, category, price, stock) -> 
            let entity = ProductEntity()
            entity.Id <- id
            entity.Name <- name
            entity.Category <- category
            entity.Price <- price
            entity.Stock <- stock
            context.Products.Add(entity) |> ignore
        )
        context.SaveChanges() |> ignore

// Load catalog from database
let loadCatalogFromDb () : Catalog.ProductCatalog =
    use context = StoreDbContext.CreateContext()
    context.Products
    |> Seq.map (fun p -> 
        {
            Id = p.Id
            Name = p.Name
            Category = p.Category
            Price = p.Price
            Stock = p.Stock
        } : Catalog.Product
    )
    |> Seq.map (fun p -> p.Id, p)
    |> Map.ofSeq

// Register the load function with Catalog module after the function is defined
// This breaks the circular dependency at compile time
do
    try
        Catalog.setLoadCatalogFunction loadCatalogFromDb
    with
    | _ -> () // Ignore if Catalog module hasn't loaded yet

// Get or create cart for user
let getOrCreateCart (userId: int) : int =
    use context = StoreDbContext.CreateContext()
    let cart = 
        query {
            for c in context.Carts do
            where (c.UserId = userId)
            select c
            take 1
        } |> Seq.tryHead
    
    match cart with
    | Some c -> c.Id
    | None ->
        let newCart = CartEntity()
        newCart.UserId <- userId
        newCart.CreatedAt <- DateTime.UtcNow
        newCart.UpdatedAt <- DateTime.UtcNow
        let added = context.Carts.Add(newCart)
        context.SaveChanges() |> ignore
        added.Entity.Id

// Save cart to database
let saveCartToDb (userId: int) (cart: Cart) =
    use context = StoreDbContext.CreateContext()
    let cartId = getOrCreateCart userId
    
    // Remove existing cart items
    let existingItems = 
        query {
            for ci in context.CartItems do
            where (ci.CartId = cartId)
            select ci
        } |> Seq.toList
    
    existingItems |> List.iter (fun item -> context.CartItems.Remove(item) |> ignore)
    
    // Add current cart items
    cart
    |> Map.toList
    |> List.iter (fun (_, item) ->
        let cartItem = CartItemEntity()
        cartItem.CartId <- cartId
        cartItem.ProductId <- item.ProductId
        cartItem.Quantity <- item.Quantity
        cartItem.UnitPrice <- item.UnitPrice
        context.CartItems.Add(cartItem) |> ignore
    )
    
    // Update cart timestamp
    let cartEntity = context.Carts.Find(cartId)
    if not (isNull cartEntity) then
        cartEntity.UpdatedAt <- DateTime.UtcNow
    
    context.SaveChanges() |> ignore

// Load cart from database
let loadCartFromDb (userId: int) : Cart =
    use context = StoreDbContext.CreateContext()
    let cartId = 
        query {
            for c in context.Carts do
            where (c.UserId = userId)
            select c.Id
            take 1
        } |> Seq.tryHead
    
    match cartId with
    | None -> Map.empty
    | Some cid ->
        let items = 
            query {
                for ci in context.CartItems do
                where (ci.CartId = cid)
                select ci
            } |> Seq.toList
        
        items
        |> List.map (fun ci ->
            let product = context.Products.Find(ci.ProductId)
            if isNull product then None
            else
                Some (ci.ProductId, {
                    ProductId = ci.ProductId
                    ProductName = product.Name
                    UnitPrice = ci.UnitPrice
                    Quantity = ci.Quantity
                })
        )
        |> List.choose id
        |> Map.ofList

