using SDL2;
using Tedd.TUI;

namespace Tedd.TUI.Platform.Sdl2;

/// <summary>
/// <see cref="IClipboard"/> bridge to the SDL2 clipboard
/// (<c>SDL_GetClipboardText</c> / <c>SDL_SetClipboardText</c>), registered by
/// <see cref="TuiSdl2Host"/> so TUI text controls copy/paste through the desktop
/// clipboard. Requires SDL video to be initialized, which the host guarantees before
/// registering the provider.
/// </summary>
public sealed class Sdl2Clipboard : IClipboard
{
    public string? GetText()
    {
        // Empty string (no clipboard text) is authoritative and stops the service's
        // fallback buffer from pasting stale text.
        if (SDL.SDL_HasClipboardText() == SDL.SDL_bool.SDL_FALSE)
            return string.Empty;
        return SDL.SDL_GetClipboardText() ?? string.Empty;
    }

    public void SetText(string text) => SDL.SDL_SetClipboardText(text ?? string.Empty);
}
