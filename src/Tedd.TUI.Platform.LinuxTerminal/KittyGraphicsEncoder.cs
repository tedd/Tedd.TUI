using System;
using System.Text;
using Tedd.TUI;

namespace Tedd.TUI.Platform.LinuxTerminal;

/// <summary>
/// Encoder for the Kitty graphics protocol. Emits
/// <c>ESC _ G a=T,f=100,...; &lt;base64 PNG&gt; ESC \</c> envelopes that the Kitty terminal
/// (and other compatible hosts) renders inline at the cursor position. Falls back to an
/// empty string when no image bytes are attached so callers can pipe through unchanged.
/// </summary>
public sealed class KittyGraphicsEncoder : IImageProtocolEncoder
{
    public string Protocol => "kitty";

    public string Encode(GraphicPlacement placement)
    {
        if (placement.ImageData == null || placement.ImageData.Length == 0)
            return string.Empty;

        var base64 = Convert.ToBase64String(placement.ImageData);

        // Chunked transmission: Kitty caps each escape to ~4096 bytes; we split the payload
        // so very large images still get through. The first chunk carries action=T (transmit
        // + display), subsequent chunks set m=1 / m=0 to continue / end the transmission.
        const int chunkSize = 4096;
        var sb = new StringBuilder(base64.Length + 128);

        for (int offset = 0; offset < base64.Length; offset += chunkSize)
        {
            int len = Math.Min(chunkSize, base64.Length - offset);
            bool first = offset == 0;
            bool last = offset + len >= base64.Length;

            sb.Append("\e_G");
            if (first)
            {
                sb.Append("a=T,f=100,");
                if (placement.CharWidth > 0) sb.Append("c=").Append(placement.CharWidth).Append(',');
                if (placement.CharHeight > 0) sb.Append("r=").Append(placement.CharHeight).Append(',');
            }
            sb.Append("m=").Append(last ? '0' : '1');
            sb.Append(';');
            sb.Append(base64, offset, len);
            sb.Append("\e\\");
        }

        return sb.ToString();
    }
}
