using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.IO;
using Tedd.TUI;
using Tedd.TUI.Controls;
using Tedd.TUI.Data;
using Tedd.TUI.Markup;
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

class Program
{
    static void Main(string[] args)
    {
        // Main block
        var window = new TuiWindow();
        var viewModel = new MainViewModel();
        window.DataContext = viewModel;
        var app = new TuiApp(window);

        var stack = new StackPanel
        {
            Orientation = Orientation.Vertical,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };

        var titleBlock = new TextBlock
        {
            Text = "Hello Tedd.TUI!",
            Foreground = TuiColor.Cyan, // Replaced ConsoleColor.Cyan
            HorizontalAlignment = HorizontalAlignment.Center
        };
        stack.Children.Add(titleBlock);

        var statusBlock = new TextBlock
        {
            Foreground = TuiColor.Green, // Replaced ConsoleColor.Green
            HorizontalAlignment = HorizontalAlignment.Center
        };
        statusBlock.SetBinding(TextBlock.TextProperty, new Binding("Status"));
        stack.Children.Add(statusBlock);

        var button = new Button
        {
            Content = "Click Me",
            ShadowStyle = ButtonShadowStyle.Solid,
            ShadowBackground = TuiColor.Black
        };

        button.Click += (s, e) =>
        {
            viewModel.OnButtonClick();
        };
        stack.Children.Add(button);

        window.Content = stack;

        // Note: For unit testing purposes, we do not run the app loop or it blocks.
        // app.Run();

        // XAML Load example
        var controller = new MyController(); // Contains OnSubmit method
        var windowXaml = (TuiWindow)XamlLoader.Load(File.ReadAllText("demo.xaml"), controller);
        var appXaml = new TuiApp(windowXaml);
        // appXaml.Run();
    }
}
