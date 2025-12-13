namespace StoreSimulator.UI.ViewModels

open System
open System.Windows.Input
open Auth.AuthService

type RegisterViewModel(onRegisterSuccess: int * string -> unit, onBackToLogin: unit -> unit) =
    inherit ViewModelBase()

    let mutable usernameLocal = ""
    let mutable emailLocal = ""
    let mutable passwordLocal = ""
    let mutable confirmPasswordLocal = ""
    let mutable errorMessageLocal = ""
    let mutable isLoadingLocal = false

    member this.Username
        with get() = usernameLocal
        and set(v) =
            usernameLocal <- v
            this.RaisePropertyChanged("Username")
            this.ClearError()

    member this.Email
        with get() = emailLocal
        and set(v) =
            emailLocal <- v
            this.RaisePropertyChanged("Email")
            this.ClearError()

    member this.Password
        with get() = passwordLocal
        and set(v) =
            passwordLocal <- v
            this.RaisePropertyChanged("Password")
            this.ClearError()

    member this.ConfirmPassword
        with get() = confirmPasswordLocal
        and set(v) =
            confirmPasswordLocal <- v
            this.RaisePropertyChanged("ConfirmPassword")
            this.ClearError()

    member this.ErrorMessage
        with get() = errorMessageLocal
        and set(v) =
            errorMessageLocal <- v
            this.RaisePropertyChanged("ErrorMessage")

    member this.IsLoading
        with get() = isLoadingLocal
        and set(v) =
            isLoadingLocal <- v
            this.RaisePropertyChanged("IsLoading")

    member private this.ClearError() =
        if not (String.IsNullOrEmpty this.ErrorMessage) then
            this.ErrorMessage <- ""

    member this.RegisterCommand =
        DelegateCommand(fun _ ->
            this.IsLoading <- true
            this.ErrorMessage <- ""
            
            if String.IsNullOrWhiteSpace this.Username then
                this.IsLoading <- false
                this.ErrorMessage <- "Username cannot be empty."
            elif String.IsNullOrWhiteSpace this.Email then
                this.IsLoading <- false
                this.ErrorMessage <- "Email cannot be empty."
            elif String.IsNullOrWhiteSpace this.Password then
                this.IsLoading <- false
                this.ErrorMessage <- "Password cannot be empty."
            elif this.Password <> this.ConfirmPassword then
                this.IsLoading <- false
                this.ErrorMessage <- "Passwords do not match."
            else
                match registerUser this.Username this.Email this.Password with
                | Ok userId ->
                    this.IsLoading <- false
                    // Auto-login after registration
                    match loginUser this.Username this.Password with
                    | Ok (uid, uname) -> onRegisterSuccess (uid, uname)
                    | Error err -> this.ErrorMessage <- $"Registration successful but login failed: {err}"
                | Error err ->
                    this.IsLoading <- false
                    this.ErrorMessage <- err
        ) :> ICommand

    member this.BackToLoginCommand =
        DelegateCommand(fun _ -> onBackToLogin()) :> ICommand

