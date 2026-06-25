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
    private readonly Dictionary<DependencyProperty, object> _styleValues = new();
    private readonly Dictionary<DependencyProperty, object> _styleTriggerValues = new();

    // Tracks properties for which SetValue was called while a trigger was active.
    // GetValue returns the local value for these, giving it precedence over the trigger.
    // Clearing the local value (ClearValue) removes the property from this set so that
    // the still-active trigger value is re-exposed instead of falling back to inherited/default.
    private readonly HashSet<DependencyProperty> _localOverridesActiveTrigger = new();
    private readonly HashSet<DependencyProperty> _localOverridesActiveStyleTrigger = new();

    protected virtual DependencyObject? InheritanceParent => null;

    public event PropertyChangedEventHandler? PropertyChanged;

    public object? GetValue(DependencyProperty dp)
    {
        // Local explicitly set overrides an active trigger.
        if (_localOverridesActiveTrigger.Contains(dp) && _localValues.TryGetValue(dp, out var overrideValue))
        {
            return overrideValue;
        }

        // Active template trigger values take precedence over everything else
        if (_triggerValues.TryGetValue(dp, out var triggerValue))
        {
            return triggerValue;
        }

        // Local explicit values set while an active style trigger is present override it.
        if (_localOverridesActiveStyleTrigger.Contains(dp) && _localValues.TryGetValue(dp, out var styleOverrideValue))
        {
            return styleOverrideValue;
        }

        // Active style trigger values take precedence over regular local values
        if (_styleTriggerValues.TryGetValue(dp, out var styleTriggerValue))
        {
            return styleTriggerValue;
        }

        // Local explicitly set values
        if (_localValues.TryGetValue(dp, out var localValue))
        {
            return localValue;
        }

        // Style explicitly set values
        if (_styleValues.TryGetValue(dp, out var styleValue))
        {
            return styleValue;
        }

        if (dp.IsInherited && InheritanceParent != null)
        {
            return InheritanceParent.GetValue(dp);
        }
        return dp.DefaultValue;
    }

    public void SetValue(DependencyProperty dp, object? value)
    {
        // Auto-coerce ConsoleColor -> TuiColor / TuiColor? for triggers, XAML, and other
        // boxed value paths where the compile-time implicit operator can't run.
        value = CoerceLegacyColor(dp, value);

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
        if (_styleTriggerValues.ContainsKey(dp))
        {
            _localOverridesActiveStyleTrigger.Add(dp);
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
        changed = changed || (_localOverridesActiveStyleTrigger.Remove(dp) && _styleTriggerValues.ContainsKey(dp));
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
        // Trigger setters and XAML attribute parsers frequently hand us a raw ConsoleColor
        // boxed value for a TuiColor / TuiColor? property; promote it transparently so the
        // legacy DPs stay source-compatible.
        value = CoerceLegacyColor(dp, value);

        // Basic type validation
        if (value != null && !dp.PropertyType.IsInstanceOfType(value))
        {
            throw new ArgumentException($"Value of type {value.GetType()} is not assignable to property {dp.Name} of type {dp.PropertyType}");
        }

        _triggerValues[dp] = value ?? null!;

        OnPropertyChanged(dp);
    }

    private static object? CoerceLegacyColor(DependencyProperty dp, object? value)
    {
        if (value is ConsoleColor cc)
        {
            if (dp.PropertyType == typeof(TuiColor) || dp.PropertyType == typeof(TuiColor?))
                return TuiColor.FromConsole(cc);
        }
        return value;
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

    internal void SetStyleValue(DependencyProperty dp, object? value)
    {
        value = CoerceLegacyColor(dp, value);

        if (value != null && !dp.PropertyType.IsInstanceOfType(value))
        {
            throw new ArgumentException($"Value of type {value.GetType()} is not assignable to property {dp.Name} of type {dp.PropertyType}");
        }

        _styleValues[dp] = value ?? null!;
        OnPropertyChanged(dp);
    }

    internal void ClearAllStyleValues()
    {
        var keys = new List<DependencyProperty>(_styleValues.Keys);
        _styleValues.Clear();
        foreach (var key in keys)
        {
            OnPropertyChanged(key);
        }
    }

    internal void SetStyleTriggerValue(DependencyProperty dp, object? value)
    {
        value = CoerceLegacyColor(dp, value);

        if (value != null && !dp.PropertyType.IsInstanceOfType(value))
        {
            throw new ArgumentException($"Value of type {value.GetType()} is not assignable to property {dp.Name} of type {dp.PropertyType}");
        }

        _styleTriggerValues[dp] = value ?? null!;
        OnPropertyChanged(dp);
    }

    internal void ClearStyleTriggerValue(DependencyProperty dp)
    {
        if (_styleTriggerValues.Remove(dp))
        {
            _localOverridesActiveStyleTrigger.Remove(dp);
            OnPropertyChanged(dp);
        }
    }

    protected virtual void OnPropertyChanged(DependencyProperty dp)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(dp.Name));
    }
}
