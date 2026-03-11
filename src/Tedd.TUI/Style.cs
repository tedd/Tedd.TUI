using System;
using System.Collections.Generic;

namespace Tedd.TUI;

public class Style
{
    public Type? TargetType { get; set; }
    public List<Setter> Setters { get; } = new List<Setter>();
    public List<TriggerBase> Triggers { get; } = new List<TriggerBase>();
}
