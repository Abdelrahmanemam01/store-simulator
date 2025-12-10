module CartJson

open System
open System.IO
open System.Text.Json
open Cart
open BackupManager

type CartItemDto = {
    ProductId: int
    ProductName: string
    UnitPrice: decimal
    Quantity: int
}

type CartDto = {
    Items: CartItemDto[]
    Total: decimal
}

let private toDto (item: Cart.CartItem) =
    { ProductId = item.ProductId; ProductName = item.ProductName; UnitPrice = item.UnitPrice; Quantity = item.Quantity }

let cartToDto (cart: Cart.Cart) : CartDto =
    let items = cart |> toList |> List.map toDto |> List.toArray
    let total = getTotalPrice cart
    { Items = items; Total = total }

let cartToJson (cart: Cart) : string =
    cartToDto cart |> JsonSerializer.Serialize

let cartToJsonPretty (cart: Cart) : string =
    let dto = cartToDto cart
    let options = JsonSerializerOptions(WriteIndented = true)
    JsonSerializer.Serialize(dto, options)

let saveCartToFile (path:string) (cart: Cart) =
    let json = cartToJson cart
    File.WriteAllText(path, json)

// load cart from json
let loadCartFromJson (json:string) : Cart.Cart =
    let dtoOpt = JsonSerializer.Deserialize<CartDto>(json) |> Option.ofObj
    match dtoOpt with
    | None -> Map.empty
    | Some dto ->
        match dto.Items with
        | null | [||] -> Map.empty   
        | items ->
            items
            |> Array.fold (fun acc i ->
                acc |> Map.add i.ProductId {ProductId = i.ProductId; ProductName = i.ProductName; UnitPrice = i.UnitPrice; Quantity = i.Quantity }
            ) Map.empty

let cartFileExists (path:string) : bool =
    File.Exists(path)


let loadCartFromFile (path:string) : Cart =
    if cartFileExists path then
        let json = File.ReadAllText(path)
        loadCartFromJson json
    else Map.empty

let saveCartWithBackup (path:string) (cart: Cart) =
    saveCartToFile path cart      
    createBackup path |> ignore   
