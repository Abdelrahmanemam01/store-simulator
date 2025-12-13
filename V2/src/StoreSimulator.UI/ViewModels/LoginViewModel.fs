namespace StoreSimulator.UI.ViewModels

open System
open System.Windows.Input
open Auth.AuthService
open Database.DatabaseService

type LoginViewModel(onLoginSuccess: int * string -> unit, ?onShowRegister: unit -> unit) =
    inherit ViewModelBase()

    let mutable usernameLocal = ""
    let mutable passwordLocal = ""
    let mutable errorMessageLocal = ""
    let mutable isLoadingLocal = false
    let showRegister = defaultArg onShowRegister (fun () -> ())

    member this.Username
        with get() = usernameLocal
        and set(v) =
            usernameLocal <- v
            this.RaisePropertyChanged("Username")
            this.ClearError()

    member this.Password
        with get() = passwordLocal
        and set(v) =
            passwordLocal <- v
            this.RaisePropertyChanged("Password")
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

    member this.LoginCommand =
        DelegateCommand(fun _ ->
            this.IsLoading <- true
            this.ErrorMessage <- ""
            
            match loginUser this.Username this.Password with
            | Ok (userId, username) ->
                this.IsLoading <- false
                onLoginSuccess (userId, username)
            | Error err ->
                this.IsLoading <- false
                this.ErrorMessage <- err
        ) :> ICommand

    member this.ShowRegisterCommand =
        DelegateCommand(fun _ -> showRegister()) :> ICommand

