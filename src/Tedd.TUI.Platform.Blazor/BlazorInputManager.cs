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

    public void QueueMouse(Microsoft.AspNetCore.Components.Web.MouseEventArgs e, string type)
    {
        // Map pixel to cell
        int x = (int)(e.OffsetX / CharWidth);
        int y = (int)(e.OffsetY / CharHeight);

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
                GlobalY = y
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
