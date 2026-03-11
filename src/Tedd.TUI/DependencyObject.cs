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

    public bool HasLocalValue => LocalValue != DependencyProperty.UnsetValue;
    public bool HasTriggerValue => TriggerValue != DependencyProperty.UnsetValue;
    public bool HasStyleValue => StyleValue != DependencyProperty.UnsetValue;

    public object? GetEffectiveValue()
    {
        if (HasTriggerValue) return TriggerValue;
        if (HasLocalValue) return LocalValue;
        if (HasStyleValue) return StyleValue;
        return DependencyProperty.UnsetValue;
    }

    public bool HasAnyValue => HasLocalValue || HasTriggerValue || HasStyleValue;
}

public class DependencyObject : INotifyPropertyChanged
{
    private readonly Dictionary<DependencyProperty, EffectiveValueEntry> _effectiveValues = new();

    protected virtual DependencyObject? InheritanceParent => null;

    public event PropertyChangedEventHandler? PropertyChanged;

    public object? GetValue(DependencyProperty dp)
    {
        if (_effectiveValues.TryGetValue(dp, out var entry))
        {
            var value = entry.GetEffectiveValue();
            if (value != DependencyProperty.UnsetValue)
            {
                return value;
            }
        }
        if (dp.IsInherited && InheritanceParent != null)
        {
            return InheritanceParent.GetValue(dp);
        }
        return dp.DefaultValue;
    }

    public void SetValue(DependencyProperty dp, object? value)
    {
        // Basic type validation
        if (value != null && value != DependencyProperty.UnsetValue && !dp.PropertyType.IsInstanceOfType(value))
        {
            throw new ArgumentException($"Value of type {value.GetType()} is not assignable to property {dp.Name} of type {dp.PropertyType}");
        }

        if (!_effectiveValues.TryGetValue(dp, out var entry))
        {
            entry = new EffectiveValueEntry();
            _effectiveValues[dp] = entry;
        }

        object? oldValue = entry.GetEffectiveValue();
        entry.LocalValue = value ?? null!;
        object? newValue = entry.GetEffectiveValue();

        if (!object.Equals(oldValue, newValue))
        {
            OnPropertyChanged(dp);
        }
    }

    internal void SetTriggerValue(DependencyProperty dp, object? value)
    {
        if (!_effectiveValues.TryGetValue(dp, out var entry))
        {
            entry = new EffectiveValueEntry();
            _effectiveValues[dp] = entry;
        }

        object? oldValue = entry.GetEffectiveValue();
        entry.TriggerValue = value ?? null!;
        object? newValue = entry.GetEffectiveValue();

        if (!object.Equals(oldValue, newValue))
        {
            OnPropertyChanged(dp);
        }
    }

    internal void ClearTriggerValue(DependencyProperty dp)
    {
        if (_effectiveValues.TryGetValue(dp, out var entry))
        {
            object? oldValue = entry.GetEffectiveValue();
            entry.TriggerValue = DependencyProperty.UnsetValue;
            object? newValue = entry.GetEffectiveValue();

            if (!entry.HasAnyValue)
            {
                _effectiveValues.Remove(dp);
            }

            if (!object.Equals(oldValue, newValue))
            {
                OnPropertyChanged(dp);
            }
        }
    }

    internal void SetStyleValue(DependencyProperty dp, object? value)
    {
        if (!_effectiveValues.TryGetValue(dp, out var entry))
        {
            entry = new EffectiveValueEntry();
            _effectiveValues[dp] = entry;
        }

        object? oldValue = entry.GetEffectiveValue();
        entry.StyleValue = value ?? null!;
        object? newValue = entry.GetEffectiveValue();

        if (!object.Equals(oldValue, newValue))
        {
            OnPropertyChanged(dp);
        }
    }

    internal void ClearStyleValue(DependencyProperty dp)
    {
        if (_effectiveValues.TryGetValue(dp, out var entry))
        {
            object? oldValue = entry.GetEffectiveValue();
            entry.StyleValue = DependencyProperty.UnsetValue;
            object? newValue = entry.GetEffectiveValue();

            if (!entry.HasAnyValue)
            {
                _effectiveValues.Remove(dp);
            }

            if (!object.Equals(oldValue, newValue))
            {
                OnPropertyChanged(dp);
            }
        }
    }

    public void ClearValue(DependencyProperty dp)
    {
        if (_effectiveValues.TryGetValue(dp, out var entry))
        {
            object? oldValue = entry.GetEffectiveValue();
            entry.LocalValue = DependencyProperty.UnsetValue;
            object? newValue = entry.GetEffectiveValue();

            if (!entry.HasAnyValue)
            {
                _effectiveValues.Remove(dp);
            }

            if (!object.Equals(oldValue, newValue))
            {
                OnPropertyChanged(dp);
            }
        }
    }

    public bool HasLocalValue(DependencyProperty dp)
    {
        return _effectiveValues.TryGetValue(dp, out var entry) && entry.HasLocalValue;
    }

    protected virtual void OnPropertyChanged(DependencyProperty dp)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(dp.Name));
    }
}
