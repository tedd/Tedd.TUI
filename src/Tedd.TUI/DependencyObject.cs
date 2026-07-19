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
    /// <summary>
    /// When true, a Binding whose Mode is left at BindingMode.Default binds TwoWay to
    /// this property (WPF's FrameworkPropertyMetadata.BindsTwoWayByDefault). Set on
    /// user-input properties such as TextBox.Text and ToggleButton.IsChecked.
    /// </summary>
    public bool BindsTwoWayByDefault { get; }

    private DependencyProperty(string name, Type propertyType, Type ownerType, object? defaultValue, bool isInherited, bool bindsTwoWayByDefault)
    {
        Name = name;
        PropertyType = propertyType;
        OwnerType = ownerType;
        DefaultValue = defaultValue;
        IsInherited = isInherited;
        BindsTwoWayByDefault = bindsTwoWayByDefault;
    }

    public static DependencyProperty Register(string name, Type propertyType, Type ownerType, object? defaultValue = null, bool isInherited = false, bool bindsTwoWayByDefault = false)
    {
        return new DependencyProperty(name, propertyType, ownerType, defaultValue, isInherited, bindsTwoWayByDefault);
    }

    public static DependencyProperty RegisterAttached(string name, Type propertyType, Type ownerType, object? defaultValue = null, bool isInherited = false)
    {
        // Attached properties are essentially the same structure in this simple implementation
        return new DependencyProperty(name, propertyType, ownerType, defaultValue, isInherited, bindsTwoWayByDefault: false);
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
        // Theme style values rank below local/trigger values but above inheritance and
        // registration defaults, matching WPF's precedence for theme styles. Inherited
        // properties still resolve theme styles of ancestors naturally because the
        // parent walk re-enters GetValue on each ancestor.
        if (ThemeManager.Current.TryGetStyleValue(GetType(), dp, out var themeValue))
        {
            return themeValue;
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

        ValidateValue(dp, value);

        object? oldEffective = GetValue(dp);

        // The local value is always stored, even when it doesn't change the effective
        // value (e.g. it equals an active trigger's value): it must survive as the
        // fallback once the trigger deactivates.
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

        // Notify only when the effective value actually changed. Without this guard a
        // no-op write (same value) still bubbled Invalidate() to the window; controls
        // that write properties during Measure/Render then re-armed the render loop
        // every frame, pinning a core at 100% CPU while the app was idle.
        if (!Equals(oldEffective, GetValue(dp)))
        {
            OnPropertyChanged(dp);
        }
    }

    private static void ValidateValue(DependencyProperty dp, object? value)
    {
        if (value == null)
        {
            // Null is only assignable to reference types and Nullable<T>; letting it
            // through for e.g. an int property would defer the failure to an unboxing
            // cast at some unrelated GetValue call site.
            if (dp.PropertyType.IsValueType && Nullable.GetUnderlyingType(dp.PropertyType) == null)
            {
                throw new ArgumentException($"Null is not assignable to property {dp.Name} of non-nullable type {dp.PropertyType}");
            }
            return;
        }

        if (!dp.PropertyType.IsInstanceOfType(value))
        {
            throw new ArgumentException($"Value of type {value.GetType()} is not assignable to property {dp.Name} of type {dp.PropertyType}");
        }
    }

    public void ClearValue(DependencyProperty dp)
    {
        object? oldEffective = GetValue(dp);

        // Clearing the local value removes the post-trigger override flag; the trigger
        // value (if still present) will naturally surface from GetValue.
        bool removedLocal = _localValues.Remove(dp);
        bool removedOverride = _localOverridesActiveTrigger.Remove(dp);
        if (!removedLocal && !removedOverride) return;

        if (!Equals(oldEffective, GetValue(dp)))
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

        ValidateValue(dp, value);

        object? oldEffective = GetValue(dp);
        _triggerValues[dp] = value ?? null!;

        if (!Equals(oldEffective, GetValue(dp)))
        {
            OnPropertyChanged(dp);
        }
    }

    internal static object? CoerceLegacyColor(DependencyProperty dp, object? value)
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
        object? oldEffective = GetValue(dp);
        if (_triggerValues.Remove(dp))
        {
            // Trigger is no longer active; any post-trigger local override is now just a
            // regular local value, so remove the flag.
            _localOverridesActiveTrigger.Remove(dp);

            if (!Equals(oldEffective, GetValue(dp)))
            {
                OnPropertyChanged(dp);
            }
        }
    }

    protected virtual void OnPropertyChanged(DependencyProperty dp)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(dp.Name));
    }
}
