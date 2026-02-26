using System;
using System.ComponentModel;
using System.Globalization;
using System.Reflection;

namespace Tedd.TUI;

public enum BindingMode
{
    OneWay,
    TwoWay,
    OneTime,
    OneWayToSource
}

public enum RelativeSourceMode
{
    None,
    Self,
    TemplatedParent,
    FindAncestor
}

public class RelativeSource
{
    public RelativeSourceMode Mode { get; set; }
    public Type AncestorType { get; set; }
    public int AncestorLevel { get; set; } = 1;

    public RelativeSource(RelativeSourceMode mode)
    {
        Mode = mode;
    }

    public static RelativeSource Self => new RelativeSource(RelativeSourceMode.Self);
    public static RelativeSource TemplatedParent => new RelativeSource(RelativeSourceMode.TemplatedParent);
}

public interface IValueConverter
{
    object Convert(object value, Type targetType, object parameter, CultureInfo culture);
    object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture);
}

public class Binding
{
    public string Path { get; set; }
    public object Source { get; set; }
    public RelativeSource RelativeSource { get; set; }
    public BindingMode Mode { get; set; } = BindingMode.OneWay;
    public IValueConverter Converter { get; set; }
    public object ConverterParameter { get; set; }
    public object FallbackValue { get; set; }

    public Binding(string path)
    {
        Path = path;
    }

    public Binding()
    {
    }
}

public class BindingExpression
{
    private readonly UIElement _target;
    private readonly DependencyProperty _property;
    private readonly Binding _binding;
    private object? _currentSource;

    public BindingExpression(UIElement target, DependencyProperty property, Binding binding)
    {
        _target = target;
        _property = property;
        _binding = binding;
    }

    private object ResolveSource()
    {
        if (_binding.Source != null)
        {
            return _binding.Source;
        }

        if (_binding.RelativeSource != null)
        {
            switch (_binding.RelativeSource.Mode)
            {
                case RelativeSourceMode.Self:
                    return _target;
                case RelativeSourceMode.TemplatedParent:
                    return _target.TemplatedParent;
                case RelativeSourceMode.FindAncestor:
                    return FindAncestor(_target, _binding.RelativeSource.AncestorType, _binding.RelativeSource.AncestorLevel);
            }
        }

        return _target.DataContext;
    }

    private object FindAncestor(UIElement start, Type ancestorType, int level)
    {
        if (start == null || ancestorType == null) return null;
        var current = start;
        int count = 0;
        while (current != null)
        {
            if (ancestorType.IsInstanceOfType(current))
            {
                count++;
                if (count == level) return current;
            }
            current = current.Parent;
        }
        return null;
    }

    public void UpdateTarget()
    {
        object? newSource = ResolveSource();

        // Handle INotifyPropertyChanged subscription change
        if (newSource != _currentSource)
        {
            if (_currentSource is INotifyPropertyChanged oldNpc)
            {
                oldNpc.PropertyChanged -= OnPropertyChanged;
            }

            _currentSource = newSource;

            if (_currentSource is INotifyPropertyChanged newNpc)
            {
                newNpc.PropertyChanged += OnPropertyChanged;
            }
        }

        if (newSource == null)
        {
            // If fallback value is set, use it? Or clear?
            // For now do nothing or set default.
            return;
        }

        object value = newSource;
        if (!string.IsNullOrEmpty(_binding.Path))
        {
             // Simple reflection to get property value from context
             // Supports basic property paths? For now just one level.
             var propInfo = newSource.GetType().GetProperty(_binding.Path);
             if (propInfo != null)
             {
                 value = propInfo.GetValue(newSource);
             }
             else
             {
                 // Property not found
                 // Check if it's a field? No.
                 // Maybe check if source IS the value (Path=".")
                 if (_binding.Path == ".") value = newSource;
                 else value = _binding.FallbackValue ?? _property.DefaultValue;
             }
        }

        // Apply Converter
        if (_binding.Converter != null)
        {
            value = _binding.Converter.Convert(value, _property.PropertyType, _binding.ConverterParameter, CultureInfo.CurrentCulture);
        }

        _target.SetValue(_property, value);
    }

    private void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == _binding.Path || string.IsNullOrEmpty(e.PropertyName))
        {
             // Context hasn't changed, but property value inside it has.
             // But we need to re-evaluate the property value.
             // We can just call UpdateTarget() but that re-resolves source (which is fine, it's fast).
             UpdateTarget();
        }
    }
}
