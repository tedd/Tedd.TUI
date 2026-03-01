# Tedd.TUI

**Tedd.TUI** is a high-performance, Cross-Platform Text User Interface (TUI) Framework for .NET 10, architected with WPF-inspired design patterns. It features a robust visual tree, hierarchical data binding, a recursive layout engine, and an event system, all optimized for zero-allocation rendering.

## Features

- **WPF-Inspired Core:** Built on a `UIElement` base with a lightweight `DependencyProperty` system and hierarchical Visual Tree.
- **Advanced Layout Engine:** Implements a comprehensive two-pass `Measure` and `Arrange` protocol.
  - **Grid:** Supports `RowDefinition`, `ColumnDefinition`, `Star` (*) sizing, and `Auto` sizing.
  - **StackPanel:** Vertical and horizontal stacking.
  - **Border:** Decorative borders with box-drawing characters.
- **Rich Control Suite:**
  - **DataGrid:** Supports `ItemsSource` binding, `AutoGenerateColumns`, selection, and pagination.
  - **Table:** Manual row management with sorting, pagination, and header customization.
  - **MarkdownView:** Renders Markdown content with theming support.
  - **Standard Controls:** `Button`, `TextBox`, `CheckBox`, `RadioButton`, `ProgressBar`, `TabControl`, `ListBox`, `ComboBox`, `DockPanel`.
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
```

## Architecture

### Core System
At the heart of Tedd.TUI is the `UIElement` class, which provides the foundation for:
- **Visual Tree:** A hierarchical structure of elements allowing for complex composition.
- **Dependency Properties:** A property system that supports value inheritance, change notification, and memory conservation.

### Data Binding
Tedd.TUI supports a hierarchical data binding system analogous to WPF, driven by the `DataContext` inherited dependency property.
- **DataContext Inheritance:** The `DataContext` property is automatically inherited down the visual tree. Assigning a `DataContext` at the root (e.g., `TuiWindow`) seamlessly propagates the data model to all descendant elements, eliminating the need for redundant assignments.
- **INotifyPropertyChanged:** Models must implement `System.ComponentModel.INotifyPropertyChanged` to trigger reactive UI updates.
- **Binding Resolutions:** The `SetBinding` method establishes a dynamic link between a target dependency property and a source property. Bindings primarily resolve against the current `DataContext`, but the framework also supports `RelativeSource` resolutions (e.g., `Self`, `TemplatedParent`, `FindAncestor`) for complex control templates.
- **Collections:** Utilizing `DataGrid` or derivatives of the `Selector` class (`ListBox`, `ComboBox`, `TabControl`) enables binding directly to collections via the `ItemsSource` property, complete with `DisplayMemberPath` text resolution.

### Layout Engine
The framework employs a robust, recursive two-pass layout system orchestrated by the abstract `Panel` class:
1.  **Measure Pass:** Container elements recursively query their children, invoking `Measure(Size availableSize)` to compute their `DesiredSize` based on layout constraints.
2.  **Arrange Pass:** Parents position and size their children within the computed physical bounds by invoking `Arrange(Rect finalRect)`.
3.  **Render Pass:** The actual rendering to the `VirtualBuffer` is heavily optimized. Containers executing the `Render` method utilize clipping rects to skip elements that are fully clipped or lie completely outside the current clip rectangle, drastically reducing CPU cycles in complex visual trees.

**Hierarchical Composition:** For container controls descending from `Panel` (such as `StackPanel`, `Grid`, `DockPanel`, `WrapPanel`, and `Canvas`), the underlying `UIElementCollection` (`Children`) systematically intercepts collection modifications. Executing `Panel.Children.Add(child)` strictly enforces visual tree integrity by automatically assigning the parent node, which inherently triggers `DataContext` propagation and establishes the routing infrastructure for input events.

### Input & Interaction
Input handling is driven by a deterministic Routed Event architecture defined within `UIElement`:
- **Standard Input Events:** Primitive interactions (`KeyDown`, `KeyUp`, `MouseDown`, `MouseUp`, `GotFocus`, `LostFocus`) are registered as bubbling `RoutedEvent` instances. They originate at the focused element or visual leaf node and systematically traverse upwards to the visual root.
- **Class Handlers:** `UIElement` exposes virtual methods (e.g., `OnKeyDown`, `OnMouseDown`) that function as internal class handlers, allowing derived controls to intercept and handle core inputs prior to instance-level event delegates. Furthermore, input coordinate resolution dynamically translates absolute global coordinates (`GlobalX`, `GlobalY`) into relative local spaces (`X`, `Y`) during event propagation via `InvokeHandler`.
- **High-Level Abstractions:** Semantic control events (e.g., `Button.ClickEvent`) seamlessly integrate into the identical bubbling routing topology, ensuring uniform event interception and traversal behavior across component boundaries.

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
