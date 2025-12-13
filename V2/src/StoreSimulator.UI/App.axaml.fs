namespace StoreSimulator.UI

open Avalonia
open Avalonia.Controls
open Avalonia.Controls.ApplicationLifetimes
open Avalonia.Data.Core
open Avalonia.Data.Core.Plugins
open Avalonia.Markup.Xaml
open StoreSimulator.UI.ViewModels
open StoreSimulator.UI.Views
open Database.DatabaseService

type App() =
    inherit Application()

    override this.Initialize() =
            AvaloniaXamlLoader.Load(this)

    override this.OnFrameworkInitializationCompleted() =

        // Line below is needed to remove Avalonia data validation.
        // Without this line you will get duplicate validations from both Avalonia and CT
        BindingPlugins.DataValidators.RemoveAt(0)

        // Initialize database
        initializeDatabase()

        match this.ApplicationLifetime with
        | :? IClassicDesktopStyleApplicationLifetime as desktop ->
             // Show login window first
             let mutable loginWindow: LoginWindow option = None
             let mutable registerWindow: RegisterWindow option = None
             
             let showMainWindow (userId, username) =
                 try
                     System.Console.WriteLine($"=== LOGIN SUCCESS: UserId={userId}, Username={username} ===")
                     
                     // Create MainWindowViewModel first
                     System.Console.WriteLine("Step 1: Creating MainWindowViewModel...")
                     let viewModel = MainWindowViewModel(userId)
                     System.Console.WriteLine("✓ MainWindowViewModel created!")
                     
                     // Create MainWindow
                     System.Console.WriteLine("Step 2: Creating MainWindow...")
                     let mainWindow = MainWindow(DataContext = viewModel)
                     System.Console.WriteLine("✓ MainWindow created!")
                     
                     // IMPORTANT: Set MainWindow BEFORE closing login window
                     // This prevents the app from exiting when login window closes
                     System.Console.WriteLine("Step 3: Setting MainWindow as desktop.MainWindow...")
                     desktop.MainWindow <- mainWindow
                     System.Console.WriteLine("✓ MainWindow set as desktop.MainWindow!")
                     
                     // Configure window properties
                     mainWindow.WindowState <- Avalonia.Controls.WindowState.Normal
                     mainWindow.ShowInTaskbar <- true
                     mainWindow.CanResize <- true
                     
                     // Show the window
                     System.Console.WriteLine("Step 4: Showing MainWindow...")
                     mainWindow.Show()
                     System.Console.WriteLine("✓ MainWindow.Show() called!")
                     
                     // Activate and bring to front
                     mainWindow.Activate()
                     mainWindow.Focus() |> ignore
                     
                     System.Console.WriteLine($"Step 5: Window properties - IsVisible={mainWindow.IsVisible}, IsActive={mainWindow.IsActive}")
                     System.Console.WriteLine("=== MAIN WINDOW IS NOW VISIBLE ===")
                     
                     // Now close login and register windows (safe because MainWindow is set)
                     System.Console.WriteLine("Step 6: Closing login/register windows...")
                     loginWindow |> Option.iter (fun w -> 
                         w.Hide()
                         w.Close()
                     )
                     registerWindow |> Option.iter (fun w -> 
                         w.Hide()
                         w.Close()
                     )
                     System.Console.WriteLine("✓ Login/register windows closed!")
                     
                 with
                 | ex -> 
                     // Log the error
                     let errorMsg = $"Error showing main window: {ex.Message}\n\nStack trace:\n{ex.StackTrace}"
                     System.Console.WriteLine(errorMsg)
                     System.Diagnostics.Debug.WriteLine(errorMsg)
                     
                     // Show error dialog to user
                     let errorWindow = Window(
                         Title = "Error - Cannot Open Store", 
                         Width = 500.0, 
                         Height = 300.0,
                         WindowStartupLocation = WindowStartupLocation.CenterScreen
                     )
                     let errorPanel = StackPanel(Margin = Thickness(20.0))
                     
                     let titleBlock = TextBlock(
                         Text = "Failed to open store window",
                         FontSize = 18.0,
                         FontWeight = Avalonia.Media.FontWeight.Bold,
                         Foreground = Avalonia.Media.Brushes.Red,
                         Margin = Thickness(0.0, 0.0, 0.0, 10.0)
                     )
                     
                     let msgBlock = TextBlock(
                         Text = $"Error: {ex.Message}\n\nPlease check the console for details.",
                         TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                         Foreground = Avalonia.Media.Brushes.White,
                         Margin = Thickness(0.0, 0.0, 0.0, 15.0)
                     )
                     
                     let okButton = Button(
                         Content = "OK",
                         Width = 100.0,
                         HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center
                     )
                     okButton.Click.Add(fun _ -> errorWindow.Close())
                     
                     errorPanel.Children.Add(titleBlock)
                     errorPanel.Children.Add(msgBlock)
                     errorPanel.Children.Add(okButton)
                     errorWindow.Content <- errorPanel
                     errorWindow.Background <- Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.FromRgb(0x0Auy, 0x0Auy, 0x0Fuy))
                     errorWindow.Show()
                     
                     // Show login window again
                     loginWindow |> Option.iter (fun w -> w.Show())
             
             let showRegisterWindow () =
                 let registerVm = RegisterViewModel(
                     showMainWindow,
                     fun () ->
                         registerWindow |> Option.iter (fun w -> w.Close())
                         loginWindow |> Option.iter (fun w -> w.Show())
                 )
                 let regWindow = RegisterWindow(DataContext = registerVm)
                 registerWindow <- Some regWindow
                 regWindow.Show()
                 loginWindow |> Option.iter (fun w -> w.Hide())
             
             let loginVm = LoginViewModel(showMainWindow, showRegisterWindow)
             let logWindow = LoginWindow(DataContext = loginVm)
             loginWindow <- Some logWindow
             desktop.MainWindow <- logWindow
        | _ -> ()

        base.OnFrameworkInitializationCompleted()
