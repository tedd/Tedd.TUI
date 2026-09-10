using Xunit;
using Tedd.TUI;

namespace Tedd.TUI.Tests
{
    public class SeparatorTests
    {
        [Fact]
        public void Separator_CanBeAddedToMenuItem()
        {
            var menu = new MenuItem();
            var separator = new Separator();
            menu.Items.Add(separator);

            Assert.Single(menu.Items);
            Assert.IsType<Separator>(menu.Items[0]);
        }

        [Fact]
        public void Separator_RendersHorizontalLine()
        {
            var sep = new Separator();
            sep.Measure(new Size(10, 10));
            sep.Arrange(new Rect(0, 0, 10, 1));

            var buffer = new VirtualBuffer(10, 5);
            buffer.FillRect(0, 0, 10, 5, ' ', TuiColor.White, TuiColor.Red);

            sep.Render(buffer, 0, 0);

            for (int x = 0; x < 10; x++)
            {
                var cell = buffer.GetPixel(x, 0);
                Assert.Equal('\u2500', cell.Character);
                Assert.Equal(TuiColor.Red, cell.Background);
            }
        }

        [Theory]
        [InlineData((int)ConsoleColor.Blue, (int)ConsoleColor.Yellow)]
        [InlineData((int)ConsoleColor.Green, (int)ConsoleColor.Magenta)]
        public void Separator_RendersWithCustomColors(int bgRaw, int fgRaw)
        {
            var bg = (TuiColor)(ConsoleColor)bgRaw;
            var fg = (TuiColor)(ConsoleColor)fgRaw;

            var sep = new Separator();
            sep.Background = bg;
            sep.Foreground = fg;
            sep.Padding = new Thickness(1);
            sep.Measure(new Size(10, 10));
            sep.Arrange(new Rect(0, 0, 10, 3));

            var buffer = new VirtualBuffer(10, 5);
            buffer.FillRect(0, 0, 10, 5, ' ', TuiColor.White, TuiColor.Black);

            sep.Render(buffer, 0, 0);

            // Padding is 1, so x = 1 to 8, y = 1, width = 8
            for (int x = 1; x < 9; x++)
            {
                var cell = buffer.GetPixel(x, 1);
                Assert.Equal('\u2500', cell.Character);
                Assert.Equal(bg, cell.Background);
                Assert.Equal(fg, cell.Foreground);
            }
        }

        [Theory]
        [InlineData(0, 10)]
        [InlineData(10, 0)]
        [InlineData(0, 0)]
        public void Separator_Render_ZeroDimension_DoesNotRender(int width, int height)
        {
            var sep = new Separator();
            sep.Measure(new Size(width, height));
            sep.Arrange(new Rect(0, 0, width, height));

            var buffer = new VirtualBuffer(10, 10);
            buffer.FillRect(0, 0, 10, 10, 'A', TuiColor.White, TuiColor.Black);

            sep.Render(buffer, 0, 0);

            // Verify no rendering happened
            for (int x = 0; x < 10; x++)
            {
                for (int y = 0; y < 10; y++)
                {
                    Assert.Equal('A', buffer.GetPixel(x, y).Character);
                }
            }
        }

        [Fact]
        public void Separator_Render_WithTemplateRoot_CallsBaseRender()
        {
            var sep = new Separator();

            var textBlock = new TextBlock { Text = "Template" };
            var template = new ControlTemplate(_ => textBlock);
            sep.Template = template;

            sep.Measure(new Size(10, 10));
            sep.Arrange(new Rect(0, 0, 10, 1));

            var buffer = new VirtualBuffer(10, 5);
            buffer.FillRect(0, 0, 10, 5, ' ', TuiColor.White, TuiColor.Black);

            sep.Render(buffer, 0, 0);

            // Check that TemplateRoot rendered instead of horizontal line
            Assert.Equal('T', buffer.GetPixel(0, 0).Character);
            Assert.Equal('e', buffer.GetPixel(1, 0).Character);
        }
    }
}
