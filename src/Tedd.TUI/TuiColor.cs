using System;
using System.Runtime.CompilerServices;

namespace Tedd.TUI;

/// <summary>
/// 32-bit RGBA color used throughout the Tedd.TUI rendering pipeline. Internally
/// packed as <c>0xAARRGGBB</c> in a single <see cref="uint"/> so equality and hash
/// codes collapse to a single integer compare. Supplies an implicit conversion from
/// <see cref="ConsoleColor"/> so legacy code (and the 16-color renderer fallback)
/// keeps working unchanged.
/// </summary>
/// <remarks>
/// <para>The static palette properties (<see cref="Red"/>, <see cref="DarkBlue"/>, ...)
/// mirror the values previously encoded in <c>RgbColorPalette</c> and
/// <c>tuiInterop.colors</c>, so visual output stays identical when running against the
/// legacy 16-color console fallback.</para>
/// <para>Color composition uses the Porter-Duff "over" operator via <see cref="Blend"/>.
/// Renderers that target 16-color hosts use <see cref="ToNearestConsoleColor"/> to quantize.</para>
/// </remarks>
public readonly struct TuiColor : IEquatable<TuiColor>
{
    private readonly uint _packed;

    /// <summary>Packed ARGB value (0xAARRGGBB).</summary>
    public uint Packed
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _packed;
    }

    public byte A
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => (byte)((_packed >> 24) & 0xFF);
    }

    public byte R
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => (byte)((_packed >> 16) & 0xFF);
    }

    public byte G
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => (byte)((_packed >> 8) & 0xFF);
    }

    public byte B
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => (byte)(_packed & 0xFF);
    }

    /// <summary>True when the color is fully opaque (A == 255).</summary>
    public bool IsOpaque
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => (_packed & 0xFF000000u) == 0xFF000000u;
    }

    /// <summary>True when the color is fully transparent (A == 0).</summary>
    public bool IsTransparent
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => (_packed & 0xFF000000u) == 0u;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public TuiColor(byte r, byte g, byte b, byte a = 255)
    {
        _packed = ((uint)a << 24) | ((uint)r << 16) | ((uint)g << 8) | b;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private TuiColor(uint packed)
    {
        _packed = packed;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TuiColor FromRgb(byte r, byte g, byte b, byte a = 255) => new TuiColor(r, g, b, a);

    /// <summary>Builds a color from a packed 0xAARRGGBB unsigned int.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TuiColor FromArgb(uint argb) => new TuiColor(argb);

    /// <summary>
    /// Parses a CSS-style color string. Accepts <c>#RRGGBB</c>, <c>#RRGGBBAA</c>,
    /// <c>#RGB</c>, <c>rgb(r,g,b)</c>, <c>rgba(r,g,b,a)</c>, or any of the
    /// 16 <see cref="ConsoleColor"/> names (case-insensitive).
    /// </summary>
    public static TuiColor FromHex(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            throw new ArgumentException("Color string is empty", nameof(text));

        ReadOnlySpan<char> s = text.AsSpan().Trim();

        if (s[0] == '#')
        {
            var hex = s.Slice(1);
            switch (hex.Length)
            {
                case 3:
                    {
                        byte r = ParseHexNibble(hex[0]);
                        byte g = ParseHexNibble(hex[1]);
                        byte b = ParseHexNibble(hex[2]);
                        return new TuiColor((byte)(r | (r << 4)), (byte)(g | (g << 4)), (byte)(b | (b << 4)));
                    }
                case 6:
                    return new TuiColor(ParseHexByte(hex[0], hex[1]), ParseHexByte(hex[2], hex[3]), ParseHexByte(hex[4], hex[5]));
                case 8:
                    return new TuiColor(
                        ParseHexByte(hex[0], hex[1]),
                        ParseHexByte(hex[2], hex[3]),
                        ParseHexByte(hex[4], hex[5]),
                        ParseHexByte(hex[6], hex[7]));
                default:
                    throw new FormatException($"Invalid hex color '{text}'. Expected #RGB, #RRGGBB, or #RRGGBBAA.");
            }
        }

        if (s.Length > 4 && (s[0] == 'r' || s[0] == 'R'))
        {
            return ParseFunctional(text);
        }

        if (Enum.TryParse<ConsoleColor>(text, ignoreCase: true, out var cc))
            return FromConsole(cc);

        throw new FormatException($"Unrecognized color string '{text}'.");
    }

    private static TuiColor ParseFunctional(string text)
    {
        int open = text.IndexOf('(');
        int close = text.IndexOf(')');
        if (open < 0 || close < 0 || close <= open)
            throw new FormatException($"Malformed color '{text}'.");

        bool hasAlpha = text.AsSpan(0, open).Trim().Equals("rgba", StringComparison.OrdinalIgnoreCase);
        var inside = text.Substring(open + 1, close - open - 1);
        var parts = inside.Split(',');

        if (hasAlpha && parts.Length != 4)
            throw new FormatException($"rgba() requires 4 components: '{text}'.");
        if (!hasAlpha && parts.Length != 3)
            throw new FormatException($"rgb() requires 3 components: '{text}'.");

        byte r = ParseColorComponent(parts[0]);
        byte g = ParseColorComponent(parts[1]);
        byte b = ParseColorComponent(parts[2]);
        byte a = hasAlpha ? ParseAlphaComponent(parts[3]) : (byte)255;
        return new TuiColor(r, g, b, a);
    }

    private static byte ParseColorComponent(string component)
    {
        var trimmed = component.Trim();
        if (byte.TryParse(trimmed, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out var v))
            return v;
        throw new FormatException($"Invalid color component '{component}'.");
    }

    private static byte ParseAlphaComponent(string component)
    {
        var trimmed = component.Trim();
        if (trimmed.IndexOf('.') >= 0)
        {
            if (double.TryParse(trimmed, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var d))
                return (byte)Math.Clamp((int)Math.Round(d * 255.0), 0, 255);
            throw new FormatException($"Invalid alpha '{component}'.");
        }

        if (byte.TryParse(trimmed, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out var b))
            return b;
        throw new FormatException($"Invalid alpha '{component}'.");
    }

    private static byte ParseHexNibble(char c)
    {
        if (c >= '0' && c <= '9') return (byte)(c - '0');
        if (c >= 'a' && c <= 'f') return (byte)(c - 'a' + 10);
        if (c >= 'A' && c <= 'F') return (byte)(c - 'A' + 10);
        throw new FormatException($"Invalid hex digit '{c}'.");
    }

    private static byte ParseHexByte(char hi, char lo) => (byte)((ParseHexNibble(hi) << 4) | ParseHexNibble(lo));

    /// <summary>
    /// Returns the canonical RGB triple for a <see cref="ConsoleColor"/>. Values mirror
    /// the existing 16-color palette used by the Blazor surface and ASCII renderers.
    /// </summary>
    public static TuiColor FromConsole(ConsoleColor color)
    {
        int idx = (int)color;
        if ((uint)idx >= (uint)PaletteR.Length) return Transparent;
        return new TuiColor(PaletteR[idx], PaletteG[idx], PaletteB[idx]);
    }

    /// <summary>
    /// Implicit conversion from the legacy <see cref="ConsoleColor"/> enum so that
    /// existing code (and the rendered XAML <c>Foreground="Red"</c> shorthand) keeps
    /// working unchanged.
    /// </summary>
    public static implicit operator TuiColor(ConsoleColor color) => FromConsole(color);

    /// <summary>
    /// Quantizes this color to the nearest of the 16 <see cref="ConsoleColor"/> entries
    /// in squared Euclidean RGB distance. Used by the legacy 16-color renderer fallback.
    /// </summary>
    public ConsoleColor ToNearestConsoleColor()
    {
        byte r = R;
        byte g = G;
        byte b = B;
        int bestIndex = 0;
        int bestDist = int.MaxValue;
        for (int i = 0; i < PaletteR.Length; i++)
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
    /// Porter-Duff "source over destination" composition. The receiver is the source
    /// (top), <paramref name="under"/> is the destination (already on the surface).
    /// Returns a fully opaque color when both inputs are opaque.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public TuiColor Blend(TuiColor under)
    {
        uint srcA = (_packed >> 24) & 0xFF;
        if (srcA == 255) return this;
        if (srcA == 0) return under;

        uint dstA = (under._packed >> 24) & 0xFF;
        uint outA = srcA + ((dstA * (255 - srcA) + 127) / 255);
        if (outA == 0) return Transparent;

        uint srcR = (_packed >> 16) & 0xFF;
        uint srcG = (_packed >> 8) & 0xFF;
        uint srcB = _packed & 0xFF;
        uint dstR = (under._packed >> 16) & 0xFF;
        uint dstG = (under._packed >> 8) & 0xFF;
        uint dstB = under._packed & 0xFF;

        uint invSa = 255 - srcA;
        uint outR = (srcR * srcA + dstR * dstA * invSa / 255 + outA / 2) / outA;
        uint outG = (srcG * srcA + dstG * dstA * invSa / 255 + outA / 2) / outA;
        uint outB = (srcB * srcA + dstB * dstA * invSa / 255 + outA / 2) / outA;

        return new TuiColor((byte)outR, (byte)outG, (byte)outB, (byte)outA);
    }

    /// <summary>
    /// Returns this color with the alpha channel replaced by <paramref name="alpha"/>.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public TuiColor WithAlpha(byte alpha) => new TuiColor(R, G, B, alpha);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Equals(TuiColor other) => _packed == other._packed;
    public override bool Equals(object? obj) => obj is TuiColor c && Equals(c);
    public override int GetHashCode() => (int)_packed;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator ==(TuiColor a, TuiColor b) => a._packed == b._packed;
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator !=(TuiColor a, TuiColor b) => a._packed != b._packed;

    public override string ToString() => A == 255
        ? $"#{R:X2}{G:X2}{B:X2}"
        : $"#{R:X2}{G:X2}{B:X2}{A:X2}";

    // 16-color palette (mirrors RgbColorPalette + tuiInterop.colors).
    private static readonly byte[] PaletteR = new byte[16]
    {
        0x00, 0x00, 0x00, 0x00, 0x8B, 0x8B, 0xBD, 0xC0,
        0x80, 0x00, 0x00, 0x00, 0xFF, 0xFF, 0xFF, 0xFF
    };
    private static readonly byte[] PaletteG = new byte[16]
    {
        0x00, 0x00, 0x64, 0x8B, 0x00, 0x00, 0xB7, 0xC0,
        0x80, 0x00, 0xFF, 0xFF, 0x00, 0x00, 0xFF, 0xFF
    };
    private static readonly byte[] PaletteB = new byte[16]
    {
        0x00, 0x8B, 0x00, 0x8B, 0x00, 0x8B, 0x6B, 0xC0,
        0x80, 0xFF, 0x00, 0xFF, 0x00, 0xFF, 0x00, 0xFF
    };

    // Named 16-color palette accessors mirroring ConsoleColor for ergonomic parity.
    public static TuiColor Black { get; } = FromConsole(ConsoleColor.Black);
    public static TuiColor DarkBlue { get; } = FromConsole(ConsoleColor.DarkBlue);
    public static TuiColor DarkGreen { get; } = FromConsole(ConsoleColor.DarkGreen);
    public static TuiColor DarkCyan { get; } = FromConsole(ConsoleColor.DarkCyan);
    public static TuiColor DarkRed { get; } = FromConsole(ConsoleColor.DarkRed);
    public static TuiColor DarkMagenta { get; } = FromConsole(ConsoleColor.DarkMagenta);
    public static TuiColor DarkYellow { get; } = FromConsole(ConsoleColor.DarkYellow);
    public static TuiColor Gray { get; } = FromConsole(ConsoleColor.Gray);
    public static TuiColor DarkGray { get; } = FromConsole(ConsoleColor.DarkGray);
    public static TuiColor Blue { get; } = FromConsole(ConsoleColor.Blue);
    public static TuiColor Green { get; } = FromConsole(ConsoleColor.Green);
    public static TuiColor Cyan { get; } = FromConsole(ConsoleColor.Cyan);
    public static TuiColor Red { get; } = FromConsole(ConsoleColor.Red);
    public static TuiColor Magenta { get; } = FromConsole(ConsoleColor.Magenta);
    public static TuiColor Yellow { get; } = FromConsole(ConsoleColor.Yellow);
    public static TuiColor White { get; } = FromConsole(ConsoleColor.White);

    /// <summary>Fully transparent (alpha=0). Useful as a "no overlay" sentinel.</summary>
    public static TuiColor Transparent { get; } = new TuiColor(0u);
}
