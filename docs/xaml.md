# XAML Guide

Tedd.TUI UIs can be defined in a XAML dialect loaded at runtime by `XamlLoader` (in the
core `Tedd.TUI` package). The same file works in every host — terminal, Blazor
(`TuiXamlView`), WPF (`TuiHostElement`), Avalonia/WinUI/MAUI (`TuiHostControl` /
`TuiHostView`) — and is tolerant of the extra attributes XAML editors add, so you can edit
it in Visual Studio with the WPF host giving you a live preview.

## Loading

```csharp
var controller = new AppController();
var window = (TuiWindow)XamlLoader.Load(xamlText, controller);
```

Element names resolve against the `Tedd.TUI`, `Tedd.TUI.Markdown` and
`Tedd.TUI.CodeColoring` namespaces (searching loaded assemblies), so `<Button />` creates
`Tedd.TUI.Button`. Namespace prefixes on elements (`<tui:Button>`) are stripped and
resolved by local name.

## The controller

The optional second argument to `XamlLoader.Load` is a **controller**: the object event
attributes and `x:Name` fields bind against.

```xml
<StackPanel>
  <TextBox x:Name="NameBox" Width="30" />
  <Button Content="Submit" Click="OnSubmit" />
  <MenuItem Command="OnNew">…</MenuItem>
</StackPanel>
```

```csharp
public class AppController
{
    public TextBox? NameBox;                 // assigned by the loader (x:Name or Name)
    public void OnSubmit() { … }             // bound to Click
    public void OnNew() { … }                // bound to Command (delegate property)
}
```

- **Events** (`Click`, `ValueChanged`, …): the attribute value is a method name on the
  controller. Parameterless methods are wrapped automatically for `RoutedEventHandler`,
  `EventHandler` and `Action` signatures.
- **Delegate properties** (`Command`): same method-name binding.
- **Named elements**: `x:Name` (or plain `Name`) assigns the element to a controller
  field with the same name and a compatible type.

## Property syntax

| Syntax | Example |
|---|---|
| Attribute | `<TextBlock Text="Hi" Foreground="Cyan" />` |
| Attached property | `<Button Grid.Row="1" Grid.ColumnSpan="2" />` |
| Property element | `<Table.Columns><TableColumn … /></Table.Columns>` |
| Element text content | `<TextBlock>Hi</TextBlock>` (sets `Content`/`Text`) |

Children are added by parent shape: `Panel.Children`, `ContentControl.Content`,
`Border.Child`, `ItemsControl.Items`, `Table` rows/columns, `TableRow` cells.

## Value conversion

- **Numbers / booleans / enums** — invariant parsing; enum values by name
  (`Orientation="Horizontal"`, `BoxStyle="Double"`).
- **Colors** (`TuiColor`) — CSS-style: `#RGB`, `#RRGGBB`, `#RRGGBBAA`, `rgb(r,g,b)`,
  `rgba(r,g,b,a)`, plus the 16 legacy `ConsoleColor` names (`Cyan`, `DarkGray`, …).
  Nullable colors accept `transparent`/`null`/empty.
- **GridLength** — `*`, `2*`, `Auto`, or an integer cell count.
- **Sizes are character cells**, not pixels: `Width="30"` means 30 columns.

## XAML-editor compatibility

`XamlLoader` ignores everything a designer adds, so a file can carry full editor metadata
and still load in every host:

```xml
<TuiWindow xmlns="urn:tedd-tui"
           xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
           xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
           xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
           mc:Ignorable="d" d:DesignWidth="80" d:DesignHeight="25">
  …
</TuiWindow>
```

Rules:

- `xmlns` / `xmlns:*` declarations are ignored.
- Element namespace prefixes are stripped (`<tui:Button>` ⇒ `Button`).
- `x:Name` is treated as `Name`; **all other prefixed attributes are ignored**
  (`mc:Ignorable`, `d:DesignWidth`, `x:Class`, …).
- XML comments are skipped.

For the live-preview editing workflow, host the file with the WPF `TuiHostElement`
(`Source="app.xaml"`) and open the hosting WPF window in the Visual Studio designer — the
designer instantiates the host element, which runs the real TUI renderer. See
[WPF](platforms/wpf.md).

## Root elements

The loader returns whatever the root element is. A `TuiWindow` root is used directly as
the hosted window; any other root is wrapped as window content by the hosts
(`TuiXamlView`, `TuiHostElement`, `TuiHostControl`, `TuiHostView`) or by your own code.
