using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Tedd.TUI;
using Tedd.TUI.Platform.Console;

namespace MyTuiApp;

// 1. Define a ViewModel implementing INotifyPropertyChanged
public class MainViewModel : INotifyPropertyChanged
{
    private int _clickCount = 0;

    public string Status
    {
        get => field;
        set
        {
            if (field != value)
            {
                field = value;
                OnPropertyChanged();
            }
        }
    } = "Ready";

    public void OnButtonClick()
    {
        _clickCount++;
        Status = $"Button Clicked {_clickCount} times!";
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

class Program
{
    public static void DummyMain(string[] args)
    {
        // 2. Create the main window
        var window = new TuiWindow();

        // 3. Set the DataContext for binding
        var viewModel = new MainViewModel();
        window.DataContext = viewModel;

        // 4. Initialize the application with the Console platform
        var app = new TuiApp(window);

        // 5. Define the UI layout
        var stack = new StackPanel
        {
            Orientation = Orientation.Vertical,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };

        // Title TextBlock
        var titleBlock = new TextBlock
        {
            Text = "Hello Tedd.TUI!",
            Foreground = ConsoleColor.Cyan,
            HorizontalAlignment = HorizontalAlignment.Center
        };
        stack.Children.Add(titleBlock); // UIElementCollection sets Parent automatically

        // Status TextBlock with Data Binding
        var statusBlock = new TextBlock
        {
            Foreground = ConsoleColor.Green,
            HorizontalAlignment = HorizontalAlignment.Center
        };
        // Bind Text property to ViewModel.Status
        statusBlock.SetBinding(TextBlock.TextProperty, new Binding("Status"));
        stack.Children.Add(statusBlock);

        // Button with Click Handler
        var button = new Button { Content = "Click Me" };

        // Subscribe to the Click event (Routed Event)
        button.Click += (s, e) =>
        {
            viewModel.OnButtonClick();
        };
        stack.Children.Add(button);

        // 6. Set the window content
        window.Content = stack;

        // 7. Run the application loop
        app.Run();
    }
}

class MyController
{
    // Field name matches x:Name in XAML for injection
    public Button SubmitButton = null!;

    // Method name matches Click handler in XAML
    public void OnSubmit(object sender, RoutedEventArgs e)
    {
        // Handle click
    }
}

class Program2
{
    public static void DummyMain()
    {
        var controller = new MyController(); // Contains OnSubmit method
        var window = (TuiWindow)XamlLoader.Load("<TuiWindow></TuiWindow>", controller);
        var app = new TuiApp(window);
        app.Run();
    }
}
