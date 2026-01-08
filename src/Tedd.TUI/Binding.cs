using System;
using System.ComponentModel;

namespace Tedd.TUI;

public class Binding
{
    public string Path { get; }
    public Binding(string path)
    {
        Path = path;
    }
}

public class BindingExpression
{
    private readonly UIElement _target;
    private readonly DependencyProperty _property;
    private readonly Binding _binding;

    public BindingExpression(UIElement target, DependencyProperty property, Binding binding)
    {
        _target = target;
        _property = property;
        _binding = binding;
    }

    public void UpdateTarget()
    {
        object context = _target.DataContext;
        if (context == null) return;

        // Simple reflection to get property value from context
        var propInfo = context.GetType().GetProperty(_binding.Path);
        if (propInfo != null)
        {
            var value = propInfo.GetValue(context);
            _target.SetValue(_property, value);
        }
    }
}
