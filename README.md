# Tedd.TUI

**Tedd.TUI** is a high-performance, Cross-Platform Text User Interface (TUI) Framework for .NET 10, architected with WPF-inspired design patterns. It features a robust visual tree, dependency properties, a recursive layout engine, and a routed event system, all optimized for zero-allocation rendering.

## Features

- **WPF-Inspired Core:** Built on a `UIElement` base with a lightweight `DependencyProperty` system and hierarchical Visual Tree.
- **Advanced Layout Engine:** Implements a comprehensive two-pass `Measure` and `Arrange` protocol.
  - **Grid:** Supports `RowDefinition`, `ColumnDefinition`, `Star` (*) sizing, and `Auto` sizing.
  - **StackPanel:** Vertical and horizontal stacking.
  - **Border:** Decorative borders with box-drawing characters.
- **Rich Control Suite:**
  - **Table:** Features sorting, pagination, header customization, and data binding support.
  - **MarkdownView:** Renders Markdown content with theming support.
  - **Standard Controls:** `Button`, `TextBox`, `CheckBox`, `RadioButton`, `ProgressBar`, `TabControl`, `ListBox`, `ComboBox`.
- **Routed Event System:** Full support for **Bubbling** and **Tunneling** event strategies.
- **Data Binding:** Hierarchical `DataContext` inheritance with `INotifyPropertyChanged` support.
- **High Performance:** Designed with a "Zero-Allocation" rendering philosophy, utilizing `Span<char>`, `stackalloc`, and double-buffered `VirtualBuffer` diffing to minimize I/O and GC pressure.
- **Cross-Platform:** Decoupled rendering pipeline supporting Console (Windows/Linux/Mac).

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

The following example demonstrates how to create a simple application using `TuiApp` and `TuiWindow`.

```csharp
using System;
using Tedd.TUI;
using Tedd.TUI.Platform.Console;

class Program
{
    static void Main(string[] args)
    {
        // 1. Create the main window
        var window = new TuiWindow();

        // 2. Initialize the application with the Console platform
        var app = new TuiApp(window);

        // 3. Define the UI layout
        var stack = new StackPanel { Orientation = Orientation.Vertical };

        stack.AddChild(new TextBlock
        {
            Text = "Hello Tedd.TUI!",
            Foreground = ConsoleColor.Cyan,
            HorizontalAlignment = HorizontalAlignment.Center
        });

        var button = new Button { Content = "Click Me" };

        // Subscribe to the Bubbling Click event
        button.Click += (s, e) =>
        {
            window.Content = new TextBlock
            {
                Text = "Button Clicked!",
                HorizontalAlignment = HorizontalAlignment.Center,
                Foreground = ConsoleColor.Green
            };
        };
        stack.AddChild(button);

        // 4. Set the window content
        window.Content = stack;

        // 5. Run the application loop
        app.Run();
    }
}
```

## Architecture

### Core System
At the heart of Tedd.TUI is the `UIElement` class, which provides the foundation for:
- **Visual Tree:** A hierarchical structure of elements allowing for complex composition.
- **Dependency Properties:** A property system that supports value inheritance, change notification, and memory conservation.

### Data Binding
Tedd.TUI supports a hierarchical data binding system similar to WPF.
- **DataContext:** The `DataContext` property is inherited down the visual tree.
- **INotifyPropertyChanged:** Models should implement `System.ComponentModel.INotifyPropertyChanged` to drive UI updates.
- **Binding:** Use `SetBinding` to link a dependency property to a property on the `DataContext`.

**Example:**
```csharp
using System.ComponentModel;
using Tedd.TUI;

public class ViewModel : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler PropertyChanged;
    private string _status = "Ready";

    public string Status
    {
        get => _status;
        set
        {
            _status = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Status)));
        }
    }
}

// In your application setup:
var vm = new ViewModel();
window.DataContext = vm;

var statusText = new TextBlock();
// Bind TextBlock.Text to ViewModel.Status
statusText.SetBinding(TextBlock.TextProperty, new Binding("Status"));
```

### Layout Engine
The framework utilizes a recursive two-pass layout system:
1.  **Measure Pass:** Parents query children for their `DesiredSize` based on available constraints. `Grid` calculates `Star` (*) sizing during this pass based on available space.
2.  **Arrange Pass:** Parents position children within the final render rectangle.

### Input & Interaction
Tedd.TUI implements a **Routed Event** system, superior to standard .NET events for UI hierarchies:
- **Tunneling:** Events travel down from the root to the source (e.g., `PreviewKeyDown`).
- **Bubbling:** Events travel up from the source to the root (e.g., `Click`, `KeyDown`), allowing parent controls to handle events from their children.

### Rendering Pipeline
Rendering is decoupled from the platform implementation.
- **VirtualBuffer:** The UI renders to an abstract double-buffered grid.
- **Diffing Algorithm:** The renderer compares the current frame with the previous one, emitting only the changed characters and color codes to the console.
- **Optimization:** Heavy use of `Span<char>` and stack allocations ensures that the rendering loop generates minimal garbage, maintaining high throughput.
- **Event-Driven Loop:** The application loop utilizes efficient OS-specific wait handles (`WaitForMultipleObjects` on Windows, `WaitHandle` on Linux) to minimize CPU usage during inactivity.

### XAML Support

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

**Controller:**
```csharp
class MyController
{
    // Field name matches x:Name in XAML for injection
    public Button SubmitButton;

    // Method name matches Click handler in XAML
    public void OnSubmit(object sender, RoutedEventArgs e)
    {
        // Handle click
    }
}
```

## License

This project is licensed under the MIT License - see the LICENSE file for details.
