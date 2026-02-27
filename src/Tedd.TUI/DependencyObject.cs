using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace Tedd.TUI;

public class DependencyProperty
{
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
    private readonly Dictionary<DependencyProperty, object> _values = new Dictionary<DependencyProperty, object>();

    protected virtual DependencyObject? InheritanceParent => null;

    public event PropertyChangedEventHandler? PropertyChanged;

    public object? GetValue(DependencyProperty dp)
    {
        if (_values.TryGetValue(dp, out var value))
        {
            return value;
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
        if (value != null && !dp.PropertyType.IsInstanceOfType(value))
        {
            throw new ArgumentException($"Value of type {value.GetType()} is not assignable to property {dp.Name} of type {dp.PropertyType}");
        }
        
        // Null check for value types handled?
        // If property is value type and value is null, this might be invalid but standard SetValue allows null which usually resets or errors?
        // For simplicity allow null if type allows it.

        if (value != null)
            _values[dp] = value;
        else
            _values.Remove(dp); // Treat null as "clear local value" or literal null?
            // In WPF SetValue(null) sets local value to null. ClearValue removes it.
            // But here I'm using dictionary. If I set null, I should store null if it's a valid value.
            // But _values uses object.
            // Let's store null.
            if (value == null) _values[dp] = null!; // Force null storage

        OnPropertyChanged(dp);
    }

    protected bool HasLocalValue(DependencyProperty dp)
    {
        return _values.ContainsKey(dp);
    }

    protected virtual void OnPropertyChanged(DependencyProperty dp)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(dp.Name));
    }
}
