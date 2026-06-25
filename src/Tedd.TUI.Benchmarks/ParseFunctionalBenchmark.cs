using BenchmarkDotNet.Attributes;
using System;
using System.Collections.Generic;

namespace Tedd.TUI.Benchmarks
{
    [MemoryDiagnoser]
    public class ParseFunctionalBenchmark
    {
        private string _rgbaText = "rgba(255, 128, 64, 0.5)";
        private string _rgbText = "rgb(10, 20, 30)";

        [Benchmark]
        public void ParseFunctional_Legacy_Rgba()
        {
            ParseFunctional_Legacy(_rgbaText);
        }

        [Benchmark]
        public void ParseFunctional_Optimized_Rgba()
        {
            ParseFunctional_Optimized(_rgbaText);
        }

        [Benchmark]
        public void ParseFunctional_Legacy_Rgb()
        {
            ParseFunctional_Legacy(_rgbText);
        }

        [Benchmark]
        public void ParseFunctional_Optimized_Rgb()
        {
            ParseFunctional_Optimized(_rgbText);
        }

        private static TuiColor ParseFunctional_Legacy(string text)
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
            return new TuiColor(r, g, b, a);
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

        private static TuiColor ParseFunctional_Optimized(string text)
        {
            ReadOnlySpan<char> span = text.AsSpan();
            int open = span.IndexOf('(');
            int close = span.IndexOf(')');
            if (open < 0 || close < 0 || close <= open)
                throw new FormatException($"Malformed color '{text}'.");

            bool hasAlpha = span.Slice(0, open).Trim().Equals("rgba", StringComparison.OrdinalIgnoreCase);
            ReadOnlySpan<char> inside = span.Slice(open + 1, close - open - 1);

            // Optimization: Replace Split with manual parsing and slicing
            Span<Range> ranges = stackalloc Range[4];
            int count = inside.Split(ranges, ',');

            if (hasAlpha && count != 4)
                throw new FormatException($"rgba() requires 4 components: '{text}'.");
            if (!hasAlpha && count != 3)
                throw new FormatException($"rgb() requires 3 components: '{text}'.");

            byte r = ParseColorComponent_Optimized(inside[ranges[0]]);
            byte g = ParseColorComponent_Optimized(inside[ranges[1]]);
            byte b = ParseColorComponent_Optimized(inside[ranges[2]]);
            byte a = hasAlpha ? ParseAlphaComponent_Optimized(inside[ranges[3]]) : (byte)255;
            return new TuiColor(r, g, b, a);
        }

        private static byte ParseColorComponent_Optimized(ReadOnlySpan<char> component)
        {
            ReadOnlySpan<char> trimmed = component.Trim();
            if (byte.TryParse(trimmed, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out var v))
                return v;
            throw new FormatException($"Invalid color component '{component.ToString()}'.");
        }

        private static byte ParseAlphaComponent_Optimized(ReadOnlySpan<char> component)
        {
            ReadOnlySpan<char> trimmed = component.Trim();
            if (trimmed.IndexOf('.') >= 0)
            {
                if (double.TryParse(trimmed, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var d))
                    return (byte)Math.Clamp((int)Math.Round(d * 255.0), 0, 255);
                throw new FormatException($"Invalid alpha '{component.ToString()}'.");
            }

            if (byte.TryParse(trimmed, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out var b))
                return b;
            throw new FormatException($"Invalid alpha '{component.ToString()}'.");
        }
    }
}
