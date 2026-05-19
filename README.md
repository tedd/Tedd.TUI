# Tedd.TUI

**Tedd.TUI** is a high-performance, Cross-Platform Text User Interface (TUI) Framework for .NET 10, architected with WPF-inspired design patterns. It features a robust visual tree, hierarchical data binding, a recursive layout engine, and an event system, all optimized for zero-allocation rendering.

## Features

- **WPF-Inspired Core:** Built on a `UIElement` base with a lightweight `DependencyProperty` system and hierarchical Visual Tree.
- **Advanced Layout Engine:** Implements a comprehensive two-pass `Measure` and `Arrange` protocol.
  - **Grid:** Supports `RowDefinition`, `ColumnDefinition`, `Star` (*) sizing, and `Auto` sizing.
  - **UniformGrid:** Symmetrical grid layouts via `Rows` and `Columns` dependency properties.
  - **StackPanel:** Vertical and horizontal stacking.
  - **WrapPanel:** Sequential layout with line/column wrapping.
  - **DockPanel:** Edge-docking arrangements using the `Dock` attached property.
  - **Canvas:** Absolute positioning via `Canvas.Left` and `Canvas.Top` attached properties.
  - **ScrollViewer:** Unbounded layout constraints allowing `Panel` contents to evaluate bounds up to `int.MaxValue`, coupled with `ScrollBar` for navigation. Scrollbar visibility is configured through the `ScrollBarVisibility` enum (`Disabled`, `Auto`, `Visible`) on both `HorizontalScrollBarVisibility` and `VerticalScrollBarVisibility`. `Disabled` clamps the content to the viewport in that axis (so wrappable children re-flow to fit on resize); `Auto` resolves visibility per-frame from the measured content extent and reserves a row/column only when the bar is actually shown; `Visible` always shows the bar and reserves space.
  - **Border:** Decorative borders with box-drawing characters.
- **Rich Control Suite:**
  - **DataGrid:** Supports `ItemsSource` binding, `AutoGenerateColumns`, selection, and pagination.
  - **Table:** Manual row management with sorting, pagination, and header customization. Supports structural customization via properties such as `ShowBorder`, `ShowHeader`, `ShowVerticalLines`, `ShowHorizontalLines`, and `BorderStyle` (e.g., `BoxStyle.Heavy`, which requires exact Unicode matching for borders like `\u2533` TDown and `\u253B` TUp), while its columns are defined via `TableColumn` utilizing `GridLength` for widths.
  - **MarkdownView:** Renders Markdown content with theming support.
  - **CodeDocument:** Renders syntax-highlighted source code utilizing the internal CodeColoring engine and customizable themes.
  - **Standard Controls:** `Button`, `TextBox`, `PasswordBox`, `TextEditor`, `CheckBox`, `RadioButton`, `ProgressBar`, `TabControl`, `ListBox`, `ComboBox`, `GroupBox`, `TreeView`, `TreeViewItem`, `HeaderedItemsControl`, `Expander`, `DialogBox`, `UniformGrid`, `ScrollViewer`, `ScrollBar`, `Thumb`, `Slider`, `GridSplitter`, `MenuBar`.
  - **ListBox:** Achieves WPF architectural isomorphism by utilizing a `ControlTemplate` wrapping an `ItemsPresenter` within a `ScrollViewer`, removing custom `MeasureOverride` and `Render` logic and deferring layout to standard XAML paradigms. The generated container, `ListBoxItem`, inherits from `ContentControl` and manages its visual state via the `IsSelected` dependency property and `Selected`/`Unselected` bubbling routed events.
  - **Slider:** Inherits from `UIElement` and exposes a bubbling `ValueChanged` routed event (`ValueChangedEvent`) that is raised from `OnPropertyChanged` when `ValueProperty` changes; the `Value` property setter performs clamping and change guarding without directly invoking the event, preserving WPF-style routed event semantics.
  - **Thumb:** Inherits from `Control` and acts as a primitive drag control. It provides standard WPF-equivalent bubbling routed events for drag lifecycles (`DragStarted`, `DragDelta`, and `DragCompleted`). The coordinate properties in specialized event arguments like `DragDeltaEventArgs.HorizontalChange` use `double` to maintain API parity with WPF, despite underlying integer mouse coordinates.
  - **GridSplitter:** Inherits from `Thumb` and dynamically adjusts the `GridLength` of adjacent `RowDefinition` and `ColumnDefinition` elements via the `DragDelta` routed event, implicitly modifying adjacent definitions (e.g., `c - 1` and `c + 1`) when occupying its own dedicated cell index.
  - **MenuBar:** Inherits from `StackPanel` to provide a horizontal menu strip (typically aligned at the top) for `MenuItem` elements, establishing layout and visual defaults (orientation, background, alignment) and rendering the background; submenu and open/close behavior is implemented by `MenuItem`.
  - **ItemsControl:** Inherits from `Control` and utilizes an `ItemsPanel` property (`ItemsPanelTemplate`) alongside an `ItemsPresenter` to generate and populate its visual layout panel, aligning with WPF logical separation. It supports dynamic data mapping via `ItemTemplate` (`DataTemplate`), automatically propagating the ambient data object to a generated `ContentPresenter` during `PrepareContainerForItemOverride`.
  - **Expander:** Inherits from `HeaderedContentControl`, utilizing the `IsExpanded` dependency property and `Expanded`/`Collapsed` bubbling routed events to toggle `ContentPresenter.Visibility` for progressive disclosure.
  - **GroupBox:** Subclasses `HeaderedContentControl` and leverages the `ControlTemplate` engine to map its `Header` and `Content` into a visual `Border`, establishing structural parity with standard WPF grouping conventions.
  - **PasswordBox:** Inherits from `Control` and employs a `ControlTemplate` housing a `TextBox` with `IsPassword = true`, bridging a crucial parity gap for secure input masking by intercepting `OnKeyDown` events to synchronize an exposed `Password` property.
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
- **Visual Tree:** A hierarchical structure of elements allowing for complex composition. Any `UIElement` can dynamically ascend the visual tree to resolve its root host via the `GetRoot()` API, enabling recursive topological queries.
- **Dependency Properties:** A property system that supports value inheritance, change notification, and memory conservation. `DependencyObject` implements `ClearValue` to deterministically remove local property overrides (allowing fallback to default or inherited values), while `SetValue(null)` explicitly stores a null value rather than deleting the entry, maintaining strict WPF isomorphism. Dependency Property value precedence within `DependencyObject.GetValue()` is resolved using discrete `_localValues` and `_triggerValues` dictionaries rather than a monolithic store: a local value assigned after a trigger becomes active overrides that trigger, otherwise an active trigger value overrides any pre-existing local value, followed by ordinary local values, inherited values, and finally default metadata. In other words, the effective order is post-trigger local override > trigger > local > inherited > default. Dependency Property accessors utilize C# expression-bodied members (`get => ...; set => ...;`) to minimize lexical boilerplate. Furthermore, API signatures deviate slightly from standard WPF: `DependencyProperty.Register` accepts default values directly rather than wrapped in a `PropertyMetadata` object.

### Data Binding
Tedd.TUI supports a hierarchical data binding system analogous to WPF, driven by the `DataContext` inherited dependency property.
- **DataContext Inheritance:** The `DataContext` property is an inherited dependency property. `DependencyObject` systematically resolves hierarchical values by recursively interrogating its `InheritanceParent` (which maps deterministically to `Parent` in `UIElement`). Assigning a `DataContext` at the root (e.g., `TuiWindow`) seamlessly propagates the data model down the visual tree via `GetVisualChild` enumeration logic dynamically triggering property invalidations.
- **INotifyPropertyChanged:** Models must implement `System.ComponentModel.INotifyPropertyChanged`. The internal `BindingExpression` autonomously hooks and unhooks to `PropertyChanged` events upon `DataContext` mutations, re-evaluating reflection paths when property names match or signify wholesale updates.
- **Binding Resolutions:** The `SetBinding` method establishes a dynamic link between a target dependency property and a source property. While bindings default to resolving against the ambient `DataContext`, the framework exposes robust `RelativeSource` topologies:
  - `Self`: Targets the `UIElement` itself.
  - `TemplatedParent`: Essential for `ControlTemplate` implementations, targets the origin control instantiating the template visual tree.
  - `FindAncestor`: Ascends the visual tree utilizing `AncestorType` and `AncestorLevel` reflection checks, useful in recursive layout bindings.
- **Collections:** Utilizing `DataGrid` or derivatives of the `Selector` class (`ListBox`, `ComboBox`, `TabControl`) enables binding directly to collections via the `ItemsSource` property. The framework resolves reflection text paths (via `DisplayMemberPath`) utilizing internal string cache pools synchronized securely with C# 13's `System.Threading.Lock`. To circumvent reflection bottlenecks during large dataset rendering, `DataGrid` optimizes bound property access performance by utilizing `System.Linq.Expressions` to compile getter delegates (`Func<object, object>`), which are globally cached in a static dictionary protected by a discrete C# 13 `System.Threading.Lock` for reuse across grid instances.

### Layout Engine
The framework employs a robust, recursive two-pass layout system orchestrated by the abstract `Panel` class:
1.  **Measure Pass:** Container elements recursively query their children, invoking `Measure(Size availableSize)` to compute their `DesiredSize` based on layout constraints. The `UIElement` explicitly calculates requested bounds factoring in the `Margin` dependency property (`Thickness`), effectively reducing the available size passed down. Note that standard WPF layout invalidation (`InvalidateMeasure()`) is absent; topological re-evaluation is universally triggered via `Invalidate()`.
2.  **Arrange Pass:** Parents position and size their children within the computed physical bounds by invoking `Arrange(Rect finalRect)`, which applies structural offsets for `Margin`. Container controls inheriting from `Control` further apply `Padding` (`Thickness`) during their `MeasureOverride` and `ArrangeOverride` operations to reduce inner available space for their embedded `TemplateRoot`. Within layout calculations, exact element dimensions must be accessed via `RenderSize.Width` and `RenderSize.Height` instead of the traditional `ActualWidth` and `ActualHeight` properties.
3.  **Render Pass:** The actual rendering to the `VirtualBuffer` is heavily optimized. Containers executing the `Render` method utilize clipping rects to skip elements that are fully clipped or lie completely outside the current clip rectangle, drastically reducing CPU cycles in complex visual trees.

**Hierarchical Composition:** The abstract `Panel` base class manages composition via its `Children` property (a `UIElementCollection`). For concrete descendants (`StackPanel`, `Grid`, `DockPanel`, `WrapPanel`, `Canvas`, and `UniformGrid`), this collection systematically intercepts modifications. Executing `Panel.Children.Add(child)` strictly enforces visual tree integrity by automatically assigning the parent node, which inherently triggers `DataContext` propagation and establishes the routing infrastructure for input events. To ensure rendering fidelity while minimizing GC pressure, `Panel` maintains a cached `_zSortedChildren` array that is rebuilt when Z-order is invalidated and performs stable Z-Index sorting via a custom $O(N \log N)$ iterative merge sort using temporary buffers rented from `ArrayPool<T>`, rather than relying on ad-hoc in-place permutations.

### Overlay System
Tedd.TUI implements a robust stacking overlay system orchestrated by `TuiWindow`. This architecture is designed to bypass standard document-flow layout passes for modal or transient visual elements (such as `DialogBox` and context menus).
- **Stacking Mechanics:** Overlays are managed chronologically via `PushOverlay` and `RemoveOverlay` (superseding the obsolete `SetOverlay` method). Overlays are structurally appended as integral components of the `TuiWindow` Visual Tree (resolvable via `GetVisualChild`) and receive the ambient `DataContext` as a one-time assignment when inserted. Because this establishes a local `DataContext` value on the overlay, subsequent `TuiWindow.DataContext` changes do not automatically propagate to existing overlays through inherited dependency-property semantics unless that local value is cleared. However, they explicitly bypass the standard recursive `Measure` and `Arrange` passes of `TuiWindow`, requiring their layout to be managed independently by the element (e.g., deterministic sizing inside `DialogBox`). Internally, the stacking system optimizes topological checks utilizing a `List<UIElement>` to provide $O(N)$ presence verification while preventing redundant stack allocations.
- **Rendering & Hit-Testing Priority:** To achieve absolute visual supremacy, the `Render` pipeline processes the overlay collection iteratively *after* the primary `Content`. Conversely, the deterministic input routing system evaluates the overlay stack in reverse topological order (top-to-bottom) during `InputHitTestRecursive`, ensuring the active overlay intercepts global input coordinates before underlying standard components.

### Input & Interaction
Input handling is orchestrated by a deterministic Routed Event architecture managed within `UIElement`:
- **Overlay Management:** `TuiWindow` provides a stacking overlay system via `PushOverlay` and `RemoveOverlay` methods (the legacy `SetOverlay` is obsolete). Overlays are managed chronologically, rendered sequentially (Z-index parity on top of the visual tree), and are evaluated via reverse-topological hit-testing first, providing a robust architecture for modal dialogs and transient components.
- **Mouse Capture:** Global mouse capture for component drag interactions (such as with the `Thumb` control) is managed via `TuiWindow.CaptureMouse(UIElement)` and `TuiWindow.ReleaseMouseCapture()`. This ensures deterministic routing of all subsequent mouse events to the capturing element regardless of cursor bounds.
- **Custom Event Routing:** Custom routed event arguments (such as `DragEventArgs`) can expose strongly typed delegate aliases (e.g., `DragStartedEventHandler`) for consumer ergonomics, but the runtime dispatch path on `UIElement` invokes handlers by casting to `RoutedEventHandler` when possible and otherwise falling back to `Delegate.DynamicInvoke`. There is no requirement for custom `RoutedEventArgs` types to override `InvokeEventHandler`; the framework does not depend on that virtual for handler invocation.
- **Standard Input Events:** Primitive interactions (`KeyDown`, `KeyUp`, `MouseDown`, `MouseUp`, `GotFocus`, `LostFocus`) are registered via `RoutedEvent.Register`. The core supports comprehensive `RoutingStrategy` execution topologies: `Tunnel` (down from root to leaf), `Bubble` (up from leaf to root), and `Direct` (local invocation isolated to the source).
- **Control State Events:** Interactive toggle controls (`CheckBox`, `RadioButton`) implement `Checked` and `Unchecked` bubbling routed events, dispatching dynamically from their `OnPropertyChanged` overrides when `IsCheckedProperty` mutates, guaranteeing robust parent container interception. `RadioButton` explicitly performs global synchronous group updates prior to dispatching `Checked` to enforce uniform topological flow.
- **Execution Phases:** `UIElement` implements two-phase input event routing. The `UIElement.RaiseEvent` pipeline utilizes a zero-allocation algorithm leveraging `System.Buffers.ArrayPool<UIElement>` to construct a discrete routing table with $O(1)$ allocation overhead by walking `Parent` references. Tunneling events prefixed with 'Preview' (e.g., `PreviewKeyDownEvent`) are dispatched first, sequentially from root to leaf; marking a preview event as `Handled` intercepts and prevents the subsequent paired non-preview bubbling event from being raised. Subsequently, `Bubble` events trace backwards from leaf to root. When `Handled = true` on a bubbling event, the route still continues all the way to the root, but instance handlers that were not registered with `handledEventsToo = true` are skipped, while class handlers and handlers that requested `handledEventsToo` continue to run.
- **Coordinate Resolution:** During mouse event dispatch, `UIElement.InvokeHandler` intercepts the `RoutedEventArgs` payload. It dynamically translates absolute global screen coordinates (`GlobalX`, `GlobalY`) into the local `RenderSize` space of the invoking element (updating the `X` and `Y` properties) utilizing `PointFromScreen` prior to emitting the class handler invocation.
- **Class vs. Instance Handlers:** The `InvokeHandler` routine systematically prioritizes overridden virtual methods (e.g., `OnKeyDown`, `OnMouseDown`), which act as implicit class handlers. Subsequentially, it dynamically invokes explicitly bound delegates from the `_eventHandlers` dictionary, effectively decoupling core layout response logic from external subscriber callbacks.
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
    public Button SubmitButton = null!;

    // Method name matches Click handler in XAML
    public void OnSubmit(object sender, RoutedEventArgs e)
    {
        // Handle click
    }
}
```

## License

This project is licensed under the MIT License - see the LICENSE file for details.
