namespace StoreSimulator.UI.Views

open Avalonia.Controls
open Avalonia.Markup.Xaml
open StoreSimulator.UI.ViewModels

type LoginWindow() as this =
    inherit Window()
    
    do this.InitializeComponent()
    
    member this.InitializeComponent() =
        AvaloniaXamlLoader.Load(this)

