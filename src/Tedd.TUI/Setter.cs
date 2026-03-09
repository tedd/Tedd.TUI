using System;

namespace Tedd.TUI;

public class Setter
{
    public DependencyProperty? Property { get; set; }
    public object? Value { get; set; }
    public string? TargetName { get; set; }

    public Setter() { }

    public Setter(DependencyProperty property, object? value)
    {
        Property = property;
        Value = value;
    }
}
