using System;
using Tedd.TUI.Markdown;

namespace Tedd.TUI.Platform.Blazor;

/// <summary>
/// Lightweight image header decoder for WebAssembly / Blazor environments.
/// Extracts pixel dimensions from image headers (PNG, JPEG, GIF, BMP, WebP)
/// without native image processing dependencies so Blazor can lay out images for HTML/Canvas rendering.
/// </summary>
public class WebImageDecoder : IImageDecoder
{
    public bool TryDecode(byte[] bytes, out RgbaImage image)
    {
        if (bytes == null || bytes.Length == 0)
        {
            image = default;
            return false;
        }

        if (TryGetDimensions(bytes, out int width, out int height))
        {
            image = new RgbaImage { Width = width, Height = height, Pixels = Array.Empty<byte>() };
            return true;
        }

        image = new RgbaImage { Width = 400, Height = 300, Pixels = Array.Empty<byte>() };
        return true;
    }

    public static bool TryGetDimensions(byte[] bytes, out int width, out int height)
    {
        width = 0;
        height = 0;
        try
        {
            // PNG: 8-byte header, IHDR chunk width/height at offset 16/20
            if (bytes.Length >= 24 &&
                bytes[0] == 0x89 && bytes[1] == 0x50 && bytes[2] == 0x4E && bytes[3] == 0x47)
            {
                width = (bytes[16] << 24) | (bytes[17] << 16) | (bytes[18] << 8) | bytes[19];
                height = (bytes[20] << 24) | (bytes[21] << 16) | (bytes[22] << 8) | bytes[23];
                return width > 0 && height > 0;
            }

            // GIF: "GIF87a" or "GIF89a"
            if (bytes.Length >= 10 &&
                bytes[0] == (byte)'G' && bytes[1] == (byte)'I' && bytes[2] == (byte)'F')
            {
                width = bytes[6] | (bytes[7] << 8);
                height = bytes[8] | (bytes[9] << 8);
                return width > 0 && height > 0;
            }

            // BMP: "BM"
            if (bytes.Length >= 26 &&
                bytes[0] == (byte)'B' && bytes[1] == (byte)'M')
            {
                width = bytes[18] | (bytes[19] << 8) | (bytes[20] << 16) | (bytes[21] << 24);
                height = Math.Abs(bytes[22] | (bytes[23] << 8) | (bytes[24] << 16) | (bytes[25] << 24));
                return width > 0 && height > 0;
            }

            // JPEG: 0xFF 0xD8
            if (bytes.Length >= 4 && bytes[0] == 0xFF && bytes[1] == 0xD8)
            {
                int i = 2;
                while (i + 8 < bytes.Length)
                {
                    if (bytes[i] != 0xFF) { i++; continue; }
                    byte marker = bytes[i + 1];
                    if (marker == 0xC0 || marker == 0xC1 || marker == 0xC2 || marker == 0xC3)
                    {
                        height = (bytes[i + 5] << 8) | bytes[i + 6];
                        width = (bytes[i + 7] << 8) | bytes[i + 8];
                        return width > 0 && height > 0;
                    }
                    int blockLength = (bytes[i + 2] << 8) | bytes[i + 3];
                    i += 2 + blockLength;
                }
            }
        }
        catch
        {
            // Ignore format parsing exceptions and fallback to default dimensions
        }

        return false;
    }
}
