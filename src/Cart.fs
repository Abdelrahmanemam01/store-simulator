module Cart

open Catalog

type CartItem = {
    ProductId : int
    ProductName: string
    Unitprice: decimal
    Quantity : int
}

type Cart = Map<int , CartItem>

let empty : Cart = Map.empty

let private productToCartItem (p : Product) qty  = {
    ProductId = p.Id
    ProductName = p.Name
    Unitprice = p.Price
    Quantity = qty
}

// add item to cart Function
let addItem (catalog: Map<int, Product>) (productId: int) (qty:int) (cart: Cart) : Cart =
    if qty <= 0 then cart
    else
        match Map.tryFind productId catalog with
        | None -> cart
        | Some p ->
            match Map.tryFind productId cart with
            | None -> cart |> Map.add productId (productToCartItem p qty)
            | Some existing ->
                let updated = { existing with Quantity = existing.Quantity + qty }
                cart |> Map.add productId updated 

// update  quantity function
let updateQuantity (productId:int) (newQty:int) (cart: Cart) : Cart =
    if newQty <= 0 then
        cart |> Map.remove productId
    else
        cart
        |> Map.change productId (fun opt ->
            match opt with
            | None -> None
            | Some item -> Some { item with Quantity = newQty })

// remove item from cart function
let removeFromCart (productId: int) (cart: Cart) : Cart =
    cart |> Map.remove productId

// clear the cart
let clearCart(_cart: Cart) : Cart = Map.empty

//display cart items as list
let toList (cart: Cart): CartItem list = cart |> Map.toList |> List.map snd

// calculate total price of cart
let getTotalPrice (cart: Cart) : decimal =
    cart
    |> Map.toList
    |> List.sumBy (fun (_id, item) -> item.Unitprice * decimal item.Quantity)

//descount calculator
let applyPrecentageDiscount (pct: decimal) (cart: Cart) :  decimal = 
     let total = getTotalPrice cart
     total - (total * pct / 100m)
