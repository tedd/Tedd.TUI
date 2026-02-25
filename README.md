# Tedd.TUI

**Tedd.TUI** is a high-performance, Cross-Platform Text User Interface (TUI) Framework for .NET 10, architected with WPF-inspired design patterns. It features a robust visual tree, hierarchical data binding, a recursive layout engine, and a routed event system, all optimized for zero-allocation rendering.

## Features

- **WPF-Inspired Core:** Built on a `UIElement` base with a lightweight `DependencyProperty` system and hierarchical Visual Tree.
- **Hierarchical Data Binding:** Supports `DataContext` inheritance and property binding, enabling MVVM patterns.
- **Advanced Layout Engine:** Implements a comprehensive two-pass `Measure` and `Arrange` protocol, supporting `Grid` (with Row/Col definitions), `StackPanel`, and `Border`.
- **Routed Event System:** Full support for **Bubbling** and **Tunneling** event strategies, enabling complex interaction models.
- **Rich Control Suite:** Includes `Table` (with pagination/sorting), `Grid`, `StackPanel`, `Button`, `TextBox`, `CheckBox`, `ProgressBar`, `TabControl`, and `MarkdownView`.
- **Zero-Allocation Rendering:** Designed with a philosophy of minimizing GC pressure by utilizing `Span<char>`, `stackalloc`, and double-buffered `VirtualBuffer` diffing.
- **Event-Driven Loop:** The application loop utilizes efficient OS-specific wait handles (`WaitForMultipleObjects` on Windows, poll/wait on Linux) to minimize CPU usage during inactivity.
- **Cross-Platform Architecture:** The core `Tedd.TUI` library is platform-agnostic, while `Tedd.TUI.Platform.Console` provides the concrete implementation for console environments.

## Getting Started

### Prerequisites

- .NET 10.0 SDK

### Building

```bash
cd src
dotnet build
```

### Running Tests

```bash
cd src
dotnet test
```

### Usage Example

The following example demonstrates how to create a simple MVVM-style application using `TuiApp` and Data Binding.

```csharp
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Tedd.TUI;
using Tedd.TUI.Platform.Console;

namespace MyTuiApp;

// 1. Define a ViewModel implementing INotifyPropertyChanged
public class MainViewModel : INotifyPropertyChanged
{
    private string _status = "Ready";
    private int _clickCount = 0;

    public string Status
    {
        get => _status;
        set
        {
            if (_status != value)
            {
                _status = value;
                OnPropertyChanged();
            }
        }
    }

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
        // 2. Create the main window
        var window = new TuiWindow();

        // 3. Set the DataContext for binding
        var viewModel = new MainViewModel();
        window.DataContext = viewModel;

        // 4. Initialize the application with the Console platform
        var app = new TuiApp(window);

        // 5. Define the UI layout
        var stack = new StackPanel { Orientation = Orientation.Vertical, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };

        // Title TextBlock
        var titleBlock = new TextBlock
        {
            Text = "Hello Tedd.TUI!",
            Foreground = ConsoleColor.Cyan,
            HorizontalAlignment = HorizontalAlignment.Center
        };
        stack.AddChild(titleBlock);

        // Status TextBlock with Data Binding
        var statusBlock = new TextBlock
        {
            Foreground = ConsoleColor.Green,
            HorizontalAlignment = HorizontalAlignment.Center
        };
        // Bind Text property to ViewModel.Status
        statusBlock.SetBinding(TextBlock.TextProperty, new Binding("Status"));
        stack.AddChild(statusBlock);

        // Button with Click Handler
        var button = new Button { Content = "Click Me" };
        button.Click += (s, e) =>
        {
            viewModel.OnButtonClick();
        };
        stack.AddChild(button);

        // 6. Set the window content
        window.Content = stack;

        // 7. Run the application loop
        app.Run();
    }
}
```

## Architecture

### Core System
At the heart of Tedd.TUI is the `UIElement` class, which provides the foundation for:
- **Visual Tree:** A hierarchical structure where every element (except the root) has a `Parent`. This structure is crucial for event routing and property inheritance.
- **Dependency Properties:** A property system that supports value inheritance (e.g., `DataContext` flows down the tree) and change notification. Properties are registered via `DependencyProperty.Register` and stored in a sparse dictionary on each `DependencyObject`.
- **Data Binding:** The `DataContext` property is inherited by all children in the visual tree. Elements can bind their properties to the `DataContext` using `SetBinding`, enabling clean separation of UI and logic (MVVM).

### Layout Engine
The framework utilizes a recursive two-pass layout system:
1.  **Measure Pass:** Parents query children for their `DesiredSize` based on available constraints. Elements calculate their size requirements (handling `Auto`, `Star`, and `Pixel` sizing).
2.  **Arrange Pass:** Parents position children within the final render rectangle. This phase commits the `RenderSize` and final coordinates relative to the parent.

### Input & Interaction
Tedd.TUI implements a **Routed Event** system, superior to standard .NET events for UI hierarchies:
- **Tunneling:** Events travel down from the root to the source (e.g., `PreviewKeyDown`).
- **Bubbling:** Events travel up from the source to the root (e.g., `Click`, `KeyDown`, `MouseDown`), allowing parent controls (like `ListBoxItem`) to intercept or handle events triggered by their children.
- **Event Handling:** Handlers are attached using `AddHandler` and removed with `RemoveHandler`. The `RaiseEvent` method traverses the visual tree to invoke handlers according to the routing strategy.

### Rendering Pipeline
Rendering is decoupled from the platform implementation.
- **VirtualBuffer:** The UI renders to an abstract double-buffered grid (`VirtualBuffer`).
- **Diffing Algorithm:** The renderer compares the current frame with the previous one, emitting only the changed characters and color codes to the console. This minimizes I/O operations, which are the primary bottleneck in console applications.
- **Zero-Allocation:** Heavy use of `Span<char>` and `stackalloc` ensures that the rendering loop generates minimal garbage, maintaining high throughput and low latency.
- **Event-Driven Loop:** The `TuiApp` loop uses OS primitives (`WaitForMultipleObjects` on Windows, `WaitHandle.WaitAny` on *nix) to sleep efficiently until input is received or a visual update is requested, ensuring near-zero CPU usage when idle.

### Platform Abstraction
- **Tedd.TUI (Core):** Contains the framework logic (`UIElement`, `Grid`, `Table`, etc.) and is platform-agnostic.
- **Tedd.TUI.Platform.Console:** Provides the concrete implementation of `IConsole`, the input manager, and the `TuiApp` host for terminal environments.

## XAML Support

Tedd.TUI supports defining UI in XAML. You can load XAML at runtime using `XamlLoader`.

**Example XAML (`demo.xaml`):**
```xml
<TuiWindow>
  <StackPanel Orientation="Vertical">
    <TextBlock Text="Hello from XAML!" Foreground="Cyan"/>
    <Button Name="SubmitButton" Content="Click Me" Click="OnSubmit"/>
  </StackPanel>
</TuiWindow>
```

**Loading XAML:**
```csharp
var controller = new MyController(); // Contains OnSubmit method
var window = (TuiWindow)XamlLoader.Load(File.ReadAllText("demo.xaml"), controller);
var app = new TuiApp(window);
app.Run();
```

## License

This project is licensed under the MIT License - see the LICENSE file for details.
