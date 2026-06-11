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
    }
}
