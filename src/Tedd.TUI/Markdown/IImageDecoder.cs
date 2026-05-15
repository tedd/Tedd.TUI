namespace Tedd.TUI.Markdown;

/// <summary>
/// Decodes raw image bytes (PNG, JPEG, etc.) into an <see cref="RgbaImage"/>.
/// Implementations live outside the core Tedd.TUI assembly (e.g. Tedd.TUI.Imaging)
/// so the core stays free of binary-image dependencies. Set
/// <see cref="Image.DefaultDecoder"/> at application startup to enable image
/// rendering globally.
/// </summary>
public interface IImageDecoder
{
    /// <summary>
    /// Attempts to decode the given image bytes. Returns true on success.
    /// </summary>
    bool TryDecode(byte[] bytes, out RgbaImage image);
}
