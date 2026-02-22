using System;
using System.ComponentModel;
using System.Reflection;

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
    private object? _currentContext;

    public BindingExpression(UIElement target, DependencyProperty property, Binding binding)
    {
        _target = target;
        _property = property;
        _binding = binding;
    }

    public void UpdateTarget()
    {
        object? context = _target.DataContext;

        // Handle INotifyPropertyChanged subscription change
        if (context != _currentContext)
        {
            if (_currentContext is INotifyPropertyChanged oldNpc)
            {
                oldNpc.PropertyChanged -= OnPropertyChanged;
            }

            _currentContext = context;

            if (_currentContext is INotifyPropertyChanged newNpc)
            {
                newNpc.PropertyChanged += OnPropertyChanged;
            }
        }

        if (context == null) return;

        // Simple reflection to get property value from context
        var propInfo = context.GetType().GetProperty(_binding.Path);
        if (propInfo != null)
        {
            var value = propInfo.GetValue(context);
            _target.SetValue(_property, value);
        }
    }

    private void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == _binding.Path || string.IsNullOrEmpty(e.PropertyName))
        {
             // Context hasn't changed, but property value inside it has.
             // We just need to refresh the value.
             // But UpdateTarget also handles context change logic.
             // Since context is same, the subscription logic won't run again.
             UpdateTarget();
        }
    }
}
