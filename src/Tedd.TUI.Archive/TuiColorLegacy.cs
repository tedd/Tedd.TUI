using System;
using System.Runtime.CompilerServices;

namespace Tedd.TUI.Archive
{
    public readonly struct TuiColorLegacy : IEquatable<TuiColorLegacy>
    {
        private readonly uint _packed;

        public uint Packed => _packed;
        public byte A => (byte)((_packed >> 24) & 0xFF);
        public byte R => (byte)((_packed >> 16) & 0xFF);
        public byte G => (byte)((_packed >> 8) & 0xFF);
        public byte B => (byte)(_packed & 0xFF);

        public TuiColorLegacy(byte r, byte g, byte b, byte a = 255)
        {
            _packed = ((uint)a << 24) | ((uint)r << 16) | ((uint)g << 8) | b;
        }

        public static TuiColorLegacy FromHex(string text)
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
                            return new TuiColorLegacy((byte)(r | (r << 4)), (byte)(g | (g << 4)), (byte)(b | (b << 4)));
                        }
                    case 6:
                        return new TuiColorLegacy(ParseHexByte(hex[0], hex[1]), ParseHexByte(hex[2], hex[3]), ParseHexByte(hex[4], hex[5]));
                    case 8:
                        return new TuiColorLegacy(
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

            return new TuiColorLegacy(0, 0, 0, 255);
        }

        private static TuiColorLegacy ParseFunctional(string text)
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
            return new TuiColorLegacy(r, g, b, a);
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

        public bool Equals(TuiColorLegacy other) => _packed == other._packed;
        public override bool Equals(object? obj) => obj is TuiColorLegacy c && Equals(c);
        public override int GetHashCode() => (int)_packed;
    }
}
