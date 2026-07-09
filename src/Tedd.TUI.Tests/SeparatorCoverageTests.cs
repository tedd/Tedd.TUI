using Xunit;
using Tedd.TUI;

namespace Tedd.TUI.Tests
{
    public class SeparatorCoverageTests
    {
        [Theory]
        [InlineData(10, 1, true, 10)]
        [InlineData(5, 1, true, 5)]
        public void Separator_WithBackground_RendersUsingDrawHLine(int width, int height, bool hasBackground, int expectedWidth)
        {
            var sep = new Separator();
            sep.Measure(new Size(width, height));
            sep.Arrange(new Rect(0, 0, width, height));

            if (hasBackground)
            {
                sep.Background = TuiColor.Blue;
            }

            var buffer = new VirtualBuffer(width, height);
            buffer.FillRect(0, 0, width, height, ' ', TuiColor.White, TuiColor.Red);

            sep.Render(buffer, 0, 0);

            for (int x = 0; x < expectedWidth; x++)
            {
                var cell = buffer.GetPixel(x, 0);
                Assert.Equal('\u2500', cell.Character);
                if (hasBackground)
                {
                    Assert.Equal(TuiColor.Blue, cell.Background);
                }
            }
        }

        [Theory]
        [InlineData(0, 10)]
        [InlineData(10, 0)]
        [InlineData(-1, 5)]
        [InlineData(5, -1)]
        public void Separator_ZeroOrNegativeWidthOrHeight_DoesNotRender(int width, int height)
        {
            var sep = new Separator();
            sep.Measure(new Size(10, 10)); // Initial measure
            sep.Arrange(new Rect(0, 0, width, height)); // Arrange to potentially 0 or negative width/height

            var buffer = new VirtualBuffer(10, 10);
            buffer.FillRect(0, 0, 10, 10, ' ', TuiColor.White, TuiColor.Red);

            sep.Render(buffer, 0, 0);

            for (int x = 0; x < 10; x++)
            {
                for (int y = 0; y < 10; y++)
                {
                    var cell = buffer.GetPixel(x, y);
                    Assert.Equal(' ', cell.Character);
                    Assert.Equal(TuiColor.Red, cell.Background);
                }
            }
        }

        [Fact]
        public void Separator_WithTemplate_RendersTemplate()
        {
            var sep = new Separator();
            sep.Template = new ControlTemplate(_ =>
            {
                var textBlock = new TextBlock { Text = "Test" };
                return textBlock;
            });

            sep.Measure(new Size(10, 10));
            sep.Arrange(new Rect(0, 0, 10, 10));

            var buffer = new VirtualBuffer(10, 10);
            buffer.FillRect(0, 0, 10, 10, ' ', TuiColor.White, TuiColor.Red);

            sep.Render(buffer, 0, 0);

            // Should render the "Test" text from template instead of line
            Assert.Equal('T', buffer.GetPixel(0, 0).Character);
            Assert.Equal('e', buffer.GetPixel(1, 0).Character);
            Assert.Equal('s', buffer.GetPixel(2, 0).Character);
            Assert.Equal('t', buffer.GetPixel(3, 0).Character);
        }
    }
}
