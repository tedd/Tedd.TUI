using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Tedd.TUI;
using Tedd.TUI.Platform.Console;

namespace MyTuiApp;

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
    static void Main(string[] args)
    {
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
            Foreground = ConsoleColor.Cyan,
            HorizontalAlignment = HorizontalAlignment.Center
        };
        stack.Children.Add(titleBlock);
        var statusBlock = new TextBlock
        {
            Foreground = ConsoleColor.Green,
            HorizontalAlignment = HorizontalAlignment.Center
        };
        statusBlock.SetBinding(TextBlock.TextProperty, new Binding("Status"));
        stack.Children.Add(statusBlock);
        var button = new Button { Content = "Click Me" };
        button.Click += (s, e) =>
        {
            viewModel.OnButtonClick();
        };
        stack.Children.Add(button);
        window.Content = stack;
        app.Run();
    }
}
