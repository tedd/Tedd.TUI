using System;
using System.IO;
using Tedd.TUI;
using Tedd.TUI.Markdown;
using Xunit;

namespace Tedd.TUI.Tests
{
    public class LuminanceRampAsciiRendererTests
    {
        [Fact]
        public void Defaults_AreCorrect()
        {
            var renderer = new LuminanceRampAsciiRenderer();
            Assert.Equal(LuminanceRampAsciiRenderer.ColorRamp, renderer.Ramp);
            Assert.False(renderer.Inverted);
            Assert.True(renderer.UseColor);
            Assert.Equal(TuiColor.Gray, renderer.Foreground);
            Assert.Equal(16, renderer.AlphaThreshold);
        }

        [Fact]
        public void Constructor_InvalidRamp_Throws()
        {
            Assert.Throws<ArgumentException>(() => new LuminanceRampAsciiRenderer("", false, true));
            Assert.Throws<ArgumentException>(() => new LuminanceRampAsciiRenderer(null!, false, true));
        }

        [Fact]
        public void Constructor_Valid_SetsProperties()
        {
            var renderer = new LuminanceRampAsciiRenderer("abc", true, false);
            Assert.Equal("abc", renderer.Ramp);
            Assert.True(renderer.Inverted);
            Assert.False(renderer.UseColor);
        }

        [Fact]
        public void Render_ZeroSize_ReturnsEmpty()
        {
            var renderer = new LuminanceRampAsciiRenderer();
            var image = new RgbaImage { Pixels = new byte[4], Width = 1, Height = 1 };
            Assert.Empty(renderer.Render(image, 0, 1, TuiColor.Black));
            Assert.Empty(renderer.Render(image, 1, 0, TuiColor.Black));
        }

        [Fact]
        public void Render_EmptyImage_ReturnsFallbackCells()
        {
            var renderer = new LuminanceRampAsciiRenderer { Foreground = TuiColor.Red };
            var image = new RgbaImage { Pixels = null!, Width = 0, Height = 0 };
            var cells = renderer.Render(image, 2, 2, TuiColor.Blue);

            Assert.Equal(4, cells.Length);
            foreach (var cell in cells)
            {
                Assert.Equal(' ', cell.Character);
                Assert.Equal(TuiColor.Red, cell.Foreground);
                Assert.Equal(TuiColor.Blue, cell.Background);
            }
        }

        [Fact]
        public void Render_TransparentPixels_UsesFallback()
        {
            var renderer = new LuminanceRampAsciiRenderer { Foreground = TuiColor.Red, AlphaThreshold = 100 };
            var pixels = new byte[] { 255, 255, 255, 50 }; // Alpha < threshold
            var image = new RgbaImage { Pixels = pixels, Width = 1, Height = 1 };
            var cells = renderer.Render(image, 1, 1, TuiColor.Blue);

            Assert.Single(cells);
            Assert.Equal(' ', cells[0].Character);
        }

        [Fact]
        public void Render_BlackPixel_UsesDarkestGlyph()
        {
            var renderer = new LuminanceRampAsciiRenderer(" .-", false, true);
            var pixels = new byte[] { 0, 0, 0, 255 };
            var image = new RgbaImage { Pixels = pixels, Width = 1, Height = 1 };
            var cells = renderer.Render(image, 1, 1, TuiColor.Black);

            Assert.Single(cells);
            Assert.Equal(' ', cells[0].Character);
            Assert.Equal(new TuiColor(0, 0, 0), cells[0].Foreground);
        }

        [Fact]
        public void Render_WhitePixel_UsesBrightestGlyph()
        {
            var renderer = new LuminanceRampAsciiRenderer(" .-", false, true);
            var pixels = new byte[] { 255, 255, 255, 255 };
            var image = new RgbaImage { Pixels = pixels, Width = 1, Height = 1 };
            var cells = renderer.Render(image, 1, 1, TuiColor.Black);

            Assert.Single(cells);
            Assert.Equal('-', cells[0].Character);
            Assert.Equal(new TuiColor(255, 255, 255), cells[0].Foreground);
        }

        [Fact]
        public void Render_Inverted_UsesDarkestForWhite()
        {
            var renderer = new LuminanceRampAsciiRenderer(" .-", true, true);
            var pixels = new byte[] { 255, 255, 255, 255 };
            var image = new RgbaImage { Pixels = pixels, Width = 1, Height = 1 };
            var cells = renderer.Render(image, 1, 1, TuiColor.Black);

            Assert.Single(cells);
            Assert.Equal(' ', cells[0].Character);
        }

        [Fact]
        public void Render_NoColor_UsesForeground()
        {
            var renderer = new LuminanceRampAsciiRenderer(" .-", false, false) { Foreground = TuiColor.Red };
            var pixels = new byte[] { 255, 255, 255, 255 };
            var image = new RgbaImage { Pixels = pixels, Width = 1, Height = 1 };
            var cells = renderer.Render(image, 1, 1, TuiColor.Black);

            Assert.Single(cells);
            Assert.Equal(TuiColor.Red, cells[0].Foreground);
        }

        [Fact]
        public void Render_NullRamp_FallsBackToColorRamp()
        {
            // By bypassing validation
            var renderer = new LuminanceRampAsciiRenderer { Ramp = null! };
            var pixels = new byte[] { 255, 255, 255, 255 };
            var image = new RgbaImage { Pixels = pixels, Width = 1, Height = 1 };
            var cells = renderer.Render(image, 1, 1, TuiColor.Black);

            Assert.Single(cells);
            Assert.Equal(LuminanceRampAsciiRenderer.ColorRamp[^1], cells[0].Character);
        }
    }

    public class RgbColorPaletteTests
    {
        [Fact]
        public void Nearest_ReturnsClosestColor()
        {
            Assert.Equal(ConsoleColor.Black, RgbColorPalette.Nearest(0, 0, 0));
            Assert.Equal(ConsoleColor.White, RgbColorPalette.Nearest(255, 255, 255));
            Assert.Equal(ConsoleColor.Red, RgbColorPalette.Nearest(255, 0, 0));
            Assert.Equal(ConsoleColor.Green, RgbColorPalette.Nearest(0, 255, 0));
            Assert.Equal(ConsoleColor.Blue, RgbColorPalette.Nearest(0, 0, 255));

            // Approximation
            Assert.Equal(ConsoleColor.DarkGray, RgbColorPalette.Nearest(120, 120, 120));
        }

        [Fact]
        public void ToRgb_ReturnsExpectedValues()
        {
            Assert.Equal((0, 0, 0), RgbColorPalette.ToRgb(ConsoleColor.Black));
            Assert.Equal((255, 255, 255), RgbColorPalette.ToRgb(ConsoleColor.White));
            Assert.Equal((255, 0, 0), RgbColorPalette.ToRgb(ConsoleColor.Red));
        }

        [Fact]
        public void ToRgb_InvalidColor_ReturnsBlack()
        {
            Assert.Equal((0, 0, 0), RgbColorPalette.ToRgb((ConsoleColor)100));
        }
    }
}
