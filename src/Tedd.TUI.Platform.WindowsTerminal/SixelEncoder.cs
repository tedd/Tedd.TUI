using System;
using Tedd.TUI;

namespace Tedd.TUI.Platform.WindowsTerminal;

/// <summary>
/// Encodes a <see cref="GraphicPlacement"/> into a DEC Sixel escape sequence
/// (<c>ESC P ... q ... ESC \</c>) suitable for Windows Terminal 1.22+ and any other
/// host that advertises Sixel support. Implementation is shared with the Linux backend
/// via the <see cref="SixelEncoderCore"/> helper in <c>Tedd.TUI</c>.
/// </summary>
public sealed class SixelEncoder : IImageProtocolEncoder
{
    public string Protocol => "sixel";

    public string Encode(GraphicPlacement placement)
    {
        return SixelEncoderCore.Encode(placement);
    }
}
