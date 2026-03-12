using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace Tedd.TUI;

public class DependencyProperty
{
    public static readonly object UnsetValue = new object();

    public string Name { get; }
    public Type PropertyType { get; }
    public Type OwnerType { get; }
    public object? DefaultValue { get; }
    public bool IsInherited { get; }

    private DependencyProperty(string name, Type propertyType, Type ownerType, object? defaultValue, bool isInherited)
    {
        Name = name;
        PropertyType = propertyType;
        OwnerType = ownerType;
        DefaultValue = defaultValue;
        IsInherited = isInherited;
    }

    public static DependencyProperty Register(string name, Type propertyType, Type ownerType, object? defaultValue = null, bool isInherited = false)
    {
        return new DependencyProperty(name, propertyType, ownerType, defaultValue, isInherited);
    }

    public static DependencyProperty RegisterAttached(string name, Type propertyType, Type ownerType, object? defaultValue = null, bool isInherited = false)
    {
        // Attached properties are essentially the same structure in this simple implementation
        return new DependencyProperty(name, propertyType, ownerType, defaultValue, isInherited);
    }
}

internal class EffectiveValueEntry
{
    public object? LocalValue { get; set; } = DependencyProperty.UnsetValue;
    public object? TriggerValue { get; set; } = DependencyProperty.UnsetValue;
    public object? StyleValue { get; set; } = DependencyProperty.UnsetValue;

    public object? GetEffectiveValue()
    {
        if (LocalValue != DependencyProperty.UnsetValue) return LocalValue;
        if (TriggerValue != DependencyProperty.UnsetValue) return TriggerValue;
        if (StyleValue != DependencyProperty.UnsetValue) return StyleValue;
        return DependencyProperty.UnsetValue;
    }
}

public class DependencyObject : INotifyPropertyChanged
{
    private readonly Dictionary<DependencyProperty, EffectiveValueEntry> _values = new();

    protected virtual DependencyObject? InheritanceParent => null;

    public event PropertyChangedEventHandler? PropertyChanged;

    public object? GetValue(DependencyProperty dp)
    {
        if (_values.TryGetValue(dp, out var entry))
        {
            var effectiveValue = entry.GetEffectiveValue();
            if (effectiveValue != DependencyProperty.UnsetValue)
            {
                return effectiveValue;
            }
        }

        if (dp.IsInherited && InheritanceParent != null)
        {
            return InheritanceParent.GetValue(dp);
        }

        return dp.DefaultValue;
    }

    private EffectiveValueEntry GetOrCreateEntry(DependencyProperty dp)
    {
        if (!_values.TryGetValue(dp, out var entry))
        {
            entry = new EffectiveValueEntry();
            _values[dp] = entry;
        }
        return entry;
    }

    public void SetValue(DependencyProperty dp, object? value)
    {
        ValidateValue(dp, value);
        var entry = GetOrCreateEntry(dp);

        var oldEffective = entry.GetEffectiveValue();
        entry.LocalValue = value;
        var newEffective = entry.GetEffectiveValue();

        if (!object.Equals(oldEffective, newEffective))
        {
            OnPropertyChanged(dp);
        }
    }

    internal void SetTriggerValue(DependencyProperty dp, object? value)
    {
        ValidateValue(dp, value);
        var entry = GetOrCreateEntry(dp);

        var oldEffective = entry.GetEffectiveValue();
        entry.TriggerValue = value;
        var newEffective = entry.GetEffectiveValue();

        if (!object.Equals(oldEffective, newEffective))
        {
            OnPropertyChanged(dp);
        }
    }

    internal void ClearTriggerValue(DependencyProperty dp)
    {
        if (_values.TryGetValue(dp, out var entry))
        {
            var oldEffective = entry.GetEffectiveValue();
            entry.TriggerValue = DependencyProperty.UnsetValue;
            var newEffective = entry.GetEffectiveValue();

            if (!object.Equals(oldEffective, newEffective))
            {
                OnPropertyChanged(dp);
            }
        }
    }

    internal void SetStyleValue(DependencyProperty dp, object? value)
    {
        ValidateValue(dp, value);
        var entry = GetOrCreateEntry(dp);

        var oldEffective = entry.GetEffectiveValue();
        entry.StyleValue = value;
        var newEffective = entry.GetEffectiveValue();

        if (!object.Equals(oldEffective, newEffective))
        {
            OnPropertyChanged(dp);
        }
    }

    internal void ClearStyleValue(DependencyProperty dp)
    {
        if (_values.TryGetValue(dp, out var entry))
        {
            var oldEffective = entry.GetEffectiveValue();
            entry.StyleValue = DependencyProperty.UnsetValue;
            var newEffective = entry.GetEffectiveValue();

            if (!object.Equals(oldEffective, newEffective))
            {
                OnPropertyChanged(dp);
            }
        }
    }

    private void ValidateValue(DependencyProperty dp, object? value)
    {
        if (value != null && value != DependencyProperty.UnsetValue && !dp.PropertyType.IsInstanceOfType(value))
        {
            throw new ArgumentException($"Value of type {value.GetType()} is not assignable to property {dp.Name} of type {dp.PropertyType}");
        }
    }

    public void ClearValue(DependencyProperty dp)
    {
        if (_values.TryGetValue(dp, out var entry))
        {
            var oldEffective = entry.GetEffectiveValue();
            entry.LocalValue = DependencyProperty.UnsetValue;
            var newEffective = entry.GetEffectiveValue();

            if (!object.Equals(oldEffective, newEffective))
            {
                OnPropertyChanged(dp);
            }
        }
    }

    public bool HasLocalValue(DependencyProperty dp)
    {
        if (_values.TryGetValue(dp, out var entry))
        {
            return entry.LocalValue != DependencyProperty.UnsetValue;
        }
        return false;
    }

    protected virtual void OnPropertyChanged(DependencyProperty dp)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(dp.Name));
    }
}
