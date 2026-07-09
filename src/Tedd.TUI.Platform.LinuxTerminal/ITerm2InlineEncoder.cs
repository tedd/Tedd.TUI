using System;
using System.Text;
using Tedd.TUI;

namespace Tedd.TUI.Platform.LinuxTerminal;

/// <summary>
/// Encoder for the iTerm2 inline image protocol
/// (<c>ESC ] 1337 ; File = inline=1 ; width=Wpx ; height=Hpx : &lt;base64&gt; BEL</c>).
/// Supported by iTerm2 (macOS) and a handful of compatible terminals (WezTerm, Ghostty).
/// </summary>
public sealed class ITerm2InlineEncoder : IImageProtocolEncoder
{
    public string Protocol => "iterm2";

    public string Encode(GraphicPlacement placement)
    {
        if (placement.ImageData == null || placement.ImageData.Length == 0)
            return string.Empty;

        var sb = new StringBuilder(placement.ImageData.Length * 4 / 3 + 128);
        sb.Append("\e]1337;File=inline=1");

        if (placement.CharWidth > 0)
        {
            // iTerm2 accepts "auto", pixel sizes ("Npx"), percentages, or character cells.
            // We use character cells because the placement already speaks that language.
            sb.Append(";width=").Append(placement.CharWidth);
        }
        if (placement.CharHeight > 0)
        {
            sb.Append(";height=").Append(placement.CharHeight);
        }
        if (!string.IsNullOrEmpty(placement.MediaType))
        {
            sb.Append(";type=").Append(placement.MediaType);
        }

        sb.Append(':');
        sb.Append(Convert.ToBase64String(placement.ImageData));
        sb.Append('\x07'); // BEL terminator (iTerm2 also accepts ESC \).
        return sb.ToString();
    }
}
