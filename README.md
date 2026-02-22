# Tedd.TUI

**Tedd.TUI** is a high-performance, Cross-Platform Text User Interface (TUI) Framework for .NET 10, architected with WPF-inspired design patterns. It features a robust visual tree, dependency properties, a recursive layout engine, and a routed event system, all optimized for zero-allocation rendering.

## Features

- **WPF-Inspired Core:** Built on a `UIElement` base with a lightweight `DependencyProperty` system and hierarchical Visual Tree.
- **Advanced Layout Engine:** Implements a comprehensive two-pass `Measure` and `Arrange` protocol, supporting `Grid` (with Row/Col definitions), `StackPanel`, and `Border`.
- **Routed Event System:** Full support for **Bubbling** and **Tunneling** event strategies, enabling complex interaction models.
- **Rich Control Suite:** Includes `Table` (with pagination/sorting), `Grid`, `StackPanel`, `Button`, `TextBox`, `CheckBox`, `ProgressBar`, `TabControl`, and `MarkdownView`.
- **High Performance:** Designed with a "Zero-Allocation" rendering philosophy, utilizing `Span<char>`, `stackalloc`, and double-buffered `VirtualBuffer` diffing to minimize I/O and GC pressure.
- **Cross-Platform:** Decoupled rendering pipeline supporting Console (Windows/Linux/Mac) and extensible for other backends (e.g., Blazor).

## Getting Started

### Prerequisites

- .NET 10.0 SDK

### Building

```bash
dotnet build
```

### Running Tests

```bash
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
        button.Click += (s, e) =>
        {
            window.Content = new TextBlock
            {
                Text = "Button Clicked!",
                HorizontalAlignment = HorizontalAlignment.Center
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
- **Dependency Properties:** A property system that supports value inheritance and change notification.
- **DataContext:** Built-in support for data binding contexts, paving the way for MVVM patterns.

### Layout Engine
The framework utilizes a recursive two-pass layout system similar to WPF:
1.  **Measure Pass:** Parents query children for their `DesiredSize` based on available constraints.
2.  **Arrange Pass:** Parents position children within the final render rectangle.

### Input & Interaction
Tedd.TUI implements a **Routed Event** system, superior to standard .NET events for UI hierarchies:
- **Tunneling:** Events travel down from the root to the source (e.g., PreviewKeyDown).
- **Bubbling:** Events travel up from the source to the root (e.g., Click, KeyDown), allowing parent controls (like ListBoxItems) to handle events from their children.

### Rendering Pipeline
Rendering is decoupled from the platform implementation.
- **VirtualBuffer:** The UI renders to an abstract double-buffered grid.
- **Diffing Algorithm:** The renderer compares the current frame with the previous one, emitting only the changed characters and color codes to the console.
- **Optimization:** Heavy use of `Span<char>` and stack allocations ensures that the rendering loop generates minimal garbage, maintaining high throughput even on lower-end hardware.

### Roadmap / Future Capabilities
- **XAML Support:** Basic `XamlLoader` exists, with plans to expand into full XAML parsing and instantiation.
- **Advanced DataBinding:** Enhancing the current `Binding` infrastructure to support complex paths and converters.

## License

This project is licensed under the MIT License - see the LICENSE file for details.
