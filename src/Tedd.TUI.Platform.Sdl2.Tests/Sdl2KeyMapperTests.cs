using SDL2;
using Tedd.TUI.Platform.Sdl2;

namespace Tedd.TUI.Platform.Sdl2.Tests;

public class Sdl2KeyMapperTests
{
    [Theory]
    [InlineData(SDL.SDL_Keycode.SDLK_a, ConsoleKey.A)]
    [InlineData(SDL.SDL_Keycode.SDLK_m, ConsoleKey.M)]
    [InlineData(SDL.SDL_Keycode.SDLK_z, ConsoleKey.Z)]
    [InlineData(SDL.SDL_Keycode.SDLK_0, ConsoleKey.D0)]
    [InlineData(SDL.SDL_Keycode.SDLK_9, ConsoleKey.D9)]
    [InlineData(SDL.SDL_Keycode.SDLK_F1, ConsoleKey.F1)]
    [InlineData(SDL.SDL_Keycode.SDLK_F12, ConsoleKey.F12)]
    [InlineData(SDL.SDL_Keycode.SDLK_F13, ConsoleKey.F13)]
    [InlineData(SDL.SDL_Keycode.SDLK_F24, ConsoleKey.F24)]
    [InlineData(SDL.SDL_Keycode.SDLK_RETURN, ConsoleKey.Enter)]
    [InlineData(SDL.SDL_Keycode.SDLK_KP_ENTER, ConsoleKey.Enter)]
    [InlineData(SDL.SDL_Keycode.SDLK_ESCAPE, ConsoleKey.Escape)]
    [InlineData(SDL.SDL_Keycode.SDLK_TAB, ConsoleKey.Tab)]
    [InlineData(SDL.SDL_Keycode.SDLK_BACKSPACE, ConsoleKey.Backspace)]
    [InlineData(SDL.SDL_Keycode.SDLK_DELETE, ConsoleKey.Delete)]
    [InlineData(SDL.SDL_Keycode.SDLK_INSERT, ConsoleKey.Insert)]
    [InlineData(SDL.SDL_Keycode.SDLK_HOME, ConsoleKey.Home)]
    [InlineData(SDL.SDL_Keycode.SDLK_END, ConsoleKey.End)]
    [InlineData(SDL.SDL_Keycode.SDLK_PAGEUP, ConsoleKey.PageUp)]
    [InlineData(SDL.SDL_Keycode.SDLK_PAGEDOWN, ConsoleKey.PageDown)]
    [InlineData(SDL.SDL_Keycode.SDLK_LEFT, ConsoleKey.LeftArrow)]
    [InlineData(SDL.SDL_Keycode.SDLK_RIGHT, ConsoleKey.RightArrow)]
    [InlineData(SDL.SDL_Keycode.SDLK_UP, ConsoleKey.UpArrow)]
    [InlineData(SDL.SDL_Keycode.SDLK_DOWN, ConsoleKey.DownArrow)]
    [InlineData(SDL.SDL_Keycode.SDLK_SPACE, ConsoleKey.Spacebar)]
    [InlineData(SDL.SDL_Keycode.SDLK_KP_0, ConsoleKey.NumPad0)]
    [InlineData(SDL.SDL_Keycode.SDLK_KP_1, ConsoleKey.NumPad1)]
    [InlineData(SDL.SDL_Keycode.SDLK_KP_9, ConsoleKey.NumPad9)]
    public void Map_TranslatesKnownKeys(SDL.SDL_Keycode keycode, ConsoleKey expected)
    {
        Assert.Equal(expected, Sdl2KeyMapper.Map(keycode));
    }

    [Theory]
    [InlineData(SDL.SDL_Keycode.SDLK_SEMICOLON)]
    [InlineData(SDL.SDL_Keycode.SDLK_LSHIFT)]
    [InlineData(SDL.SDL_Keycode.SDLK_LGUI)]
    [InlineData(SDL.SDL_Keycode.SDLK_CAPSLOCK)]
    public void Map_ReturnsNullForUnmappedKeys(SDL.SDL_Keycode keycode)
    {
        Assert.Null(Sdl2KeyMapper.Map(keycode));
    }

    [Theory]
    [InlineData(SDL.SDL_Keycode.SDLK_RETURN)]
    [InlineData(SDL.SDL_Keycode.SDLK_KP_ENTER)]
    [InlineData(SDL.SDL_Keycode.SDLK_ESCAPE)]
    [InlineData(SDL.SDL_Keycode.SDLK_TAB)]
    [InlineData(SDL.SDL_Keycode.SDLK_BACKSPACE)]
    [InlineData(SDL.SDL_Keycode.SDLK_DELETE)]
    [InlineData(SDL.SDL_Keycode.SDLK_UP)]
    [InlineData(SDL.SDL_Keycode.SDLK_F1)]
    [InlineData(SDL.SDL_Keycode.SDLK_F24)]
    public void IsControlKey_TrueForNavigationEditingAndFunctionKeys(SDL.SDL_Keycode keycode)
    {
        Assert.True(Sdl2KeyMapper.IsControlKey(keycode));
    }

    [Theory]
    [InlineData(SDL.SDL_Keycode.SDLK_a)]
    [InlineData(SDL.SDL_Keycode.SDLK_5)]
    [InlineData(SDL.SDL_Keycode.SDLK_SPACE)]
    [InlineData(SDL.SDL_Keycode.SDLK_SEMICOLON)]
    public void IsControlKey_FalseForPrintableKeys(SDL.SDL_Keycode keycode)
    {
        Assert.False(Sdl2KeyMapper.IsControlKey(keycode));
    }

    [Fact]
    public void MapModifiers_TranslatesEachModifier()
    {
        Assert.Equal(ConsoleModifiers.Control, Sdl2KeyMapper.MapModifiers(SDL.SDL_Keymod.KMOD_LCTRL));
        Assert.Equal(ConsoleModifiers.Control, Sdl2KeyMapper.MapModifiers(SDL.SDL_Keymod.KMOD_RCTRL));
        Assert.Equal(ConsoleModifiers.Shift, Sdl2KeyMapper.MapModifiers(SDL.SDL_Keymod.KMOD_LSHIFT));
        Assert.Equal(ConsoleModifiers.Alt, Sdl2KeyMapper.MapModifiers(SDL.SDL_Keymod.KMOD_RALT));
        Assert.Equal((ConsoleModifiers)0, Sdl2KeyMapper.MapModifiers(SDL.SDL_Keymod.KMOD_NONE));
        Assert.Equal(
            ConsoleModifiers.Control | ConsoleModifiers.Shift,
            Sdl2KeyMapper.MapModifiers(SDL.SDL_Keymod.KMOD_LCTRL | SDL.SDL_Keymod.KMOD_RSHIFT));
    }

    [Theory]
    [InlineData('a', ConsoleKey.A)]
    [InlineData('Z', ConsoleKey.Z)]
    [InlineData('5', ConsoleKey.D5)]
    [InlineData(' ', ConsoleKey.Spacebar)]
    [InlineData('ø', ConsoleKey.Packet)]
    [InlineData('!', ConsoleKey.Packet)]
    public void MapChar_TranslatesTypedCharacters(char c, ConsoleKey expected)
    {
        Assert.Equal(expected, Sdl2KeyMapper.MapChar(c));
    }
}
