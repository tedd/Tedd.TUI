using System;
using ImageMagick;
using Tedd.TUI.Markdown;

namespace Tedd.TUI.Imaging;

/// <summary>
/// <see cref="IImageDecoder"/> backed by Magick.NET (ImageMagick). Supports the wide range
/// of formats ImageMagick understands out of the box (PNG, JPEG, BMP, GIF, TIFF, WebP, TGA,
/// HEIC, etc.). For animated formats only the first frame is decoded.
/// </summary>
public sealed class MagickNetDecoder : IImageDecoder
{
    public bool TryDecode(byte[] bytes, out RgbaImage image)
    {
        if (bytes == null || bytes.Length == 0)
        {
            image = default;
            return false;
        }

        try
        {
            // MagickImage (vs MagickImageCollection) loads only the first frame of animated
            // formats, matching the previous ImageSharp behavior.
            using var img = new MagickImage(bytes);

            // Ensure we have an alpha channel so the RGBA pixel mapping always returns
            // four bytes per pixel even for opaque source images.
            if (!img.HasAlpha)
            {
                img.Alpha(AlphaOption.Opaque);
            }

            int width = checked((int)img.Width);
            int height = checked((int)img.Height);

            using var pixels = img.GetPixelsUnsafe();
            var raw = pixels.ToByteArray(PixelMapping.RGBA);
            if (raw == null || raw.Length != width * height * 4)
            {
                image = default;
                return false;
            }

            image = new RgbaImage
            {
                Width = width,
                Height = height,
                Pixels = raw
            };
            return true;
        }
        catch
        {
            image = default;
            return false;
        }
    }
}
