module Database.Models

open System
open System.ComponentModel.DataAnnotations
open System.ComponentModel.DataAnnotations.Schema

[<AllowNullLiteral>]
[<Table("Users")>]
type User() =
    [<Key>]
    member val Id = 0 with get, set
    member val Username = "" with get, set
    [<Required>]
    member val Email = "" with get, set
    [<Required>]
    member val PasswordHash = "" with get, set
    member val CreatedAt = DateTime.UtcNow with get, set

[<AllowNullLiteral>]
[<Table("Products")>]
type ProductEntity() =
    [<Key>]
    member val Id = 0 with get, set
    member val Name = "" with get, set
    member val Category = "" with get, set
    member val Price = 0M with get, set
    member val Stock = 0 with get, set

[<AllowNullLiteral>]
[<Table("Carts")>]
type CartEntity() =
    [<Key>]
    member val Id = 0 with get, set
    member val UserId = 0 with get, set
    member val CreatedAt = DateTime.UtcNow with get, set
    member val UpdatedAt = DateTime.UtcNow with get, set

[<AllowNullLiteral>]
[<Table("CartItems")>]
type CartItemEntity() =
    [<Key>]
    member val Id = 0 with get, set
    member val CartId = 0 with get, set
    member val ProductId = 0 with get, set
    member val Quantity = 0 with get, set
    member val UnitPrice = 0M with get, set

