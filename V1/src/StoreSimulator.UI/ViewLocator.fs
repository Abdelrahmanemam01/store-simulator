namespace StoreSimulator.UI

open System
open Avalonia.Controls
open Avalonia.Controls.Templates
open StoreSimulator.UI.ViewModels
open StoreSimulator.UI.Views

type ViewLocator() =
    interface IDataTemplate with
        member _.Build(data: obj) =
            match data with
            | :? MainWindowViewModel -> upcast MainWindow()
            | _ -> null

        member _.Match(data: obj) =
            data :? ViewModelBase
