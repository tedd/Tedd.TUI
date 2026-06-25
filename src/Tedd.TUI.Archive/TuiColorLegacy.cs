using System;

namespace Tedd.TUI.Archive
{
    public class TuiColorLegacy
    {
        public static TuiColorLegacy ParseFunctional_Legacy(string text)
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

            byte r = ParseColorComponent_Legacy(parts[0]);
            byte g = ParseColorComponent_Legacy(parts[1]);
            byte b = ParseColorComponent_Legacy(parts[2]);
            byte a = hasAlpha ? ParseAlphaComponent_Legacy(parts[3]) : (byte)255;
            return new TuiColorLegacy(); // returning dummy value for archiving
        }

        private static byte ParseColorComponent_Legacy(string component)
        {
            var trimmed = component.Trim();
            if (byte.TryParse(trimmed, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out var v))
                return v;
            throw new FormatException($"Invalid color component '{component}'.");
        }

        private static byte ParseAlphaComponent_Legacy(string component)
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
    }
}
