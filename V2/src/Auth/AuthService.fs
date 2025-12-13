module Auth.AuthService

open System
open System.Linq
open Microsoft.EntityFrameworkCore
open Database.StoreDbContext
open Database.Models
open BCrypt.Net

// Register a new user
let registerUser (username: string) (email: string) (password: string) : Result<int, string> =
    if String.IsNullOrWhiteSpace(username) then
        Error "Username cannot be empty."
    elif String.IsNullOrWhiteSpace(email) then
        Error "Email cannot be empty."
    elif String.IsNullOrWhiteSpace(password) || password.Length < 6 then
        Error "Password must be at least 6 characters long."
    else
        use context = StoreDbContext.CreateContext()
        
        // Check if username already exists
        let usernameExists = 
            query {
                for u in context.Users do
                where (u.Username = username)
                select u
            } |> Seq.isEmpty |> not
        
        if usernameExists then
            Error "Username already exists."
        else
            // Check if email already exists
            let emailExists = 
                query {
                    for u in context.Users do
                    where (u.Email = email)
                    select u
                } |> Seq.isEmpty |> not
            
            if emailExists then
                Error "Email already exists."
            else
                let passwordHash = BCrypt.HashPassword(password)
                let newUser = User()
                newUser.Username <- username
                newUser.Email <- email
                newUser.PasswordHash <- passwordHash
                newUser.CreatedAt <- DateTime.UtcNow
                
                let added = context.Users.Add(newUser)
                context.SaveChanges() |> ignore
                Ok added.Entity.Id

// Login user
let loginUser (username: string) (password: string) : Result<int * string, string> =
    if String.IsNullOrWhiteSpace(username) then
        Error "Username cannot be empty."
    elif String.IsNullOrWhiteSpace(password) then
        Error "Password cannot be empty."
    else
        use context = StoreDbContext.CreateContext()
        
        let user = 
            query {
                for u in context.Users do
                where (u.Username = username)
                select u
                take 1
            } |> Seq.tryHead
        
        match user with
        | None -> Error "Invalid username or password."
        | Some u ->
            if BCrypt.Verify(password, u.PasswordHash) then
                Ok (u.Id, u.Username)
            else
                Error "Invalid username or password."

// Get user by ID
let getUserById (userId: int) : User option =
    use context = StoreDbContext.CreateContext()
    query {
        for u in context.Users do
        where (u.Id = userId)
        select u
        take 1
    } |> Seq.tryHead

