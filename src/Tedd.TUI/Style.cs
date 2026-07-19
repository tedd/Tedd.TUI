using System;
using System.Collections.Generic;

namespace Tedd.TUI;

/// <summary>
/// A reusable set of property values for a control type, mirroring the XAML
/// <c>&lt;Style TargetType="..."&gt;</c> concept. Styles are the building blocks of a
/// <see cref="TuiTheme"/>: each style carries plain <see cref="Setter"/>s that supply
/// values for dependency properties on every element assignable to
/// <see cref="TargetType"/>.
/// </summary>
/// <remarks>
/// Theme style values rank below local values and trigger values but above inherited
/// values and registration defaults, matching WPF's precedence for theme-level styles.
/// Setting a property directly on an element therefore always wins over the theme,
/// and <see cref="DependencyObject.ClearValue"/> restores the themed value.
/// </remarks>
public class Style
{
    public Style() { }

    public Style(Type targetType)
    {
        TargetType = targetType;
    }

    /// <summary>
    /// The element type this style applies to. The style also applies to derived types;
    /// when several styles match an element, setters from the style with the most derived
    /// <see cref="TargetType"/> win.
    /// </summary>
    public Type? TargetType { get; set; }

    /// <summary>
    /// Property values applied by this style. <see cref="Setter.TargetName"/> is not
    /// used for theme styles and is ignored.
    /// </summary>
    public List<Setter> Setters { get; } = new();

    /// <summary>Adds a setter and returns this style for fluent theme construction.</summary>
    public Style Set(DependencyProperty property, object? value)
    {
        Setters.Add(new Setter(property, value));
        return this;
    }
}
