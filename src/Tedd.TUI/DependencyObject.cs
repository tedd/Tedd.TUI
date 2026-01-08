using System;
using System.Collections.Generic;

namespace Tedd.TUI;

public class DependencyProperty
{
    public string Name { get; }
    public Type PropertyType { get; }
    public Type OwnerType { get; }
    public object DefaultValue { get; }

    private DependencyProperty(string name, Type propertyType, Type ownerType, object defaultValue)
    {
        Name = name;
        PropertyType = propertyType;
        OwnerType = ownerType;
        DefaultValue = defaultValue;
    }

    public static DependencyProperty Register(string name, Type propertyType, Type ownerType, object defaultValue = null)
    {
        return new DependencyProperty(name, propertyType, ownerType, defaultValue);
    }
}

public class DependencyObject
{
    private readonly Dictionary<DependencyProperty, object> _values = new Dictionary<DependencyProperty, object>();

    public object GetValue(DependencyProperty dp)
    {
        if (_values.TryGetValue(dp, out var value))
        {
            return value;
        }
        return dp.DefaultValue;
    }

    public void SetValue(DependencyProperty dp, object value)
    {
        // Basic type validation
        if (value != null && !dp.PropertyType.IsInstanceOfType(value))
        {
            throw new ArgumentException($"Value of type {value.GetType()} is not assignable to property {dp.Name} of type {dp.PropertyType}");
        }
        
        _values[dp] = value;
        OnPropertyChanged(dp);
    }

    protected virtual void OnPropertyChanged(DependencyProperty dp)
    {
        // Hook for inheritance or binding updates
    }
}
