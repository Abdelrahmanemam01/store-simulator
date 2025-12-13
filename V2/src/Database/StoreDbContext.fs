module Database.StoreDbContext

open Microsoft.EntityFrameworkCore
open Database.Models

type StoreDbContext(options: DbContextOptions<StoreDbContext>) =
    inherit DbContext(options)

    [<DefaultValue>]
    val mutable users: DbSet<User>
    member this.Users
        with get() = this.users
        and set v = this.users <- v

    [<DefaultValue>]
    val mutable products: DbSet<ProductEntity>
    member this.Products
        with get() = this.products
        and set v = this.products <- v

    [<DefaultValue>]
    val mutable carts: DbSet<CartEntity>
    member this.Carts
        with get() = this.carts
        and set v = this.carts <- v

    [<DefaultValue>]
    val mutable cartItems: DbSet<CartItemEntity>
    member this.CartItems
        with get() = this.cartItems
        and set v = this.cartItems <- v

    override this.OnModelCreating(modelBuilder: ModelBuilder) =
        base.OnModelCreating(modelBuilder)
        
        modelBuilder.Entity<User>()
            .HasIndex([| "Username" |])
            .IsUnique() |> ignore
            
        modelBuilder.Entity<User>()
            .HasIndex([| "Email" |])
            .IsUnique() |> ignore

        modelBuilder.Entity<CartItemEntity>()
            .HasIndex([| "CartId"; "ProductId" |])
            .IsUnique() |> ignore

    static member CreateContext() =
        let optionsBuilder = DbContextOptionsBuilder<StoreDbContext>()
        optionsBuilder.UseSqlite("Data Source=store.db") |> ignore
        new StoreDbContext(optionsBuilder.Options)

