using System;

namespace Tedd.TUI.Markdown;

/// <summary>
/// Maps 24-bit RGB pixel values to the closest of the 16 <see cref="ConsoleColor"/>
/// entries. The reference palette matches the colors used by the Blazor renderers
/// (see <c>tuiInterop.colors</c> and <c>TuiDomGrid.ToHtmlColor</c>) so the same
/// image looks consistent across console and web surfaces.
/// </summary>
public static class RgbColorPalette
{
    // Index in this array is the ConsoleColor enum value (0..15).
    // Values mirror the Blazor side palette so the on-screen color stays consistent.
    private static readonly byte[] PaletteR;
    private static readonly byte[] PaletteG;
    private static readonly byte[] PaletteB;

    static RgbColorPalette()
    {
        PaletteR = new byte[16];
        PaletteG = new byte[16];
        PaletteB = new byte[16];

        // Mirrors tuiInterop.colors / TuiDomGrid.ToHtmlColor.
        Set(ConsoleColor.Black, 0x00, 0x00, 0x00);
        Set(ConsoleColor.DarkBlue, 0x00, 0x00, 0x8B);
        Set(ConsoleColor.DarkGreen, 0x00, 0x64, 0x00);
        Set(ConsoleColor.DarkCyan, 0x00, 0x8B, 0x8B);
        Set(ConsoleColor.DarkRed, 0x8B, 0x00, 0x00);
        Set(ConsoleColor.DarkMagenta, 0x8B, 0x00, 0x8B);
        Set(ConsoleColor.DarkYellow, 0xBD, 0xB7, 0x6B);
        Set(ConsoleColor.Gray, 0xC0, 0xC0, 0xC0);
        Set(ConsoleColor.DarkGray, 0x80, 0x80, 0x80);
        Set(ConsoleColor.Blue, 0x00, 0x00, 0xFF);
        Set(ConsoleColor.Green, 0x00, 0xFF, 0x00);
        Set(ConsoleColor.Cyan, 0x00, 0xFF, 0xFF);
        Set(ConsoleColor.Red, 0xFF, 0x00, 0x00);
        Set(ConsoleColor.Magenta, 0xFF, 0x00, 0xFF);
        Set(ConsoleColor.Yellow, 0xFF, 0xFF, 0x00);
        Set(ConsoleColor.White, 0xFF, 0xFF, 0xFF);
    }

    private static void Set(ConsoleColor color, byte r, byte g, byte b)
    {
        int i = (int)color;
        PaletteR[i] = r;
        PaletteG[i] = g;
        PaletteB[i] = b;
    }

    /// <summary>
    /// Returns the <see cref="ConsoleColor"/> whose palette entry has the smallest squared
    /// Euclidean distance in RGB space to the supplied color.
    /// </summary>
    public static ConsoleColor Nearest(byte r, byte g, byte b)
    {
        int bestIndex = 0;
        int bestDist = int.MaxValue;
        for (int i = 0; i < 16; i++)
        {
            int dr = r - PaletteR[i];
            int dg = g - PaletteG[i];
            int db = b - PaletteB[i];
            int dist = dr * dr + dg * dg + db * db;
            if (dist < bestDist)
            {
                bestDist = dist;
                bestIndex = i;
                if (dist == 0) break;
            }
        }
        return (ConsoleColor)bestIndex;
    }

    /// <summary>
    /// Returns the 24-bit RGB triple associated with the given <see cref="ConsoleColor"/>.
    /// </summary>
    public static (byte R, byte G, byte B) ToRgb(ConsoleColor color)
    {
        int i = (int)color;
        if ((uint)i >= 16) return (0, 0, 0);
        return (PaletteR[i], PaletteG[i], PaletteB[i]);
    }
}
