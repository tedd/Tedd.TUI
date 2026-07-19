using Xunit;
using Tedd.TUI;
using System;
using Tedd.TUI.Tests.TestInfrastructure;

namespace Tedd.TUI.Tests
{
    public class ScrollBarTests
    {
        [Theory]
        [InlineData(0, 100, 150, 100)]
        [InlineData(0, 100, -50, 0)]
        [InlineData(0, 100, 50, 50)]
        [InlineData(10, 20, 5, 10)]
        public void TestValueClamping(int min, int max, int input, int expected)
        {
            var scrollBar = new ScrollBar();
            scrollBar.Minimum = min;
            scrollBar.Maximum = max;
            scrollBar.Value = input;

            Assert.Equal(expected, scrollBar.Value);
        }

        [Fact]
        public void TestRender_Vertical()
        {
            var scrollBar = new ScrollBar
            {
                Orientation = Orientation.Vertical,
                Minimum = 0,
                Maximum = 10,
                Value = 0,
                Height = 10,
                Width = 1,
                ViewportSize = 1 // Small viewport, small thumb
            };

            // Setup layout
            scrollBar.Measure(new Size(1, 10));
            scrollBar.Arrange(new Rect(0, 0, 1, 10));

            var buffer = new VirtualBuffer(1, 10);
            scrollBar.Render(buffer, 0, 0);

            // Check Arrows
            Assert.Equal('▲', buffer.GetPixel(0, 0).Character);
            Assert.Equal('▼', buffer.GetPixel(0, 9).Character);

            // Check Thumb at top (index 0 of track)
            // Track starts at y=1, ends at y=8. Length = 8.
            // Value=0 -> Thumb should be at y=1.
            Assert.Equal('█', buffer.GetPixel(0, 1).Character);

            // Check Track
            Assert.Equal('░', buffer.GetPixel(0, 5).Character);
        }

        [Fact]
        public void TestRender_Horizontal()
        {
            var scrollBar = new ScrollBar
            {
                Orientation = Orientation.Horizontal,
                Minimum = 0,
                Maximum = 10,
                Value = 10, // Max value
                Height = 1,
                Width = 10,
                ViewportSize = 1
            };

            // Setup layout
            scrollBar.Measure(new Size(10, 1));
            scrollBar.Arrange(new Rect(0, 0, 10, 1));

            var buffer = new VirtualBuffer(10, 1);
            scrollBar.Render(buffer, 0, 0);

            // Check Arrows
            Assert.Equal('◄', buffer.GetPixel(0, 0).Character);
            Assert.Equal('►', buffer.GetPixel(9, 0).Character);

            // Check Thumb at bottom (end of track)
            // Track starts at x=1, ends at x=8. Length = 8.
            // Value=10 -> Thumb should be at x=8.
            Assert.Equal('█', buffer.GetPixel(8, 0).Character);

            // Check Track
            Assert.Equal('░', buffer.GetPixel(1, 0).Character);
        }

        [Fact]
        public void MouseClick_NestedSiblingScrollBars_ChangesOnlyClickedArrow()
        {
            var vertical = new ScrollBar
            {
                Orientation = Orientation.Vertical,
                Minimum = 0,
                Maximum = 100,
                Value = 50,
                SmallChange = 5,
                Height = 10,
                Width = 1
            };
            var horizontal = new ScrollBar
            {
                Orientation = Orientation.Horizontal,
                Minimum = 0,
                Maximum = 100,
                Value = 40,
                SmallChange = 7,
                Height = 1,
                Width = 10
            };
            var canvas = new Canvas { Width = 20, Height = 14 };
            var caption = new TextBlock { Text = "Scroll controls" };
            Canvas.SetLeft(caption, 1);
            Canvas.SetTop(caption, 0);
            Canvas.SetLeft(vertical, 3);
            Canvas.SetTop(vertical, 2);
            Canvas.SetLeft(horizontal, 7);
            Canvas.SetTop(horizontal, 12);
            canvas.AddChild(caption);
            canvas.AddChild(vertical);
            canvas.AddChild(horizontal);
            var host = new ControlTestHost(new Border { Child = canvas }, 24, 18);

            host.Click(vertical, 0, 0);
            Assert.Equal(45, vertical.Value);
            Assert.Equal(40, horizontal.Value);

            host.Click(vertical, 0, vertical.RenderSize.Height - 1);
            Assert.Equal(50, vertical.Value);
            Assert.Equal(40, horizontal.Value);

            host.Click(horizontal, 0, 0);
            Assert.Equal(50, vertical.Value);
            Assert.Equal(33, horizontal.Value);

            host.Click(horizontal, horizontal.RenderSize.Width - 1, 0);
            Assert.Equal(40, horizontal.Value);
        }

        [Fact]
        public void MouseClick_NestedScrollBarTrack_PagesWithoutActivatingSibling()
        {
            var scrollBar = new ScrollBar
            {
                Orientation = Orientation.Vertical,
                Minimum = 0,
                Maximum = 100,
                Value = 50,
                LargeChange = 20,
                Height = 12,
                Width = 1,
                ViewportSize = 10
            };
            var sibling = new Button { Content = "Other", Width = 8 };
            var row = new StackPanel { Orientation = Orientation.Horizontal };
            row.AddChild(new TextBlock { Text = "  " });
            row.AddChild(scrollBar);
            row.AddChild(new TextBlock { Text = "    " });
            row.AddChild(sibling);
            var surface = new StackPanel();
            surface.AddChild(new TextBlock { Text = "Page through results" });
            surface.AddChild(row);
            surface.AddChild(new TextBlock { Text = "status" });
            var host = new ControlTestHost(new Border { Child = surface }, 24, 17);
            var siblingClicks = 0;
            sibling.Click += (_, _) => siblingClicks++;

            // At value 50, the thumb is at local Y=5; Y=2 pages up.
            host.Click(scrollBar, 0, 2);
            Assert.Equal(30, scrollBar.Value);

            // At value 30, the thumb is at local Y=3; Y=8 pages down.
            host.Click(scrollBar, 0, 8);
            Assert.Equal(50, scrollBar.Value);
            Assert.Equal(0, siblingClicks);

            host.Click(sibling, 2, 1);
            Assert.Equal(1, siblingClicks);
            Assert.Equal(50, scrollBar.Value);
        }

        [Fact]
        public void MouseDrag_NestedScrollBar_CapturesAndContinuesOutsideBounds()
        {
            var scrollBar = new ScrollBar
            {
                Orientation = Orientation.Vertical,
                Minimum = 0,
                Maximum = 100,
                Value = 0,
                Height = 102,
                Width = 1,
                ViewportSize = 1
            };
            var sibling = new ScrollBar
            {
                Orientation = Orientation.Vertical,
                Value = 25,
                Height = 12,
                Width = 1
            };
            var row = new StackPanel { Orientation = Orientation.Horizontal };
            row.AddChild(new TextBlock { Text = "  " });
            row.AddChild(scrollBar);
            row.AddChild(new TextBlock { Text = "     " });
            row.AddChild(sibling);
            var host = new ControlTestHost(new Border { Child = row }, 14, 106);
            var start = scrollBar.PointToScreen(new Point(0, 1));

            host.MouseDown(start.X, start.Y);
            Assert.Same(scrollBar, host.Window.CapturedElement);

            // Capture keeps routing to the scrollbar even far outside its one-cell width.
            host.MouseMove(start.X + 20, start.Y + 10);
            Assert.InRange(scrollBar.Value, 9, 11);
            Assert.Equal(25, sibling.Value);

            host.MouseUp(start.X + 30, start.Y + 10);
            Assert.Null(host.Window.CapturedElement);
            var valueAfterRelease = scrollBar.Value;

            host.MouseMove(start.X, start.Y + 50);
            Assert.Equal(valueAfterRelease, scrollBar.Value);
            Assert.Equal(25, sibling.Value);
        }

        [Fact]
        public void MouseDrag_SubCellMoves_ScrollsLineByLine()
        {
            // Height 12 -> inner track 10, thumb size 1, available slide 9.
            // One whole cell of thumb travel covers 100/9 ≈ 11 lines, so line-by-line
            // scrolling is only reachable through sub-cell (pixel host) precision.
            var scrollBar = new ScrollBar
            {
                Orientation = Orientation.Vertical,
                Minimum = 0,
                Maximum = 100,
                Value = 0,
                Height = 12,
                Width = 1,
                ViewportSize = 10
            };
            var host = new ControlTestHost(new Border { Child = scrollBar }, 5, 14);

            // Thumb is at local Y=1 when Value=0; press its center.
            var thumb = scrollBar.PointToScreen(new Point(0, 1));
            double x = thumb.X + 0.5;
            double y = thumb.Y + 0.5;
            host.MouseDownF(x, y);
            Assert.Same(scrollBar, host.Window.CapturedElement);
            Assert.Equal(0, scrollBar.Value);

            // 0.05 cells * 100/9 = 0.56 -> rounds to 1: a slight move scrolls one line.
            host.MouseMoveF(x, y + 0.05);
            Assert.Equal(1, scrollBar.Value);

            // 0.14 cells * 100/9 = 1.56 -> 2 lines. Mapping stays absolute, not additive.
            host.MouseMoveF(x, y + 0.14);
            Assert.Equal(2, scrollBar.Value);

            // A whole cell matches the coarse (terminal) granularity: 100/9 -> 11.
            host.MouseMoveF(x, y + 1.0);
            Assert.Equal(11, scrollBar.Value);

            // Sub-cell moves keep working far away from the bar horizontally.
            host.MouseMoveF(x + 17, y + 1.09);
            Assert.Equal(12, scrollBar.Value);

            // Returning to the anchor restores the starting value.
            host.MouseMoveF(x, y);
            Assert.Equal(0, scrollBar.Value);

            host.MouseUpF(x, y);
            Assert.Null(host.Window.CapturedElement);
        }
    }
}
