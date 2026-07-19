using System;
using SDL2;

namespace Tedd.TUI.Platform.Sdl2;

/// <summary>
/// Translates SDL2 keyboard state into the <see cref="ConsoleKey"/> /
/// <see cref="ConsoleModifiers"/> model the TUI input pipeline consumes.
/// </summary>
public static class Sdl2KeyMapper
{
    /// <summary>
    /// Maps an SDL <see cref="SDL.SDL_Keycode"/> to a <see cref="ConsoleKey"/>. Returns null
    /// for keys that should be delivered through SDL_TEXTINPUT instead (printable characters
    /// without Ctrl/Alt), so the host can decide which pipeline handles them.
    /// </summary>
    public static ConsoleKey? Map(SDL.SDL_Keycode key)
    {
        if (key >= SDL.SDL_Keycode.SDLK_a && key <= SDL.SDL_Keycode.SDLK_z)
            return ConsoleKey.A + (key - SDL.SDL_Keycode.SDLK_a);
        if (key >= SDL.SDL_Keycode.SDLK_0 && key <= SDL.SDL_Keycode.SDLK_9)
            return ConsoleKey.D0 + (key - SDL.SDL_Keycode.SDLK_0);
        if (key >= SDL.SDL_Keycode.SDLK_KP_1 && key <= SDL.SDL_Keycode.SDLK_KP_9)
            return ConsoleKey.NumPad1 + (key - SDL.SDL_Keycode.SDLK_KP_1);
        if (key >= SDL.SDL_Keycode.SDLK_F1 && key <= SDL.SDL_Keycode.SDLK_F12)
            return ConsoleKey.F1 + (key - SDL.SDL_Keycode.SDLK_F1);
        if (key >= SDL.SDL_Keycode.SDLK_F13 && key <= SDL.SDL_Keycode.SDLK_F24)
            return ConsoleKey.F13 + (key - SDL.SDL_Keycode.SDLK_F13);

        return key switch
        {
            SDL.SDL_Keycode.SDLK_RETURN => ConsoleKey.Enter,
            SDL.SDL_Keycode.SDLK_KP_ENTER => ConsoleKey.Enter,
            SDL.SDL_Keycode.SDLK_ESCAPE => ConsoleKey.Escape,
            SDL.SDL_Keycode.SDLK_TAB => ConsoleKey.Tab,
            SDL.SDL_Keycode.SDLK_BACKSPACE => ConsoleKey.Backspace,
            SDL.SDL_Keycode.SDLK_DELETE => ConsoleKey.Delete,
            SDL.SDL_Keycode.SDLK_INSERT => ConsoleKey.Insert,
            SDL.SDL_Keycode.SDLK_HOME => ConsoleKey.Home,
            SDL.SDL_Keycode.SDLK_END => ConsoleKey.End,
            SDL.SDL_Keycode.SDLK_PAGEUP => ConsoleKey.PageUp,
            SDL.SDL_Keycode.SDLK_PAGEDOWN => ConsoleKey.PageDown,
            SDL.SDL_Keycode.SDLK_LEFT => ConsoleKey.LeftArrow,
            SDL.SDL_Keycode.SDLK_RIGHT => ConsoleKey.RightArrow,
            SDL.SDL_Keycode.SDLK_UP => ConsoleKey.UpArrow,
            SDL.SDL_Keycode.SDLK_DOWN => ConsoleKey.DownArrow,
            SDL.SDL_Keycode.SDLK_SPACE => ConsoleKey.Spacebar,
            SDL.SDL_Keycode.SDLK_KP_0 => ConsoleKey.NumPad0,
            _ => null
        };
    }

    /// <summary>
    /// True for keys the host must handle on SDL_KEYDOWN because they never (reliably)
    /// produce an SDL_TEXTINPUT event: navigation, editing and function keys.
    /// </summary>
    public static bool IsControlKey(SDL.SDL_Keycode key)
    {
        if (key >= SDL.SDL_Keycode.SDLK_F1 && key <= SDL.SDL_Keycode.SDLK_F12) return true;
        if (key >= SDL.SDL_Keycode.SDLK_F13 && key <= SDL.SDL_Keycode.SDLK_F24) return true;
        return key switch
        {
            SDL.SDL_Keycode.SDLK_RETURN or SDL.SDL_Keycode.SDLK_KP_ENTER
                or SDL.SDL_Keycode.SDLK_ESCAPE or SDL.SDL_Keycode.SDLK_TAB
                or SDL.SDL_Keycode.SDLK_BACKSPACE or SDL.SDL_Keycode.SDLK_DELETE
                or SDL.SDL_Keycode.SDLK_INSERT or SDL.SDL_Keycode.SDLK_HOME
                or SDL.SDL_Keycode.SDLK_END or SDL.SDL_Keycode.SDLK_PAGEUP
                or SDL.SDL_Keycode.SDLK_PAGEDOWN or SDL.SDL_Keycode.SDLK_LEFT
                or SDL.SDL_Keycode.SDLK_RIGHT or SDL.SDL_Keycode.SDLK_UP
                or SDL.SDL_Keycode.SDLK_DOWN => true,
            _ => false
        };
    }

    public static ConsoleModifiers MapModifiers(SDL.SDL_Keymod modifiers)
    {
        ConsoleModifiers m = 0;
        if ((modifiers & SDL.SDL_Keymod.KMOD_CTRL) != 0) m |= ConsoleModifiers.Control;
        if ((modifiers & SDL.SDL_Keymod.KMOD_SHIFT) != 0) m |= ConsoleModifiers.Shift;
        if ((modifiers & SDL.SDL_Keymod.KMOD_ALT) != 0) m |= ConsoleModifiers.Alt;
        return m;
    }

    /// <summary>Maps a typed character (from SDL_TEXTINPUT) to the closest ConsoleKey.</summary>
    public static ConsoleKey MapChar(char c)
    {
        if (c >= 'a' && c <= 'z') return ConsoleKey.A + (c - 'a');
        if (c >= 'A' && c <= 'Z') return ConsoleKey.A + (c - 'A');
        if (c >= '0' && c <= '9') return ConsoleKey.D0 + (c - '0');
        if (c == ' ') return ConsoleKey.Spacebar;
        return ConsoleKey.Packet;
    }
}
