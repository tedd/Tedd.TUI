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
    public Type? AncestorType { get; set; }
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
    public string? Path { get; set; }
    public object? Source { get; set; }
    public RelativeSource? RelativeSource { get; set; }
    public BindingMode Mode { get; set; } = BindingMode.OneWay;
    public IValueConverter? Converter { get; set; }
    public object? ConverterParameter { get; set; }
    public object? FallbackValue { get; set; }

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
    // Breaks target<->source update cycles for TwoWay bindings whose converter or type
    // coercion doesn't round-trip to an equal value.
    private bool _isUpdating;
    private bool _targetSubscribed;

    /// <summary>The dependency property this expression drives on its target.</summary>
    internal DependencyProperty TargetProperty => _property;

    public BindingExpression(UIElement target, DependencyProperty property, Binding binding)
    {
        _target = target;
        _property = property;
        _binding = binding;
    }

    /// <summary>
    /// Activates the expression: wires target-change tracking for modes that write back
    /// to the source and performs the initial value transfer in the mode's direction.
    /// </summary>
    internal void Attach()
    {
        if (_binding.Mode is BindingMode.TwoWay or BindingMode.OneWayToSource)
        {
            _target.PropertyChanged += OnTargetPropertyChanged;
            _targetSubscribed = true;
        }

        if (_binding.Mode == BindingMode.OneWayToSource)
        {
            _currentSource = ResolveSource();
            UpdateSource();
        }
        else
        {
            UpdateTarget();
        }
    }

    /// <summary>
    /// Deactivates the expression, dropping both the source INPC subscription and the
    /// target subscription so a replaced binding cannot keep updating (or leak).
    /// </summary>
    internal void Detach()
    {
        if (_currentSource is INotifyPropertyChanged npc)
        {
            npc.PropertyChanged -= OnPropertyChanged;
        }
        _currentSource = null;

        if (_targetSubscribed)
        {
            _target.PropertyChanged -= OnTargetPropertyChanged;
            _targetSubscribed = false;
        }
    }

    private object? ResolveSource()
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

    private object? FindAncestor(UIElement start, Type? ancestorType, int level)
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

        // OneWayToSource never writes the target; a source change (e.g. new DataContext)
        // re-resolves and pushes the target's current value into the new source instead.
        if (_binding.Mode == BindingMode.OneWayToSource)
        {
            _currentSource = newSource;
            UpdateSource();
            return;
        }

        // Handle INotifyPropertyChanged subscription change. OneTime bindings transfer
        // the value whenever they are (re)activated — initial set, TemplatedParent or
        // DataContext change — but never track subsequent source mutations.
        if (newSource != _currentSource)
        {
            if (_currentSource is INotifyPropertyChanged oldNpc)
            {
                oldNpc.PropertyChanged -= OnPropertyChanged;
            }

            _currentSource = newSource;

            if (_binding.Mode != BindingMode.OneTime && _currentSource is INotifyPropertyChanged newNpc)
            {
                newNpc.PropertyChanged += OnPropertyChanged;
            }
        }

        if (newSource == null)
        {
            // If fallback value is set, use it
            if (_binding.FallbackValue != null)
            {
                _target.SetValue(_property, _binding.FallbackValue);
            }
            return;
        }

        object? value = newSource;
        if (!string.IsNullOrEmpty(_binding.Path))
        {
            // Simple reflection to get property value from context
            var propInfo = newSource.GetType().GetProperty(_binding.Path);
            if (propInfo != null)
            {
                value = propInfo.GetValue(newSource);
            }
            else
            {
                // Property not found
                if (_binding.Path == ".")
                    value = newSource;
                else
                    value = _binding.FallbackValue ?? _property.DefaultValue;
            }
        }

        // Apply Converter
        if (_binding.Converter != null && value != null)
        {
            value = _binding.Converter.Convert(value, _property.PropertyType, _binding.ConverterParameter, CultureInfo.CurrentCulture);
        }

        if (_isUpdating) return;
        _isUpdating = true;
        try
        {
            _target.SetValue(_property, value);
        }
        finally
        {
            _isUpdating = false;
        }
    }

    /// <summary>
    /// Pushes the target property's current value into the source property (TwoWay /
    /// OneWayToSource). No-op when the source property is missing, read-only, or the
    /// value cannot be converted; a failed write must never take down input handling.
    /// </summary>
    private void UpdateSource()
    {
        if (_isUpdating) return;

        object? source = _currentSource ?? ResolveSource();
        _currentSource = source;
        if (source == null) return;
        if (string.IsNullOrEmpty(_binding.Path) || _binding.Path == ".") return;

        var propInfo = source.GetType().GetProperty(_binding.Path);
        if (propInfo == null || !propInfo.CanWrite) return;

        object? value = _target.GetValue(_property);

        if (_binding.Converter != null)
        {
            value = _binding.Converter.ConvertBack(value!, propInfo.PropertyType, _binding.ConverterParameter!, CultureInfo.CurrentCulture);
        }

        try
        {
            if (value != null && !propInfo.PropertyType.IsInstanceOfType(value))
            {
                var targetType = Nullable.GetUnderlyingType(propInfo.PropertyType) ?? propInfo.PropertyType;
                value = Convert.ChangeType(value, targetType, CultureInfo.CurrentCulture);
            }

            // Skip no-op writes so INPC sources don't echo the value straight back.
            if (Equals(propInfo.GetValue(source), value)) return;

            _isUpdating = true;
            try
            {
                propInfo.SetValue(source, value);
            }
            finally
            {
                _isUpdating = false;
            }
        }
        catch
        {
            // Unconvertible value: leave the source unchanged.
        }
    }

    private void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == _binding.Path || string.IsNullOrEmpty(e.PropertyName))
        {
            UpdateTarget();
        }
    }

    private void OnTargetPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == _property.Name)
        {
            UpdateSource();
        }
    }
}
