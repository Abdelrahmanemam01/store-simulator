

module Catalog

type Product = {
    Id : int
    Name : string
    Category : string
    Price : decimal
    Stock : int
}

type ProductCatalog = Map<int, Product>

let private sampleProducts : Product list =
    [
        { Id=1; Name="Laptop"; Category="Electronics"; Price=15000M; Stock=10 }
        { Id=2; Name="Mouse"; Category="Electronics"; Price=250M; Stock=50 }
        { Id=3; Name="Keyboard"; Category="Electronics"; Price=400M; Stock=30 }
        { Id=4; Name="Monitor"; Category="Electronics"; Price=2200M; Stock=12 }
        { Id=5; Name="USB Flash 32GB"; Category="Electronics"; Price=180M; Stock=60 }
        { Id=6; Name="Fresh Milk"; Category="Grocery"; Price=28.5M; Stock=100 }
        { Id=7; Name="Tea Pack"; Category="Grocery"; Price=35M; Stock=70 }
        { Id=8; Name="Sugar 1kg"; Category="Grocery"; Price=25M; Stock=90 }
        { Id=9; Name="Pasta 500g"; Category="Grocery"; Price=19M; Stock=80 }
        { Id=10; Name="Olive Oil"; Category="Grocery"; Price=120M; Stock=40 }
        { Id=11; Name="Shampoo"; Category="Cosmetics"; Price=70M; Stock=30 }
        { Id=12; Name="Face Wash"; Category="Cosmetics"; Price=55M; Stock=25 }
        { Id=13; Name="Hand Cream"; Category="Cosmetics"; Price=45M; Stock=18 }
        { Id=14; Name="Perfume"; Category="Cosmetics"; Price=300M; Stock=10 }
        { Id=15; Name="Book: Algorithms"; Category="Books"; Price=180M; Stock=20 }
        { Id=16; Name="Book: Clean Code"; Category="Books"; Price=250M; Stock=15 }
        { Id=17; Name="Book: Deep Learning"; Category="Books"; Price=350M; Stock=8 }
        { Id=18; Name="Notebook A5"; Category="Books"; Price=15M; Stock=100 }
        { Id=19; Name="Hammer"; Category="Tools"; Price=140M; Stock=25 }
        { Id=20; Name="Screwdriver"; Category="Tools"; Price=80M; Stock=30 }
        { Id=21; Name="Drill"; Category="Tools"; Price=900M; Stock=10 }
    ]


let loadCatalog () : ProductCatalog =
    sampleProducts
    |> List.map (fun p -> p.Id, p)
    |> Map.ofList


let getAllProducts(catalog : ProductCatalog) : Product list = 
    catalog |> Map.toList |> List.map snd


let findById (id: int) (catalog: ProductCatalog) : Product option = 
    catalog |> Map.tryFind id


let filterByCategory (category: string) (catalog: ProductCatalog) : Product list =
    catalog 
    |> Map.toList 
    |> List.map snd 
    |> List.filter(fun p -> p.Category.ToLower() = category.ToLower())


let searchByName (keyword: string) (catalog: ProductCatalog) : Product list = 
    catalog 
    |> Map.toList 
    |> List.map snd 
    |> List.filter(fun p -> p.Name.ToLower().Contains(keyword.ToLower()))


// Generic filter: supply any predicate over Product
let filter (predicate: Product -> bool) (catalog: ProductCatalog) : Product list =
    catalog
    |> Map.toList
    |> List.map snd
    |> List.filter predicate

// Filter products by price range (inclusive)
let filterByPriceRange (minPrice: decimal) (maxPrice: decimal) (catalog: ProductCatalog) : Product list =
    filter (fun p -> p.Price >= minPrice && p.Price <= maxPrice) catalog

// Filter products by minimum stock available
let filterByStockAvailability (minStock: int) (catalog: ProductCatalog) : Product list =
    filter (fun p -> p.Stock >= minStock) catalog

// Combined filter example: category + maximum price
let filterByCategoryAndMaxPrice (category: string) (maxPrice: decimal) (catalog: ProductCatalog) : Product list =
    filter (fun p -> p.Category.ToLower() = category.ToLower() && p.Price <= maxPrice) catalog