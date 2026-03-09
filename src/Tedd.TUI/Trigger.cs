using System;
using System.Collections.Generic;

namespace Tedd.TUI;

public class Trigger : TriggerBase
{
    public DependencyProperty? Property { get; set; }
    public object? Value { get; set; }
    public List<Setter> Setters { get; } = new List<Setter>();
}
