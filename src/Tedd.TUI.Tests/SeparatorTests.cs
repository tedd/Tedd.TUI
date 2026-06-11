using System;
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
            sep.Render(buffer, 0, 0);

            // Check that it rendered a horizontal line
            Assert.Equal('\u2500', buffer.GetPixel(5, 0).Character);
        }
    }
}
