using System;
using System.Collections.Generic;
using Tedd.TUI;
using Xunit;

namespace Tedd.TUI.Tests
{
    /// <summary>
    /// Covers the pre-rendered scroll region path: <see cref="ScrollViewer"/> and its
    /// derivatives render their whole content into a <see cref="ScrollPane"/> when the surface
    /// offers a <see cref="VirtualBuffer.ScrollPanes"/> channel, instead of clipping it.
    /// </summary>
    public class ScrollPaneTests
    {
        /// <summary>Ten single-character lines stacked vertically: "L0".."L9", one row each.</summary>
        private static StackPanel TallContent(int lines = 10, int width = 6)
        {
            var panel = new StackPanel { Orientation = Orientation.Vertical };
            for (int i = 0; i < lines; i++)
            {
                panel.Children.Add(new TextBlock { Text = "L" + i, Width = width, Height = 1 });
            }
            return panel;
        }

        private static ScrollViewer LayOutViewer(UIElement content, int w, int h)
        {
            var viewer = new ScrollViewer
            {
                Content = content,
                VerticalScrollBarVisibility = ScrollBarVisibility.Visible,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            };
            viewer.Measure(new Size(w, h));
            viewer.Arrange(new Rect(0, 0, w, h));
            return viewer;
        }

        private static string RowText(VirtualBuffer buffer, int y)
        {
            var chars = new char[buffer.Width];
            for (int x = 0; x < buffer.Width; x++) chars[x] = buffer.GetPixel(x, y).Character;
            return new string(chars).TrimEnd();
        }

        // The regression gate that matters most: every non-DOM host leaves ScrollPanes null, and
        // for those the render path must be exactly what it was before panes existed.
        [Fact]
        public void NoChannel_ClipsAsBefore_AndRegistersNoPane()
        {
            var viewer = LayOutViewer(TallContent(), 8, 4);
            var buffer = new VirtualBuffer(8, 4);

            viewer.Render(buffer, 0, 0);

            Assert.Null(buffer.ScrollPanes);
            // Only the viewport rows were drawn; row 3 is the last visible one.
            Assert.Equal("L0", RowText(buffer, 0));
            Assert.Equal("L3", RowText(buffer, 3));
        }

        [Fact]
        public void WithChannel_RegistersPaneCarryingFullExtent()
        {
            var viewer = LayOutViewer(TallContent(), 8, 4);
            var buffer = new VirtualBuffer(8, 4) { ScrollPanes = new List<ScrollPane>() };

            viewer.Render(buffer, 0, 0);

            var pane = Assert.Single(buffer.ScrollPanes!);

            // The pane buffer holds all ten lines, not just the four that fit.
            Assert.Equal(10, pane.Content.Height);
            Assert.Equal("L0", RowText(pane.Content, 0));
            Assert.Equal("L9", RowText(pane.Content, 9));

            // The viewport is the visible box, minus the column the scrollbar occupies.
            Assert.Equal(0, pane.Viewport.X);
            Assert.Equal(0, pane.Viewport.Y);
            Assert.Equal(7, pane.Viewport.Width);
            Assert.Equal(4, pane.Viewport.Height);
        }

        [Fact]
        public void PaneOffsetTracksScrollBarValue()
        {
            var viewer = LayOutViewer(TallContent(), 8, 4);
            viewer.ScrollToVerticalOffset(3);

            var buffer = new VirtualBuffer(8, 4) { ScrollPanes = new List<ScrollPane>() };
            viewer.Render(buffer, 0, 0);

            var pane = Assert.Single(buffer.ScrollPanes!);
            Assert.Equal(3, pane.OffsetY);
            Assert.Equal(0, pane.OffsetX);

            // Content is pinned to the pane origin; the surface applies the offset, not the render.
            Assert.Equal("L0", RowText(pane.Content, 0));
        }

        [Fact]
        public void ScrollBarStillRendersIntoTheOwningBuffer()
        {
            var viewer = LayOutViewer(TallContent(), 8, 4);
            var buffer = new VirtualBuffer(8, 4) { ScrollPanes = new List<ScrollPane>() };

            viewer.Render(buffer, 0, 0);

            // The bar is not scrolled, so it must stay on the flat grid rather than move
            // into the pane. Column 7 is the reserved scrollbar column.
            Assert.Equal('▲', buffer.GetPixel(7, 0).Character);
            Assert.Equal('▼', buffer.GetPixel(7, 3).Character);
        }

        [Fact]
        public void ContentThatFits_StaysOnTheClipPath()
        {
            // Two lines in a four-row viewport: nothing to scroll, so pre-rendering would only
            // cost nodes. The clip path is pixel-identical and cheaper.
            var viewer = LayOutViewer(TallContent(lines: 2), 8, 4);
            var buffer = new VirtualBuffer(8, 4) { ScrollPanes = new List<ScrollPane>() };

            viewer.Render(buffer, 0, 0);

            Assert.Empty(buffer.ScrollPanes!);
            Assert.Equal("L0", RowText(buffer, 0));
        }

        [Fact]
        public void OptedOutViewer_TakesTheClipPathEvenWithAChannel()
        {
            var viewer = LayOutViewer(TallContent(), 8, 4);
            ScrollViewer.SetPrerenderContent(viewer, false);

            var buffer = new VirtualBuffer(8, 4) { ScrollPanes = new List<ScrollPane>() };
            viewer.Render(buffer, 0, 0);

            Assert.Empty(buffer.ScrollPanes!);
            Assert.Equal("L0", RowText(buffer, 0));
            Assert.Equal("L3", RowText(buffer, 3));
        }

        [Fact]
        public void PrerenderContent_DefaultsToTrue()
        {
            Assert.True(new ScrollViewer().PrerenderContent);
        }

        [Fact]
        public void NestedViewer_RegistersInsideTheOuterPane()
        {
            // The inner viewer needs an explicit Height: an unbounded parent otherwise arranges
            // it at its full desired extent, leaving it nothing to scroll.
            var inner = new ScrollViewer
            {
                Content = TallContent(lines: 12),
                Height = 5,
                VerticalScrollBarVisibility = ScrollBarVisibility.Visible,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            };

            var outer = new ScrollViewer
            {
                Content = inner,
                VerticalScrollBarVisibility = ScrollBarVisibility.Visible,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            };
            outer.Measure(new Size(10, 3));
            outer.Arrange(new Rect(0, 0, 10, 3));

            var buffer = new VirtualBuffer(10, 3) { ScrollPanes = new List<ScrollPane>() };
            outer.Render(buffer, 0, 0);

            var outerPane = Assert.Single(buffer.ScrollPanes!);

            // Nesting needs no bookkeeping: the inner viewer registered into the outer pane's own
            // channel, so the surface just recurses.
            Assert.NotNull(outerPane.Content.ScrollPanes);
            var innerPane = Assert.Single(outerPane.Content.ScrollPanes!);
            Assert.Equal(12, innerPane.Content.Height);
        }

        [Fact]
        public void BorderPaneViewportSitsInsideTheBorderAndPadding()
        {
            var border = new Border
            {
                Child = TallContent(lines: 12),
                BoxStyle = BoxStyle.Single,
                Padding = new Thickness(1, 0, 0, 0),
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            };
            border.Measure(new Size(10, 5));
            border.Arrange(new Rect(0, 0, 10, 5));

            var buffer = new VirtualBuffer(10, 5) { ScrollPanes = new List<ScrollPane>() };
            border.Render(buffer, 0, 0);

            var pane = Assert.Single(buffer.ScrollPanes!);

            // One cell in for the border line, plus the left padding.
            Assert.Equal(2, pane.Viewport.X);
            Assert.Equal(1, pane.Viewport.Y);
            Assert.Equal(12, pane.Content.Height);

            // The border frame itself is still on the flat grid underneath the pane.
            Assert.Equal('┌', buffer.GetPixel(0, 0).Character);
        }

        [Fact]
        public void PaneInheritsTheBackgroundItSitsOn()
        {
            // Children that render with a transparent background read what they sit on via
            // GetPixel. Seeding the pane with the cell at the viewport origin keeps that working.
            // The labels are left-aligned and narrower than the pane, so the right-hand columns
            // show the seed rather than anything the content painted.
            var content = new StackPanel { Orientation = Orientation.Vertical };
            for (int i = 0; i < 12; i++)
            {
                content.Children.Add(new TextBlock
                {
                    Text = "L" + i,
                    Width = 3,
                    Height = 1,
                    HorizontalAlignment = HorizontalAlignment.Left,
                });
            }

            var border = new Border
            {
                Child = content,
                BoxStyle = BoxStyle.Single,
                Background = TuiColor.Blue,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            };
            border.Measure(new Size(10, 5));
            border.Arrange(new Rect(0, 0, 10, 5));

            var buffer = new VirtualBuffer(10, 5) { ScrollPanes = new List<ScrollPane>() };
            border.Render(buffer, 0, 0);

            var pane = Assert.Single(buffer.ScrollPanes!);
            Assert.Equal(8, pane.Content.Width);
            // A blank pane would clear this to the default black instead.
            Assert.Equal(TuiColor.Blue, pane.Content.GetPixel(7, 0).Background);
        }
    }
}
