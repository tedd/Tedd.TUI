using System;
using Microsoft.UI.Input;
using Windows.System;
using Windows.UI.Core;

namespace Tedd.TUI.Platform.WinUI;

/// <summary>
/// Translates WinUI keyboard state into the <see cref="ConsoleKey"/> /
/// <see cref="ConsoleModifiers"/> model the TUI input pipeline consumes.
/// </summary>
public static class WinUIKeyMapper
{
    /// <summary>Maps a WinUI <see cref="VirtualKey"/>. Returns null for keys delivered via CharacterReceived.</summary>
    public static ConsoleKey? Map(VirtualKey key)
    {
        // VirtualKey letter/digit values match the VK_* codes ConsoleKey mirrors.
        if (key >= VirtualKey.A && key <= VirtualKey.Z)
            return ConsoleKey.A + (key - VirtualKey.A);
        if (key >= VirtualKey.Number0 && key <= VirtualKey.Number9)
            return ConsoleKey.D0 + (key - VirtualKey.Number0);
        if (key >= VirtualKey.NumberPad0 && key <= VirtualKey.NumberPad9)
            return ConsoleKey.NumPad0 + (key - VirtualKey.NumberPad0);
        if (key >= VirtualKey.F1 && key <= VirtualKey.F24)
            return ConsoleKey.F1 + (key - VirtualKey.F1);

        return key switch
        {
            VirtualKey.Enter => ConsoleKey.Enter,
            VirtualKey.Escape => ConsoleKey.Escape,
            VirtualKey.Tab => ConsoleKey.Tab,
            VirtualKey.Back => ConsoleKey.Backspace,
            VirtualKey.Delete => ConsoleKey.Delete,
            VirtualKey.Insert => ConsoleKey.Insert,
            VirtualKey.Home => ConsoleKey.Home,
            VirtualKey.End => ConsoleKey.End,
            VirtualKey.PageUp => ConsoleKey.PageUp,
            VirtualKey.PageDown => ConsoleKey.PageDown,
            VirtualKey.Left => ConsoleKey.LeftArrow,
            VirtualKey.Right => ConsoleKey.RightArrow,
            VirtualKey.Up => ConsoleKey.UpArrow,
            VirtualKey.Down => ConsoleKey.DownArrow,
            VirtualKey.Space => ConsoleKey.Spacebar,
            _ => null
        };
    }

    /// <summary>True for keys that must be handled in KeyDown because they produce no CharacterReceived.</summary>
    public static bool IsControlKey(VirtualKey key)
    {
        if (key >= VirtualKey.F1 && key <= VirtualKey.F24) return true;
        return key switch
        {
            VirtualKey.Enter or VirtualKey.Escape or VirtualKey.Tab or VirtualKey.Back
                or VirtualKey.Delete or VirtualKey.Insert or VirtualKey.Home or VirtualKey.End
                or VirtualKey.PageUp or VirtualKey.PageDown
                or VirtualKey.Left or VirtualKey.Right or VirtualKey.Up or VirtualKey.Down => true,
            _ => false
        };
    }

    /// <summary>Reads the current Ctrl/Shift/Alt state from the thread's keyboard source.</summary>
    public static ConsoleModifiers GetCurrentModifiers()
    {
        ConsoleModifiers m = 0;
        if (IsDown(VirtualKey.Control)) m |= ConsoleModifiers.Control;
        if (IsDown(VirtualKey.Shift)) m |= ConsoleModifiers.Shift;
        if (IsDown(VirtualKey.Menu)) m |= ConsoleModifiers.Alt;
        return m;
    }

    private static bool IsDown(VirtualKey key) =>
        (InputKeyboardSource.GetKeyStateForCurrentThread(key) & CoreVirtualKeyStates.Down) != 0;

    /// <summary>Maps a typed character (from CharacterReceived) to the closest ConsoleKey.</summary>
    public static ConsoleKey MapChar(char c)
    {
        if (c >= 'a' && c <= 'z') return ConsoleKey.A + (c - 'a');
        if (c >= 'A' && c <= 'Z') return ConsoleKey.A + (c - 'A');
        if (c >= '0' && c <= '9') return ConsoleKey.D0 + (c - '0');
        if (c == ' ') return ConsoleKey.Spacebar;
        return ConsoleKey.Packet;
    }
}
