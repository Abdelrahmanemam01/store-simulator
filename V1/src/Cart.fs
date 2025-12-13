module Cart

open Catalog

type CartItem = {
    ProductId : int
    ProductName: string
    UnitPrice: decimal
    Quantity : int
}

type Cart = Map<int , CartItem>

let empty : Cart = Map.empty

let private productToCartItem (p : Product) qty  = {
    ProductId = p.Id
    ProductName = p.Name
    UnitPrice = p.Price
    Quantity = qty
}

// add item to cart Function
// Now returns Result<Cart, string> to report errors (invalid qty, not found, out of stock)
let addItem (catalog: Map<int, Product>) (productId: int) (qty:int) (cart: Cart) : Result<Cart, string> =
    if qty <= 0 then
        Error "Quantity must be greater than 0."
    else
        match Map.tryFind productId catalog with
        | None -> Error "Product not found."
        | Some p ->
            match Map.tryFind productId cart with
            | None ->
                if qty > p.Stock then
                    Error $"Only {p.Stock} items available in stock."
                else
                    let newCart = cart |> Map.add productId (productToCartItem p qty)
                    Ok newCart
            | Some existing ->
                let newQty = existing.Quantity + qty
                if newQty > p.Stock then
                    Error $"only {p.Stock} items are available."
                                        //Error $"You requested {newQty}, but only {p.Stock} items are available."

                else
                    let updated = { existing with Quantity = newQty }
                    let newCart = cart |> Map.add productId updated
                    Ok newCart

// update quantity function
// Accepts catalog so we can validate against stock; returns Result to report errors or OK with updated cart
let updateQuantity (catalog: Map<int, Product>) (productId:int) (newQty:int) (cart: Cart) : Result<Cart, string> =
    match Map.tryFind productId catalog with
    | None -> Error "Product not found."
    | Some p ->
        if newQty > p.Stock then
            Error $"Only {p.Stock} items available."
        elif newQty <= 0 then
            // remove item if quantity <= 0
            Ok (cart |> Map.remove productId)
        else
            match Map.tryFind productId cart with
            | None -> Error "Item not in cart."
            | Some item ->
                let updated = { item with Quantity = newQty }
                Ok (cart |> Map.add productId updated)

// remove item from cart function (unchanged)
let removeFromCart (productId: int) (cart: Cart) : Cart =
    cart |> Map.remove productId

// clear the cart
let clearCart(_cart: Cart) : Cart = Map.empty

// display cart items as list
let toList (cart: Cart): CartItem list = cart |> Map.toList |> List.map snd

// calculate total price of cart
let getTotalPrice (cart: Cart) : decimal =
    cart
    |> Map.toList
    |> List.sumBy (fun (_id, item) -> item.UnitPrice * decimal item.Quantity)

// discount calculator
let applyPercentageDiscount (pct: decimal) (cart: Cart) :  decimal = 
    let total = getTotalPrice cart
    total - (total * pct / 100m)
