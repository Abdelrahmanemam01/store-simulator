namespace StoreSimulator.UI.Views

open Avalonia.Controls
open Avalonia.Markup.Xaml

type RegisterWindow() as this =
    inherit Window()
    
    do this.InitializeComponent()
    
    member this.InitializeComponent() =
        AvaloniaXamlLoader.Load(this)

