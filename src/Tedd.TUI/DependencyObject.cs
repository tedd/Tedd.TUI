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

public class DependencyObject : INotifyPropertyChanged
{
    private readonly Dictionary<DependencyProperty, object> _localValues = new();
    private readonly Dictionary<DependencyProperty, object> _triggerValues = new();

    protected virtual DependencyObject? InheritanceParent => null;
    public event PropertyChangedEventHandler? PropertyChanged;

    public object? GetValue(DependencyProperty dp)
    {
        if (_localValues.TryGetValue(dp, out var value))
        {
            return value;
        }
        if (_triggerValues.TryGetValue(dp, out var tValue))
        {
            return tValue;
        }
        if (dp.IsInherited && InheritanceParent != null)
        {
            return InheritanceParent.GetValue(dp);
        }
        return dp.DefaultValue;
    }

    public void SetValue(DependencyProperty dp, object? value)
    {
        if (value != null && !dp.PropertyType.IsInstanceOfType(value))
        {
            throw new ArgumentException($"Value of type {value.GetType()} is not assignable to property {dp.Name} of type {dp.PropertyType}");
        }

        _localValues[dp] = value ?? null!;

        OnPropertyChanged(dp);
    }

    internal void SetTriggerValue(DependencyProperty dp, object? value)
    {
        _triggerValues[dp] = value ?? null!;
        OnPropertyChanged(dp);
    }

    internal void ClearTriggerValue(DependencyProperty dp)
    {
        if (_triggerValues.Remove(dp))
        {
            OnPropertyChanged(dp);
        }
    }

    public void ClearValue(DependencyProperty dp)
    {
        if (_localValues.Remove(dp))
        {
            OnPropertyChanged(dp);
        }
    }

    public bool HasLocalValue(DependencyProperty dp)
    {
        return _localValues.ContainsKey(dp);
    }

    protected virtual void OnPropertyChanged(DependencyProperty dp)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(dp.Name));
    }
}
