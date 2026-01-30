using System;
using System.Collections.Generic;

namespace Tedd.TUI;

public class DependencyProperty
{
    public string Name { get; }
    public Type PropertyType { get; }
    public Type OwnerType { get; }
    public object DefaultValue { get; }
    public bool IsInherited { get; }

    private DependencyProperty(string name, Type propertyType, Type ownerType, object defaultValue, bool isInherited)
    {
        Name = name;
        PropertyType = propertyType;
        OwnerType = ownerType;
        DefaultValue = defaultValue;
        IsInherited = isInherited;
    }

    public static DependencyProperty Register(string name, Type propertyType, Type ownerType, object defaultValue = null, bool isInherited = false)
    {
        return new DependencyProperty(name, propertyType, ownerType, defaultValue, isInherited);
    }
}

public class DependencyObject
{
    private readonly Dictionary<DependencyProperty, object> _values = new Dictionary<DependencyProperty, object>();

    protected virtual DependencyObject InheritanceParent => null;

    public object GetValue(DependencyProperty dp)
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

    protected bool HasLocalValue(DependencyProperty dp)
    {
        return _values.ContainsKey(dp);
    }

    protected virtual void OnPropertyChanged(DependencyProperty dp)
    {
        // Hook for inheritance or binding updates
    }
}
