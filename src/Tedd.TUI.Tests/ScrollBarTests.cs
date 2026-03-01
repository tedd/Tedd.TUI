using Xunit;
using Tedd.TUI;
using System;

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
        public void TestInput_Step()
        {
            var scrollBar = new ScrollBar
            {
                Orientation = Orientation.Vertical,
                Minimum = 0,
                Maximum = 100,
                Value = 50,
                SmallChange = 5,
                Height = 10,
                Width = 1
            };
            scrollBar.Measure(new Size(1, 10));
            scrollBar.Arrange(new Rect(0, 0, 1, 10));

            // Click Up Arrow (0, 0)
            var args = new MouseEventArgs { X = 0, Y = 0 };
            scrollBar.OnMouseDown(args);

            Assert.Equal(45, scrollBar.Value);

            // Click Down Arrow (0, 9)
            args = new MouseEventArgs { X = 0, Y = 9 };
            scrollBar.OnMouseDown(args);

            Assert.Equal(50, scrollBar.Value);
        }

        [Fact]
        public void TestInput_Page()
        {
            var scrollBar = new ScrollBar
            {
                Orientation = Orientation.Vertical,
                Minimum = 0,
                Maximum = 100,
                Value = 50,
                LargeChange = 20,
                Height = 12, // Arrows: 2, Track: 10
                Width = 1,
                ViewportSize = 10 // Thumb size approx 1?
            };
            // Range = 100. ContentSize = 110. InnerLen = 10.
            // ThumbSize = 10 * 10 / 110 = 0.9 -> 1.
            // AvailableSlide = 9.
            // Value=50. ThumbPos = 9 * (50-0) / 100 = 4.5 -> 4.
            // Track starts at Y=1. Thumb at 1+4=5.

            scrollBar.Measure(new Size(1, 12));
            scrollBar.Arrange(new Rect(0, 0, 1, 12));

            // Click above thumb (Y=2) -> Page Up
            var args = new MouseEventArgs { X = 0, Y = 2 };
            scrollBar.OnMouseDown(args);

            Assert.Equal(30, scrollBar.Value); // 50 - 20

            // Click below thumb (Y=8) -> Page Down
            // Recalculate thumb pos? Value changed to 30.
            // ThumbPos for 30: 9 * 30 / 100 = 2.7 -> 2. Thumb at 1+2=3.
            // Y=8 is well below thumb.
            args = new MouseEventArgs { X = 0, Y = 8 };
            scrollBar.OnMouseDown(args);

            Assert.Equal(50, scrollBar.Value); // 30 + 20
        }

        [Fact]
        public void TestInput_Drag()
        {
            var scrollBar = new ScrollBar
            {
                Orientation = Orientation.Vertical,
                Minimum = 0,
                Maximum = 100,
                Value = 0,
                Height = 102, // Arrows: 2, Track: 100
                Width = 1,
                ViewportSize = 1
            };
            // InnerLen = 100. Range = 100. ContentSize = 101.
            // ThumbSize = 100 * 1 / 101 = 0 -> 1.
            // AvailableSlide = 99.
            // 1 pixel drag ~= 1 value roughly.

            scrollBar.Measure(new Size(1, 102));
            scrollBar.Arrange(new Rect(0, 0, 1, 102));

            // MouseDown on Thumb at Y=1 (Value=0)
            var downArgs = new MouseEventArgs { X = 0, Y = 1 };
            scrollBar.OnMouseDown(downArgs);

            // Drag down by 10 pixels
            var moveArgs = new MouseEventArgs { X = 0, Y = 11 };
            scrollBar.OnMouseMove(moveArgs);

            // Verify value changed
            // DeltaPixels = 10.
            // DeltaValue = 10 * 100 / 99 = 10.
            // Expected Value = 10.
            Assert.True(scrollBar.Value >= 9 && scrollBar.Value <= 11, $"Value was {scrollBar.Value}, expected ~10");

            // MouseUp
            var upArgs = new MouseEventArgs { X = 0, Y = 11 };
            scrollBar.OnMouseUp(upArgs);

            // Drag after Up should not change value
            var moveArgs2 = new MouseEventArgs { X = 0, Y = 50 };
            scrollBar.OnMouseMove(moveArgs2);

            Assert.True(scrollBar.Value >= 9 && scrollBar.Value <= 11);
        }
    }
}
