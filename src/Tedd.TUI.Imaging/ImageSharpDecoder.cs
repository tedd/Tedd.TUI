using System;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Tedd.TUI.Markdown;

namespace Tedd.TUI.Imaging;

/// <summary>
/// <see cref="IImageDecoder"/> backed by SixLabors.ImageSharp. Supports PNG, JPEG, BMP,
/// GIF, TGA, and WebP out of the box. For animated formats only the first frame is decoded.
/// </summary>
public sealed class ImageSharpDecoder : IImageDecoder
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
            using var img = SixLabors.ImageSharp.Image.Load<Rgba32>(bytes);
            int width = img.Width;
            int height = img.Height;
            var pixels = new byte[width * height * 4];

            img.CopyPixelDataTo(pixels);

            image = new RgbaImage
            {
                Width = width,
                Height = height,
                Pixels = pixels
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
