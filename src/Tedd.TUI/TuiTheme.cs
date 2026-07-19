using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Tedd.TUI;

/// <summary>
/// A named collection of implicit <see cref="Style"/>s plus loose keyed resources —
/// the Tedd.TUI equivalent of a XAML theme <c>ResourceDictionary</c>
/// (<c>Themes/Dark.xaml</c> and friends). The active theme is published through
/// <see cref="ThemeManager.Current"/>; elements resolve un-set dependency properties
/// against it automatically, so swapping the theme restyles the whole application.
/// </summary>
/// <remarks>
/// <para>Predefined themes live in <see cref="TuiThemes"/>. Custom themes are plain
/// instances: create one, add styles, assign it to <see cref="ThemeManager.Current"/>.</para>
/// <para>Style lookups are cached per element type for render-loop performance. A theme
/// that is mutated after it has been used must call <see cref="InvalidateCache"/> for
/// the changes to become visible.</para>
/// </remarks>
public class TuiTheme
{
    public TuiTheme(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Name = name;
    }

    /// <summary>Display name of the theme ("Dark", "TurboPascal", ...).</summary>
    public string Name { get; }

    /// <summary>The implicit styles making up this theme.</summary>
    public List<Style> Styles { get; } = new();

    /// <summary>
    /// Loose keyed resources (a XAML ResourceDictionary analogue) for values that are
    /// not tied to a dependency property. Reserved for application use.
    /// </summary>
    public Dictionary<string, object?> Resources { get; } = new();

    // Per-element-type merged setter tables. A null entry is a cached miss so types
    // with no styles stay a single lookup on the GetValue fall-through path.
    private readonly ConcurrentDictionary<Type, Dictionary<DependencyProperty, object?>?> _mergedStyleCache = new();

    /// <summary>Creates a style for <paramref name="targetType"/>, adds it, and returns it.</summary>
    public Style StyleFor(Type targetType)
    {
        ArgumentNullException.ThrowIfNull(targetType);
        var style = new Style(targetType);
        Styles.Add(style);
        InvalidateCache();
        return style;
    }

    /// <summary>
    /// Drops the internal style lookup cache. Call after mutating <see cref="Styles"/>
    /// (or a contained style) once the theme has already been used for rendering.
    /// </summary>
    public void InvalidateCache() => _mergedStyleCache.Clear();

    /// <summary>Looks up a loose resource by key.</summary>
    public bool TryGetResource(string key, out object? value) => Resources.TryGetValue(key, out value);

    /// <summary>
    /// Resolves the effective style value of <paramref name="dp"/> for an element of
    /// <paramref name="type"/>, honoring target-type inheritance (a style for
    /// <c>ButtonBase</c> applies to <c>Button</c>, the most derived match wins).
    /// </summary>
    internal bool TryGetStyleValue(Type type, DependencyProperty dp, out object? value)
    {
        var merged = _mergedStyleCache.GetOrAdd(type, BuildMergedStyle);
        if (merged != null && merged.TryGetValue(dp, out value))
            return true;

        value = null;
        return false;
    }

    private Dictionary<DependencyProperty, object?>? BuildMergedStyle(Type type)
    {
        List<(Style Style, int Depth, int Index)>? matches = null;
        for (int i = 0; i < Styles.Count; i++)
        {
            var style = Styles[i];
            if (style.TargetType != null && style.TargetType.IsAssignableFrom(type))
                (matches ??= new()).Add((style, TypeDepth(style.TargetType), i));
        }

        if (matches == null) return null;

        // Base-type styles first so more derived target types override shared
        // properties; declaration order breaks ties (later styles win).
        matches.Sort((a, b) =>
        {
            int cmp = a.Depth.CompareTo(b.Depth);
            return cmp != 0 ? cmp : a.Index.CompareTo(b.Index);
        });

        var merged = new Dictionary<DependencyProperty, object?>();
        foreach (var (style, _, _) in matches)
        {
            foreach (var setter in style.Setters)
            {
                if (setter.Property == null) continue;
                merged[setter.Property] = DependencyObject.CoerceLegacyColor(setter.Property, setter.Value);
            }
        }

        return merged.Count > 0 ? merged : null;
    }

    private static int TypeDepth(Type type)
    {
        int depth = 0;
        for (var t = type; t != null; t = t.BaseType) depth++;
        return depth;
    }
}
