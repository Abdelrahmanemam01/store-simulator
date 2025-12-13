namespace StoreSimulator.UI.Views

open Avalonia
open Avalonia.Controls
open Avalonia.Markup.Xaml

type MainWindow () as this = 
    inherit Window ()

    do 
        System.Console.WriteLine("MainWindow constructor called!")
        this.InitializeComponent()
        System.Console.WriteLine("MainWindow InitializeComponent completed!")
        
        // Ensure window properties are set
        this.ShowInTaskbar <- true
        this.CanResize <- true
        this.WindowState <- WindowState.Normal
    
    member private this.InitializeComponent() =
#if DEBUG
        this.AttachDevTools()
#endif
        System.Console.WriteLine("Loading MainWindow XAML...")
        AvaloniaXamlLoader.Load(this)
        System.Console.WriteLine("MainWindow XAML loaded successfully!")
