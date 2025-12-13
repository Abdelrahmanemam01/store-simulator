namespace StoreSimulator.UI.ViewModels

open System
open System.ComponentModel
open System.Windows.Input

/// Base class that implements INotifyPropertyChanged
type ViewModelBase() =
    let propertyChanged = Event<PropertyChangedEventHandler, PropertyChangedEventArgs>()

    interface INotifyPropertyChanged with
        [<CLIEvent>]
        member _.PropertyChanged = propertyChanged.Publish

    member this.RaisePropertyChanged(propertyName: string) =
        propertyChanged.Trigger(this, PropertyChangedEventArgs(propertyName))

/// Simple ICommand implementation we will use instead of ReactiveCommand
type DelegateCommand(action: obj -> unit, ?canExecute: obj -> bool) =
    let canExecuteChanged = Event<EventHandler, EventArgs>()

    interface ICommand with
        [<CLIEvent>]
        member _.CanExecuteChanged = canExecuteChanged.Publish

        member _.CanExecute(parameter: obj) =
            match canExecute with
            | Some f -> f parameter
            | None -> true

        member _.Execute(parameter: obj) =
            action parameter

    member this.RaiseCanExecuteChanged() =
        canExecuteChanged.Trigger(this, EventArgs.Empty)
