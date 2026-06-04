using System;
using System.IO;
using Tedd.TUI;
using Tedd.TUI.Markdown;
using Xunit;

namespace Tedd.TUI.Tests
{
    public class HyperlinkCoverageTests
    {
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void Measure_EmptyText_ReturnsZero(string? text)
        {
            var hyperlink = new Hyperlink { Text = text! };
            hyperlink.Measure(new Size(100, 100));
            Assert.Equal(0, hyperlink.DesiredSize.Width);
            Assert.Equal(0, hyperlink.DesiredSize.Height);
        }

        [Theory]
        [InlineData("Link", 4)]
        public void Measure_ValidText_ReturnsExpectedWidth(string text, int expectedWidth)
        {
            var hyperlink = new Hyperlink { Text = text };
            hyperlink.Measure(new Size(100, 100));
            Assert.Equal(expectedWidth, hyperlink.DesiredSize.Width);
            Assert.Equal(1, hyperlink.DesiredSize.Height);
        }

        [Fact]
        public void Render_EmptyText_DoesNothing()
        {
            var buffer = new VirtualBuffer(10, 10);
            var hyperlink = new Hyperlink { Text = "" };
            hyperlink.Measure(new Size(10, 1));
            hyperlink.Arrange(new Rect(0, 0, 10, 1));
            hyperlink.Render(buffer, 0, 0);

            // Verify buffer is empty
            for (int x = 0; x < 10; x++)
            {
                for (int y = 0; y < 10; y++)
                {
                    Assert.Equal(' ', buffer.GetPixel(x, y).Character);
                }
            }
        }

        [Fact]
        public void Render_ValidText_DrawsText()
        {
            var buffer = new VirtualBuffer(10, 10);
            var hyperlink = new Hyperlink { Text = "Test" };
            hyperlink.Measure(new Size(10, 1));
            hyperlink.Arrange(new Rect(0, 0, 10, 1));
            hyperlink.Render(buffer, 0, 0);

            Assert.Equal('T', buffer.GetPixel(0, 0).Character);
            Assert.Equal('e', buffer.GetPixel(1, 0).Character);
            Assert.Equal('s', buffer.GetPixel(2, 0).Character);
            Assert.Equal('t', buffer.GetPixel(3, 0).Character);
        }

        [Fact]
        public void Render_Focused_ChangesForeground()
        {
            var buffer = new VirtualBuffer(10, 10);
            var hyperlink = new Hyperlink { Text = "Test", Foreground = TuiColor.Red };
            hyperlink.IsFocused = true;
            hyperlink.Measure(new Size(10, 1));
            hyperlink.Arrange(new Rect(0, 0, 10, 1));
            hyperlink.Render(buffer, 0, 0);

            Assert.Equal(TuiColor.Cyan, buffer.GetPixel(0, 0).Foreground);
        }

        [Fact]
        public void Render_UsesBackgroundFromBuffer_IfNull()
        {
            var buffer = new VirtualBuffer(10, 10);
            buffer.SetPixel(0, 0, ' ', TuiColor.White, TuiColor.Green);
            var hyperlink = new Hyperlink { Text = "A", Background = null };
            hyperlink.Measure(new Size(10, 1));
            hyperlink.Arrange(new Rect(0, 0, 10, 1));
            hyperlink.Render(buffer, 0, 0);

            Assert.Equal(TuiColor.Green, buffer.GetPixel(0, 0).Background);
        }

        [Fact]
        public void Render_ClipsText()
        {
            var buffer = new VirtualBuffer(10, 10);
            var hyperlink = new Hyperlink { Text = "Testing" };
            hyperlink.Measure(new Size(10, 1));
            hyperlink.Arrange(new Rect(0, 0, 4, 1)); // Smaller than text
            hyperlink.Render(buffer, 0, 0);

            Assert.Equal('T', buffer.GetPixel(0, 0).Character);
            Assert.Equal('e', buffer.GetPixel(1, 0).Character);
            Assert.Equal('s', buffer.GetPixel(2, 0).Character);
            Assert.Equal('t', buffer.GetPixel(3, 0).Character);
            Assert.Equal(' ', buffer.GetPixel(4, 0).Character); // Should not be drawn
        }

        [Fact]
        public void Interactions_TriggersClick()
        {
            var window = new TuiWindow { Width = 100, Height = 100 };
            var hyperlink = new Hyperlink { Text = "Test" };
            window.Content = hyperlink;

            bool clicked = false;
            hyperlink.Click += (s, e) => clicked = true;

            hyperlink.OnMouseDown(new MouseEventArgs { X = 0, Y = 0 });
            Assert.True(clicked);
            Assert.True(hyperlink.IsFocused);

            clicked = false;
            hyperlink.OnKeyDown(new KeyEventArgs { Key = ConsoleKey.Spacebar, KeyChar = ' ' });
            Assert.True(clicked);

            clicked = false;
            hyperlink.OnKeyDown(new KeyEventArgs { Key = ConsoleKey.Enter, KeyChar = '\n' });
            Assert.True(clicked);

            // Other keys don't trigger
            clicked = false;
            hyperlink.OnKeyDown(new KeyEventArgs { Key = ConsoleKey.A, KeyChar = 'a' });
            Assert.False(clicked);
        }

        [Fact]
        public void UrlProperty_SetGet_Works()
        {
            var hyperlink = new Hyperlink();
            hyperlink.Url = "http://example.com";
            Assert.Equal("http://example.com", hyperlink.Url);
        }
    }
}
