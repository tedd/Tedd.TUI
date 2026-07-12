using System;
using System.Windows.Input;

namespace Tedd.TUI.Platform.Wpf;

/// <summary>
/// Translates WPF keyboard state into the <see cref="ConsoleKey"/> /
/// <see cref="ConsoleModifiers"/> model the TUI input pipeline consumes.
/// </summary>
public static class WpfKeyMapper
{
    /// <summary>
    /// Maps a WPF <see cref="Key"/> to a <see cref="ConsoleKey"/>. Returns null for keys
    /// that should be delivered through TextInput instead (printable characters without
    /// Ctrl/Alt), so the host can decide which pipeline handles them.
    /// </summary>
    public static ConsoleKey? Map(Key key)
    {
        if (key >= Key.A && key <= Key.Z)
            return ConsoleKey.A + (key - Key.A);
        if (key >= Key.D0 && key <= Key.D9)
            return ConsoleKey.D0 + (key - Key.D0);
        if (key >= Key.NumPad0 && key <= Key.NumPad9)
            return ConsoleKey.NumPad0 + (key - Key.NumPad0);
        if (key >= Key.F1 && key <= Key.F24)
            return ConsoleKey.F1 + (key - Key.F1);

        return key switch
        {
            Key.Enter => ConsoleKey.Enter,
            Key.Escape => ConsoleKey.Escape,
            Key.Tab => ConsoleKey.Tab,
            Key.Back => ConsoleKey.Backspace,
            Key.Delete => ConsoleKey.Delete,
            Key.Insert => ConsoleKey.Insert,
            Key.Home => ConsoleKey.Home,
            Key.End => ConsoleKey.End,
            Key.PageUp => ConsoleKey.PageUp,
            Key.PageDown => ConsoleKey.PageDown,
            Key.Left => ConsoleKey.LeftArrow,
            Key.Right => ConsoleKey.RightArrow,
            Key.Up => ConsoleKey.UpArrow,
            Key.Down => ConsoleKey.DownArrow,
            Key.Space => ConsoleKey.Spacebar,
            _ => null
        };
    }

    /// <summary>
    /// True for keys the host must handle in KeyDown because they never (reliably) produce
    /// a TextInput event: navigation, editing and function keys.
    /// </summary>
    public static bool IsControlKey(Key key)
    {
        if (key >= Key.F1 && key <= Key.F24) return true;
        return key switch
        {
            Key.Enter or Key.Escape or Key.Tab or Key.Back or Key.Delete or Key.Insert
                or Key.Home or Key.End or Key.PageUp or Key.PageDown
                or Key.Left or Key.Right or Key.Up or Key.Down => true,
            _ => false
        };
    }

    public static ConsoleModifiers MapModifiers(ModifierKeys modifiers)
    {
        ConsoleModifiers m = 0;
        if ((modifiers & ModifierKeys.Control) != 0) m |= ConsoleModifiers.Control;
        if ((modifiers & ModifierKeys.Shift) != 0) m |= ConsoleModifiers.Shift;
        if ((modifiers & ModifierKeys.Alt) != 0) m |= ConsoleModifiers.Alt;
        return m;
    }

    /// <summary>Maps a typed character (from TextInput) to the closest ConsoleKey.</summary>
    public static ConsoleKey MapChar(char c)
    {
        if (c >= 'a' && c <= 'z') return ConsoleKey.A + (c - 'a');
        if (c >= 'A' && c <= 'Z') return ConsoleKey.A + (c - 'A');
        if (c >= '0' && c <= '9') return ConsoleKey.D0 + (c - '0');
        if (c == ' ') return ConsoleKey.Spacebar;
        return ConsoleKey.Packet;
    }
}
