using System;
using System.Collections.Generic;
using System.Threading;

namespace Tedd.TUI;

/// <summary>Payload for <see cref="ThemeManager.ThemeChanged"/>.</summary>
public sealed class ThemeChangedEventArgs : EventArgs
{
    public ThemeChangedEventArgs(TuiTheme oldTheme, TuiTheme newTheme)
    {
        OldTheme = oldTheme;
        NewTheme = newTheme;
    }

    public TuiTheme OldTheme { get; }
    public TuiTheme NewTheme { get; }
}

/// <summary>
/// Publishes the application-wide <see cref="TuiTheme"/>, playing the role that
/// <c>Application.Current.Resources</c> merged theme dictionaries play in WPF.
/// <see cref="DependencyObject.GetValue"/> consults <see cref="Current"/> when a
/// property has no local or trigger value, so assigning a new theme restyles every
/// element that has not been explicitly overridden.
/// </summary>
/// <remarks>
/// <para>Typical usage: <c>ThemeManager.Current = TuiThemes.TurboPascal;</c></para>
/// <para>Attached <see cref="TuiWindow"/>s are tracked weakly and re-rendered
/// automatically when the theme changes.</para>
/// </remarks>
public static class ThemeManager
{
    private static readonly System.Threading.Lock _sync = new();
    private static readonly List<WeakReference<TuiWindow>> _windows = new();
    private static TuiTheme _current = TuiThemes.Dark;

    // Ambient override so tests (and embedded hosts) can theme a single async flow
    // without touching the process-global theme other flows are rendering with.
    private static readonly AsyncLocal<TuiTheme?> _ambient = new();

    /// <summary>
    /// The active theme. Never null; defaults to <see cref="TuiThemes.Dark"/>.
    /// Assigning a new theme raises <see cref="ThemeChanged"/> and re-renders all
    /// live windows. An ambient scope opened with <see cref="BeginScope"/> takes
    /// precedence for the current async flow.
    /// </summary>
    public static TuiTheme Current
    {
        get => _ambient.Value ?? _current;
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            TuiTheme old;
            lock (_sync)
            {
                old = _current;
                if (ReferenceEquals(old, value)) return;
                _current = value;
            }

            ThemeChanged?.Invoke(null, new ThemeChangedEventArgs(old, value));
            NotifyWindows();
        }
    }

    /// <summary>Raised after the global theme is replaced (not for ambient scopes).</summary>
    public static event EventHandler<ThemeChangedEventArgs>? ThemeChanged;

    /// <summary>
    /// Overrides <see cref="Current"/> for the current async flow until the returned
    /// scope is disposed. Does not raise <see cref="ThemeChanged"/> or notify windows;
    /// intended for tests and for hosts embedding differently-themed islands.
    /// </summary>
    public static IDisposable BeginScope(TuiTheme theme)
    {
        ArgumentNullException.ThrowIfNull(theme);
        var scope = new ThemeScope(_ambient.Value);
        _ambient.Value = theme;
        return scope;
    }

    private sealed class ThemeScope : IDisposable
    {
        private readonly TuiTheme? _previous;
        private bool _disposed;

        public ThemeScope(TuiTheme? previous) => _previous = previous;

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _ambient.Value = _previous;
        }
    }

    /// <summary>
    /// Tracks a window (weakly) so it is refreshed when the theme changes.
    /// Called from the <see cref="TuiWindow"/> constructor.
    /// </summary>
    internal static void RegisterWindow(TuiWindow window)
    {
        lock (_sync)
        {
            for (int i = _windows.Count - 1; i >= 0; i--)
            {
                if (!_windows[i].TryGetTarget(out var existing))
                    _windows.RemoveAt(i);
                else if (ReferenceEquals(existing, window))
                    return;
            }

            _windows.Add(new WeakReference<TuiWindow>(window));
        }
    }

    private static void NotifyWindows()
    {
        List<TuiWindow>? alive = null;
        lock (_sync)
        {
            for (int i = _windows.Count - 1; i >= 0; i--)
            {
                if (_windows[i].TryGetTarget(out var window))
                    (alive ??= new()).Add(window);
                else
                    _windows.RemoveAt(i);
            }
        }

        if (alive == null) return;
        foreach (var window in alive)
            window.OnGlobalThemeChanged();
    }
}
