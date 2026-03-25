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
    // Tracks properties for which SetValue was called while a trigger was active.
    // GetValue returns the local value for these, giving it precedence over the trigger.
    // Clearing the local value (ClearValue) removes the property from this set so that
    // the still-active trigger value is re-exposed instead of falling back to inherited/default.
    private readonly HashSet<DependencyProperty> _localOverridesActiveTrigger = new();

    protected virtual DependencyObject? InheritanceParent => null;

    public event PropertyChangedEventHandler? PropertyChanged;

    public object? GetValue(DependencyProperty dp)
    {
        // A local value explicitly set while a trigger is active takes highest precedence.
        if (_localOverridesActiveTrigger.Contains(dp) && _localValues.TryGetValue(dp, out var overrideValue))
        {
            return overrideValue;
        }
        // Active trigger values take precedence over pre-existing local values.
        if (_triggerValues.TryGetValue(dp, out var triggerValue))
        {
            return triggerValue;
        }
        if (_localValues.TryGetValue(dp, out var localValue))
        {
            return localValue;
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

        _localValues[dp] = value ?? null!;

        // If an active trigger is present for this property, track the local value as a
        // post-trigger override so that GetValue returns it with higher priority than the
        // trigger.  The trigger value is intentionally kept so that clearing the local
        // value (ClearValue) re-exposes the trigger rather than falling back to
        // inherited/default.
        if (_triggerValues.ContainsKey(dp))
        {
            _localOverridesActiveTrigger.Add(dp);
        }

        OnPropertyChanged(dp);
    }

    public void ClearValue(DependencyProperty dp)
    {
        bool changed = _localValues.Remove(dp);
        // Clearing the local value removes the post-trigger override flag; the trigger
        // value (if still present) will naturally surface from GetValue.
        // Even when no local value was present, removing the override flag can expose a
        // different effective value (the trigger's), so treat it as a change.
        changed = changed || (_localOverridesActiveTrigger.Remove(dp) && _triggerValues.ContainsKey(dp));
        if (changed)
        {
            OnPropertyChanged(dp);
        }
    }

    public bool HasLocalValue(DependencyProperty dp)
    {
        return _localValues.ContainsKey(dp);
    }

    internal void SetTriggerValue(DependencyProperty dp, object? value)
    {
        // Basic type validation
        if (value != null && !dp.PropertyType.IsInstanceOfType(value))
        {
            throw new ArgumentException($"Value of type {value.GetType()} is not assignable to property {dp.Name} of type {dp.PropertyType}");
        }

        _triggerValues[dp] = value ?? null!;

        OnPropertyChanged(dp);
    }

    internal void ClearTriggerValue(DependencyProperty dp)
    {
        if (_triggerValues.Remove(dp))
        {
            // Trigger is no longer active; any post-trigger local override is now just a
            // regular local value, so remove the flag.
            _localOverridesActiveTrigger.Remove(dp);
            OnPropertyChanged(dp);
        }
    }

    protected virtual void OnPropertyChanged(DependencyProperty dp)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(dp.Name));
    }
}
