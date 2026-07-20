using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Reflection;

namespace Tedd.TUI.Data;

public enum BindingMode
{
    OneWay,
    TwoWay,
    OneTime,
    OneWayToSource,
    /// <summary>
    /// Resolves at attach time: TwoWay for properties registered with
    /// BindsTwoWayByDefault (TextBox.Text, ToggleButton.IsChecked, ...), OneWay otherwise.
    /// This mirrors WPF, where an unspecified Mode picks up the property's default.
    /// </summary>
    Default
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
    /// <summary>Name (x:Name / Name) of another element in the same tree to use as source.</summary>
    public string? ElementName { get; set; }
    public RelativeSource? RelativeSource { get; set; }
    public BindingMode Mode { get; set; } = BindingMode.Default;
    public IValueConverter? Converter { get; set; }
    public object? ConverterParameter { get; set; }
    public object? FallbackValue { get; set; }
    /// <summary>Value pushed to the target when the binding resolves to null.</summary>
    public object? TargetNullValue { get; set; }
    /// <summary>
    /// Format applied when the target property is a string. Either a composite format
    /// ("{0:N0} items") or a bare format specifier ("N0", treated as "{0:N0}").
    /// </summary>
    public string? StringFormat { get; set; }

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
    /// <summary>Sentinel for "the path could not be resolved" (distinct from a resolved null).</summary>
    private static readonly object Unresolved = new object();

    private readonly UIElement _target;
    private readonly DependencyProperty _property;
    private readonly Binding _binding;
    // "" and "." both mean "the source object itself".
    private readonly string[] _pathSegments;
    // INPC sources subscribed along the path; _chainProps[i] is the segment watched on
    // _chainObjects[i]. Rebuilt on every UpdateTarget so intermediate object swaps
    // (vm.Child = other) re-hook the tail of the chain.
    private readonly List<INotifyPropertyChanged> _chainObjects = [];
    private readonly List<string> _chainProps = [];
    // Effective mode: Binding.Mode with BindingMode.Default resolved against the
    // target property's BindsTwoWayByDefault registration flag.
    private BindingMode _mode;
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
        _pathSegments = string.IsNullOrEmpty(binding.Path) || binding.Path == "."
            ? Array.Empty<string>()
            : binding.Path.Split('.');
    }

    /// <summary>
    /// Activates the expression: resolves the effective mode, wires target-change
    /// tracking for modes that write back to the source, and performs the initial
    /// value transfer in the mode's direction.
    /// </summary>
    internal void Attach()
    {
        _mode = _binding.Mode == BindingMode.Default
            ? (_property.BindsTwoWayByDefault ? BindingMode.TwoWay : BindingMode.OneWay)
            : _binding.Mode;

        if (_mode is BindingMode.TwoWay or BindingMode.OneWayToSource)
        {
            _target.PropertyChanged += OnTargetPropertyChanged;
            _targetSubscribed = true;
        }

        if (_mode == BindingMode.OneWayToSource)
        {
            UpdateSource();
        }
        else
        {
            UpdateTarget();
        }
    }

    /// <summary>
    /// Deactivates the expression, dropping the source INPC subscriptions and the
    /// target subscription so a replaced binding cannot keep updating (or leak).
    /// </summary>
    internal void Detach()
    {
        UnsubscribeChain();

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

        if (!string.IsNullOrEmpty(_binding.ElementName))
        {
            // Resolved from the tree root so forward references work once the tree is
            // assembled; bindings re-resolve on parent/DataContext changes and after
            // XamlLoader finishes building the tree.
            return _target.GetRoot()?.FindName(_binding.ElementName);
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
        // OneWayToSource never writes the target; a source change (e.g. new DataContext)
        // re-resolves and pushes the target's current value into the new source instead.
        if (_mode == BindingMode.OneWayToSource)
        {
            UpdateSource();
            return;
        }

        UnsubscribeChain();

        object? source = ResolveSource();
        if (source == null)
        {
            // WPF keeps the target's current value when the source is missing unless a
            // FallbackValue is provided.
            if (_binding.FallbackValue != null)
            {
                SetTargetValue(CoerceFallback(_binding.FallbackValue));
            }
            return;
        }

        object? value = EvaluateAndSubscribe(source);
        if (ReferenceEquals(value, Unresolved))
        {
            // Broken path: FallbackValue (no converter/format applied), else the
            // property's registration default.
            SetTargetValue(_binding.FallbackValue != null
                ? CoerceFallback(_binding.FallbackValue)
                : _property.DefaultValue);
            return;
        }

        if (_binding.Converter != null && value != null)
        {
            value = _binding.Converter.Convert(value, _property.PropertyType, _binding.ConverterParameter!, CultureInfo.CurrentCulture);
        }

        if (value == null)
        {
            if (_binding.TargetNullValue != null)
            {
                value = _binding.TargetNullValue;
            }
        }
        else if (_binding.StringFormat != null && _property.PropertyType == typeof(string))
        {
            value = FormatValue(value);
        }

        value = ConvertToTargetType(value, out bool converted);
        if (!converted)
        {
            value = _binding.FallbackValue ?? _property.DefaultValue;
        }

        SetTargetValue(value);
    }

    /// <summary>
    /// Walks the path from <paramref name="source"/>, subscribing (except for OneTime
    /// bindings) to change notification on every INPC object that exposes a segment,
    /// so a swap anywhere along "A.B.C" re-evaluates the binding. Returns the resolved
    /// value, or <see cref="Unresolved"/> when a segment is missing or an intermediate
    /// object is null.
    /// </summary>
    private object? EvaluateAndSubscribe(object source)
    {
        object? current = source;
        for (int i = 0; i < _pathSegments.Length; i++)
        {
            if (current == null) return Unresolved;

            if (_mode != BindingMode.OneTime && current is INotifyPropertyChanged npc)
            {
                npc.PropertyChanged += OnSourcePropertyChanged;
                _chainObjects.Add(npc);
                _chainProps.Add(_pathSegments[i]);
            }

            var propInfo = current.GetType().GetProperty(_pathSegments[i]);
            if (propInfo == null) return Unresolved;
            current = propInfo.GetValue(current);
        }
        return current;
    }

    private void UnsubscribeChain()
    {
        for (int i = 0; i < _chainObjects.Count; i++)
        {
            _chainObjects[i].PropertyChanged -= OnSourcePropertyChanged;
        }
        _chainObjects.Clear();
        _chainProps.Clear();
    }

    private object FormatValue(object value)
    {
        string format = _binding.StringFormat!;
        // A bare specifier ("N0", "yyyy-MM-dd") formats the value itself, matching
        // WPF's treatment of StringFormat without a composite placeholder.
        if (format.IndexOf('{') < 0)
        {
            format = "{0:" + format + "}";
        }
        return string.Format(CultureInfo.CurrentCulture, format, value);
    }

    /// <summary>
    /// Coerces a resolved value to the target property's type the way WPF's default
    /// conversion does: pass-through when assignable, ToString for string targets,
    /// enum parsing, and IConvertible changes for the rest. A null for a non-nullable
    /// value type property becomes the property default. On failure the caller falls
    /// back to FallbackValue / default instead of throwing out of input handling.
    /// </summary>
    private object? ConvertToTargetType(object? value, out bool success)
    {
        success = true;
        Type targetType = _property.PropertyType;

        if (value == null)
        {
            if (targetType.IsValueType && Nullable.GetUnderlyingType(targetType) == null)
            {
                return _property.DefaultValue;
            }
            return null;
        }

        if (targetType.IsInstanceOfType(value)) return value;

        Type underlying = Nullable.GetUnderlyingType(targetType) ?? targetType;
        if (underlying.IsInstanceOfType(value)) return value;

        try
        {
            if (underlying == typeof(string))
            {
                return System.Convert.ToString(value, CultureInfo.CurrentCulture);
            }
            if (underlying.IsEnum)
            {
                return value is string s
                    ? Enum.Parse(underlying, s, ignoreCase: true)
                    : Enum.ToObject(underlying, System.Convert.ChangeType(value, Enum.GetUnderlyingType(underlying), CultureInfo.CurrentCulture)!);
            }
            if (underlying == typeof(TuiColor) && value is string colorText)
            {
                return TuiColor.FromHex(colorText);
            }
            return System.Convert.ChangeType(value, underlying, CultureInfo.CurrentCulture);
        }
        catch
        {
            success = false;
            return null;
        }
    }

    /// <summary>
    /// FallbackValue frequently arrives as a XAML attribute string; coerce it to the
    /// target property type, keeping it verbatim when conversion is impossible.
    /// </summary>
    private object? CoerceFallback(object fallback)
    {
        object? coerced = ConvertToTargetType(fallback, out bool converted);
        return converted ? coerced : fallback;
    }

    private void SetTargetValue(object? value)
    {
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

        object? source = ResolveSource();
        if (source == null) return;
        if (_pathSegments.Length == 0) return;

        // Walk to the object owning the last segment.
        object? current = source;
        for (int i = 0; i < _pathSegments.Length - 1 && current != null; i++)
        {
            current = current.GetType().GetProperty(_pathSegments[i])?.GetValue(current);
        }
        if (current == null) return;

        var propInfo = current.GetType().GetProperty(_pathSegments[^1]);
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
                value = targetType.IsEnum && value is string enumText
                    ? Enum.Parse(targetType, enumText, ignoreCase: true)
                    : System.Convert.ChangeType(value, targetType, CultureInfo.CurrentCulture);
            }

            // Skip no-op writes so INPC sources don't echo the value straight back.
            if (Equals(propInfo.GetValue(current), value)) return;

            _isUpdating = true;
            try
            {
                propInfo.SetValue(current, value);
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

    private void OnSourcePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (string.IsNullOrEmpty(e.PropertyName))
        {
            UpdateTarget();
            return;
        }

        for (int i = 0; i < _chainObjects.Count; i++)
        {
            if (ReferenceEquals(_chainObjects[i], sender) && _chainProps[i] == e.PropertyName)
            {
                UpdateTarget();
                return;
            }
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
