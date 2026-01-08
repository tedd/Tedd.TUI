# Tedd.TUI

**Tedd.TUI** is a Cross-Platform Text User Interface (TUI) Framework for .NET 10, inspired by WPF architecture. It provides a visual tree, dependency properties, a recursive layout engine (Measure/Arrange), and decoupled rendering to support multiple backends (Console, HTML, etc.).

## Features

- **WPF-Inspired Architecture:** Uses a `UIElement` base class with a lightweight `DependencyProperty` system.
- **Layout System:** Implements a two-pass `Measure` and `Arrange` layout protocol.
- **Visual Tree:** Supports nesting of controls like `Border`, `StackPanel`, etc.
- **Controls:** Includes `TextBlock`, `Border`, `StackPanel`, `Button`.
- **XAML Support:** Includes a basic `XamlLoader` to parse declarative UI definitions.
- **Decoupled Rendering:** Renders to a `VirtualBuffer` intermediate representation.

## Getting Started

### Prerequisites

- .NET 8.0 SDK (Targeting .NET 10 compatible architecture)

### Building

```bash
dotnet build
```

### Running Tests

```bash
dotnet test
```

### Usage Example

```csharp
using Tedd.TUI;

// Create a window
var window = new TuiWindow();

// Create content
var stack = new StackPanel { Orientation = Orientation.Vertical };
stack.AddChild(new TextBlock { Text = "Hello TeddUI!", Foreground = ConsoleColor.Cyan });
stack.AddChild(new Button { Content = "Click Me" });

window.Content = stack;

// Render
var buffer = new VirtualBuffer(80, 24);
window.Measure(new Size(80, 24));
window.Arrange(new Rect(0, 0, 80, 24));
window.Render(buffer);

// Output buffer to console (using ConsoleRenderer)
var renderer = new ConsoleRenderer();
renderer.Render(buffer);
```

## Architecture

1.  **Phase 1: Core Architecture**: `UIElement`, `DependencyObject`, `Geometry`.
2.  **Phase 2: Component Library**: Basic controls and layout containers.
3.  **Phase 3: Rendering Pipeline**: `VirtualBuffer` and `IRenderer`.
4.  **Phase 4: XAML & DataBinding**: Declarative UI and MVVM support foundation.

## License

This project is licensed under the MIT License - see the LICENSE file for details.
