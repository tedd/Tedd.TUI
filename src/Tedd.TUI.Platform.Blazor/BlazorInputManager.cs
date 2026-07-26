using System;
using System.Collections.Concurrent;
using Microsoft.AspNetCore.Components.Web;
using Tedd.TUI;

namespace Tedd.TUI.Platform.Blazor;

public class BlazorInputManager
{
    private readonly TuiWindow _window;
    private readonly ConcurrentQueue<Action> _eventQueue = new();

    public event Action? InputAvailable;

    public int CharWidth { get; set; } = 10;
    public int CharHeight { get; set; } = 18;

    public BlazorInputManager(TuiWindow window)
    {
        _window = window;
    }

    public void ProcessInput()
    {
        while (_eventQueue.TryDequeue(out var action))
        {
            action();
        }
    }

    public void QueueKey(KeyboardEventArgs e, bool isDown)
    {
        if (!isDown) return;

        var key = MapKey(e.Key);

        char keyChar = (e.Key.Length == 1) ? e.Key[0] : '\0';

        var modifiers = GetModifiers(e);

        var args = new KeyEventArgs
        {
            Key = key,
            KeyChar = keyChar,
            Modifiers = modifiers
        };

        _eventQueue.Enqueue(() => _window.ProcessKey(args));
        InputAvailable?.Invoke();
    }

    /// <summary>
    /// Queues a browser wheel event as a TUI <see cref="MouseWheelEventArgs"/> so
    /// <see cref="ScrollViewer"/>/<see cref="Controls.Primitives.ScrollBar"/> under the
    /// pointer scroll. Browser deltas are normalized to the WPF convention Tedd.TUI uses
    /// everywhere else: ±<see cref="MouseWheelEventArgs.WheelNotch"/> (120) per physical
    /// notch, positive when scrolling up/away from the user.
    /// </summary>
    public void QueueWheel(WheelEventArgs e)
    {
        // DeltaY is positive when scrolling down, opposite of the TUI/WPF sign convention.
        int delta = -(int)Math.Round(NormalizeWheelDelta(e.DeltaY, e.DeltaMode));
        if (delta == 0)
            return;

        double fx = e.OffsetX / CharWidth;
        double fy = e.OffsetY / CharHeight;

        _eventQueue.Enqueue(() =>
        {
            _window.ProcessMouse(new MouseWheelEventArgs(UIElement.MouseWheelEvent)
            {
                GlobalX = (int)fx,
                GlobalY = (int)fy,
                GlobalXF = fx,
                GlobalYF = fy,
                Modifiers = GetModifiers(e),
                Delta = delta
            });
        });
        InputAvailable?.Invoke();
    }

    /// <summary>
    /// Converts a <c>WheelEvent</c> delta into TUI notch units. <c>deltaMode</c> varies by
    /// browser and device — Chrome reports pixels, Firefox commonly reports lines — so each
    /// mode is scaled by how much one notch means in that unit. Fractional results are
    /// intentional: consumers accumulate them, so trackpads still scroll smoothly.
    /// </summary>
    internal static double NormalizeWheelDelta(double delta, long deltaMode) => deltaMode switch
    {
        // Lines: browsers send one notch as the system "lines per notch" setting (3 by default).
        1 => delta / 3.0 * MouseWheelEventArgs.WheelNotch,
        // Pages: one notch is a full page.
        2 => delta * MouseWheelEventArgs.WheelNotch,
        // Pixels: ~100 CSS px per notch is what Chrome/Edge emit.
        _ => delta / 100.0 * MouseWheelEventArgs.WheelNotch
    };

    /// <summary>
    /// Queues a browser mouse event as the matching TUI routed event, mapping pixels to cells.
    /// </summary>
    /// <remarks>
    /// Fractional cell coordinates are carried alongside the integer ones: a browser reports
    /// pixels, and <see cref="Controls.Primitives.ScrollBar"/> maps a drag through
    /// <c>GlobalXF</c>/<c>GlobalYF</c> so the thumb tracks the pointer smoothly instead of
    /// snapping a whole cell at a time. Without them those properties fall back to the centre
    /// of the integer cell — all a terminal can report, but a needless loss here.
    /// </remarks>
    public void QueueMouse(Microsoft.AspNetCore.Components.Web.MouseEventArgs e, string type)
    {
        // Map pixel to cell, keeping the sub-cell remainder.
        double fx = e.OffsetX / CharWidth;
        double fy = e.OffsetY / CharHeight;
        int x = (int)Math.Floor(fx);
        int y = (int)Math.Floor(fy);
        var modifiers = GetModifiers(e);

        _eventQueue.Enqueue(() =>
        {
            RoutedEvent? routedEvent = type switch
            {
                "mousedown" when e.Button == 0 => UIElement.MouseDownEvent,
                "mouseup" when e.Button == 0 => UIElement.MouseUpEvent,
                "mousemove" => UIElement.MouseMoveEvent,
                _ => null
            };

            if (routedEvent == null)
                return;

            _window.ProcessMouse(new Tedd.TUI.MouseEventArgs(routedEvent)
            {
                GlobalX = x,
                GlobalY = y,
                GlobalXF = fx,
                GlobalYF = fy,
                Modifiers = modifiers
            });
        });
        InputAvailable?.Invoke();
    }

    private ConsoleModifiers GetModifiers(KeyboardEventArgs e)
    {
        ConsoleModifiers m = 0;
        if (e.CtrlKey) m |= ConsoleModifiers.Control;
        if (e.ShiftKey) m |= ConsoleModifiers.Shift;
        if (e.AltKey) m |= ConsoleModifiers.Alt;
        return m;
    }

    private static ConsoleModifiers GetModifiers(Microsoft.AspNetCore.Components.Web.MouseEventArgs e)
    {
        ConsoleModifiers m = 0;
        if (e.CtrlKey) m |= ConsoleModifiers.Control;
        if (e.ShiftKey) m |= ConsoleModifiers.Shift;
        if (e.AltKey) m |= ConsoleModifiers.Alt;
        return m;
    }

    private ConsoleKey MapKey(string key)
    {
        if (key.Length == 1)
        {
            var c = key[0];
            if (c >= 'a' && c <= 'z') return (ConsoleKey)(c - 'a' + (int)ConsoleKey.A);
            if (c >= 'A' && c <= 'Z') return (ConsoleKey)(c - 'A' + (int)ConsoleKey.A);
            if (c >= '0' && c <= '9') return (ConsoleKey)(c - '0' + (int)ConsoleKey.D0);
        }

        return key switch
        {
            "Enter" => ConsoleKey.Enter,
            "Backspace" => ConsoleKey.Backspace,
            "Tab" => ConsoleKey.Tab,
            "Escape" => ConsoleKey.Escape,
            "ArrowUp" => ConsoleKey.UpArrow,
            "ArrowDown" => ConsoleKey.DownArrow,
            "ArrowLeft" => ConsoleKey.LeftArrow,
            "ArrowRight" => ConsoleKey.RightArrow,
            "Delete" => ConsoleKey.Delete,
            "Insert" => ConsoleKey.Insert,
            "Home" => ConsoleKey.Home,
            "End" => ConsoleKey.End,
            "PageUp" => ConsoleKey.PageUp,
            "PageDown" => ConsoleKey.PageDown,
            "F1" => ConsoleKey.F1,
            "F2" => ConsoleKey.F2,
            "F3" => ConsoleKey.F3,
            "F4" => ConsoleKey.F4,
            "F5" => ConsoleKey.F5,
            "F6" => ConsoleKey.F6,
            "F7" => ConsoleKey.F7,
            "F8" => ConsoleKey.F8,
            "F9" => ConsoleKey.F9,
            "F10" => ConsoleKey.F10,
            "F11" => ConsoleKey.F11,
            "F12" => ConsoleKey.F12,
            " " => ConsoleKey.Spacebar,
            _ => ConsoleKey.Process
        };
    }
}
