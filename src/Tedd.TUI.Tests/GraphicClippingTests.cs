using System.Collections.Generic;
using Tedd.TUI;
using Xunit;

namespace Tedd.TUI.Tests
{
    /// <summary>
    /// Bitmap placements have to obey the same clip stack text writes do. Without that, an image
    /// inside a <see cref="ScrollViewer"/> is drawn at its absolute position whatever the scroll
    /// offset, so scrolling it out of view leaves it painted over the surroundings — scrollbars
    /// included.
    /// </summary>
    public class GraphicClippingTests
    {
        private static VirtualBuffer BufferWithGraphics(int w, int h) =>
            new VirtualBuffer(w, h) { Graphics = new List<GraphicPlacement>() };

        private static GraphicPlacement At(int x, int y, int w, int h) => new GraphicPlacement
        {
            CharX = x,
            CharY = y,
            CharWidth = w,
            CharHeight = h,
            ImageData = new byte[] { 1, 2, 3 },
            MediaType = "image/png",
        };

        [Fact]
        public void FullyInsideClip_IsAddedWhole()
        {
            var buffer = BufferWithGraphics(20, 10);
            buffer.PushClip(new Rect(0, 0, 20, 10));

            Assert.True(buffer.AddGraphic(At(2, 2, 4, 3)));

            var g = Assert.Single(buffer.Graphics!);
            Assert.False(g.IsClipped);
            Assert.Equal(2, g.CharX);
            Assert.Equal(4, g.CharWidth);
        }

        [Fact]
        public void FullyOutsideClip_IsDropped()
        {
            var buffer = BufferWithGraphics(20, 10);
            buffer.PushClip(new Rect(0, 0, 20, 4));

            // Scrolled well below the viewport: nothing of it shows.
            Assert.False(buffer.AddGraphic(At(0, 6, 4, 3)));

            Assert.Empty(buffer.Graphics!);
        }

        [Fact]
        public void PartlyAboveClip_KeepsFullRectAndRecordsVisibleRegion()
        {
            var buffer = BufferWithGraphics(20, 10);
            buffer.PushClip(new Rect(0, 4, 20, 6));

            // Six rows tall starting two rows above the viewport top.
            Assert.True(buffer.AddGraphic(At(0, 2, 4, 6)));

            var g = Assert.Single(buffer.Graphics!);
            Assert.True(g.IsClipped);

            // The placement still describes the whole image, so a surface crops it instead of
            // squashing it and the aspect ratio survives.
            Assert.Equal(2, g.CharY);
            Assert.Equal(6, g.CharHeight);

            // Only the part below the viewport top is visible.
            Assert.Equal(4, g.ClipCharY);
            Assert.Equal(4, g.ClipCharHeight);
            Assert.Equal(0, g.ClipCharX);
            Assert.Equal(4, g.ClipCharWidth);
        }

        [Fact]
        public void PartlyRightOfClip_IsCutAtTheViewportEdge()
        {
            var buffer = BufferWithGraphics(20, 10);
            // A viewport one column narrower than the surface, as a vertical scrollbar leaves it.
            buffer.PushClip(new Rect(0, 0, 19, 10));

            Assert.True(buffer.AddGraphic(At(16, 0, 6, 3)));

            var g = Assert.Single(buffer.Graphics!);
            Assert.True(g.IsClipped);
            Assert.Equal(16, g.ClipCharX);
            Assert.Equal(3, g.ClipCharWidth); // stops at column 19, off the scrollbar
        }

        [Fact]
        public void NestedClipsIntersect()
        {
            var buffer = BufferWithGraphics(20, 10);
            buffer.PushClip(new Rect(0, 0, 20, 8));
            buffer.PushClip(new Rect(0, 3, 20, 10));

            Assert.True(buffer.AddGraphic(At(0, 0, 4, 20)));

            var g = Assert.Single(buffer.Graphics!);
            Assert.True(g.IsClipped);
            Assert.Equal(3, g.ClipCharY);
            Assert.Equal(5, g.ClipCharHeight); // rows 3..7
        }

        [Fact]
        public void WithNoClipPushed_StillStopsAtTheSurfaceEdge()
        {
            var buffer = BufferWithGraphics(10, 5);

            Assert.True(buffer.AddGraphic(At(8, 0, 6, 2)));

            var g = Assert.Single(buffer.Graphics!);
            Assert.True(g.IsClipped);
            Assert.Equal(2, g.ClipCharWidth); // columns 8..9
        }

        [Fact]
        public void PoppingAClipRestoresTheFullSurface()
        {
            var buffer = BufferWithGraphics(20, 10);
            buffer.PushClip(new Rect(0, 0, 20, 2));
            buffer.PopClip();

            Assert.True(buffer.AddGraphic(At(0, 4, 4, 3)));

            var g = Assert.Single(buffer.Graphics!);
            Assert.False(g.IsClipped);
        }

        [Fact]
        public void TextOnlySurface_AcceptsNoPlacements()
        {
            // Graphics stays null on surfaces that cannot composite bitmaps.
            var buffer = new VirtualBuffer(20, 10);

            Assert.False(buffer.AddGraphic(At(0, 0, 4, 3)));
            Assert.Null(buffer.Graphics);
        }

        [Fact]
        public void DegeneratePlacementIsRejected()
        {
            var buffer = BufferWithGraphics(20, 10);

            Assert.False(buffer.AddGraphic(At(0, 0, 0, 3)));
            Assert.False(buffer.AddGraphic(At(0, 0, 4, 0)));
            Assert.Empty(buffer.Graphics!);
        }
    }
}
