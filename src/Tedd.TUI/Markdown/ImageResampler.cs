using System;

namespace Tedd.TUI.Markdown;

/// <summary>
/// Small helper for resampling <see cref="RgbaImage"/> data into a packed RGBA8 buffer
/// at an arbitrary target size. Used by built-in <see cref="IAsciiArtRenderer"/>
/// implementations and exposed publicly so third-party renderers can share the same
/// resampling kernel.
/// </summary>
public static class ImageResampler
{
    /// <summary>
    /// Bilinear-resamples <paramref name="src"/> to a packed RGBA8 buffer of size
    /// <paramref name="targetWidth"/> * <paramref name="targetHeight"/> * 4 bytes
    /// (row-major, top-down).
    /// </summary>
    /// <remarks>
    /// Returns an all-zero buffer when the source image is empty. When either target
    /// dimension is 1, samples from the centre of the source for that axis. The kernel
    /// favours simplicity over filtering quality; for very large downscales (e.g. a
    /// 4000-pixel image to 20 cells) consider box-averaging instead.
    /// </remarks>
    public static byte[] Bilinear(RgbaImage src, int targetWidth, int targetHeight)
    {
        if (targetWidth <= 0 || targetHeight <= 0)
            return Array.Empty<byte>();

        var dst = new byte[targetWidth * targetHeight * 4];
        if (src.Pixels == null || src.Width <= 0 || src.Height <= 0)
            return dst;

        int sw = src.Width;
        int sh = src.Height;
        byte[] sp = src.Pixels;

        // Scale factors: when targetSize >= 2 we span (sw-1) across (targetSize-1) steps so
        // both endpoints are sampled. For targetSize == 1 we sample at the source centre.
        float sx = targetWidth > 1 ? (sw - 1f) / (targetWidth - 1f) : 0f;
        float sy = targetHeight > 1 ? (sh - 1f) / (targetHeight - 1f) : 0f;
        float cx = (sw - 1f) * 0.5f;
        float cy = (sh - 1f) * 0.5f;

        for (int y = 0; y < targetHeight; y++)
        {
            float fy = targetHeight > 1 ? y * sy : cy;
            int y0 = (int)fy;
            int y1 = y0 + 1; if (y1 >= sh) y1 = sh - 1;
            float wy = fy - y0;

            for (int x = 0; x < targetWidth; x++)
            {
                float fx = targetWidth > 1 ? x * sx : cx;
                int x0 = (int)fx;
                int x1 = x0 + 1; if (x1 >= sw) x1 = sw - 1;
                float wx = fx - x0;

                int i00 = (y0 * sw + x0) * 4;
                int i10 = (y0 * sw + x1) * 4;
                int i01 = (y1 * sw + x0) * 4;
                int i11 = (y1 * sw + x1) * 4;

                int dstIdx = (y * targetWidth + x) * 4;
                for (int c = 0; c < 4; c++)
                {
                    float top = sp[i00 + c] * (1 - wx) + sp[i10 + c] * wx;
                    float bot = sp[i01 + c] * (1 - wx) + sp[i11 + c] * wx;
                    float val = top * (1 - wy) + bot * wy;
                    int iv = (int)(val + 0.5f);
                    if (iv < 0) iv = 0; else if (iv > 255) iv = 255;
                    dst[dstIdx + c] = (byte)iv;
                }
            }
        }
        return dst;
    }
}
