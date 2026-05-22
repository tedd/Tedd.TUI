using System;
using Xunit;
using Tedd.TUI;

namespace Tedd.TUI.Tests;

public class ValidatorDialogBoxTests
{
    [Theory]
    [InlineData(BoxStyle.Single, '\u250C', '\u2510', '\u2514', '\u2518', '\u2500', '\u2502')]
    [InlineData(BoxStyle.Double, '\u2554', '\u2557', '\u255A', '\u255D', '\u2550', '\u2551')]
    [InlineData(BoxStyle.Heavy, '\u250F', '\u2513', '\u2517', '\u251B', '\u2501', '\u2503')]
    public void CoordinatePreciseCharacterAssertion_BoxStyles_DialogBox(BoxStyle style, char tl, char tr, char bl, char br, char h, char v)
    {
        var dialog = new DialogBox
        {
            BoxStyle = style,
            Width = 10,
            Height = 10,
            Title = "A" // Very short title to fit within brackets
        };

        var root = new TuiWindow();
        root.Measure(new Size(20, 20));
        root.Arrange(new Rect(0, 0, 20, 20));

        root.PushOverlay(dialog);

        // Explicit measure and arrange for overlay to match constraints
        dialog.Measure(new Size(10, 10));
        dialog.Arrange(new Rect(0, 0, 10, 10));

        var buffer = new VirtualBuffer(10, 10);
        dialog.Render(buffer, 0, 0);

        // Verify corners
        Assert.Equal(tl, buffer.GetPixel(0, 0).Character);
        Assert.Equal(tr, buffer.GetPixel(9, 0).Character);
        Assert.Equal(bl, buffer.GetPixel(0, 9).Character);
        Assert.Equal(br, buffer.GetPixel(9, 9).Character);

        // Verify horizontal edges (sample bottom point to avoid title area)
        Assert.Equal(h, buffer.GetPixel(5, 9).Character);

        // Verify vertical edges (sample middle points)
        Assert.Equal(v, buffer.GetPixel(0, 5).Character);
        Assert.Equal(v, buffer.GetPixel(9, 5).Character);
    }

    [Fact]
    public void HierarchicalCompositionValidation_DynamicStateMutation_Overlay()
    {
        var root = new TuiWindow();
        root.Width = 30;
        root.Height = 20;

        var dialog = new DialogBox
        {
            BoxStyle = BoxStyle.Double,
            Title = "D"
        };
        dialog.Content = new TextBlock { Text = "Test" };

        root.PushOverlay(dialog);

        // Standard measure/arrange of window
        root.Measure(new Size(30, 20));
        root.Arrange(new Rect(0, 0, 30, 20));

        // When shown via Show(), it centers it based on DesiredSize. Wait, let's call Show() manually if needed, or mock it.
        dialog.Show();

        // The DialogBox centered: Width depends on content + border. Content is "Test" (4).
        // Border adds 2 + padding. DialogBox defaults say: Width > 0 ? Width : 40.
        // Wait, DialogBox desiredWidth = Math.Max(contentSize.Width + 2, titleWidth).
        // Let's assert its actual render size or buffer presence.

        var buffer = new VirtualBuffer(30, 20);
        root.Render(buffer, 0, 0);

        // Assert it rendered its border.
        // Let's look for Double TopLeft.
        bool foundDoubleTL = false;
        for (int y = 0; y < 20; y++)
        {
            for (int x = 0; x < 30; x++)
            {
                if (buffer.GetPixel(x, y).Character == '\u2554')
                {
                    foundDoubleTL = true;
                    break;
                }
            }
        }
        Assert.True(foundDoubleTL, "Dialog border TopLeft should be visible in the buffer");

        // Dynamic State Mutation: Resize window
        root.Measure(new Size(50, 30));
        root.Arrange(new Rect(0, 0, 50, 30));

        // Let's explicitly reposition the overlay since TuiWindow might not automatically trigger overlay layout
        dialog.Measure(new Size(50, 30));
        dialog.Arrange(new Rect(0, 0, dialog.DesiredSize.Width, dialog.DesiredSize.Height));

        var resizedBuffer = new VirtualBuffer(50, 30);
        root.Render(resizedBuffer, 0, 0);

        // Find Double TL again.
        bool foundDoubleTLResized = false;
        for (int y = 0; y < 30; y++)
        {
            for (int x = 0; x < 50; x++)
            {
                if (resizedBuffer.GetPixel(x, y).Character == '\u2554')
                {
                    foundDoubleTLResized = true;
                    break;
                }
            }
        }
        Assert.True(foundDoubleTLResized, "Dialog border TopLeft should be visible after resize");
    }

    [Fact]
    public void BoundaryAndEdgeVerification_ZeroSize_SingleSize()
    {
        var dialog = new DialogBox { BoxStyle = BoxStyle.Single, Title = "Z" };

        // 0x0
        dialog.Measure(new Size(0, 0));
        dialog.Arrange(new Rect(0, 0, 0, 0));
        var buffer0 = new VirtualBuffer(10, 10);
        dialog.Render(buffer0, 0, 0);

        // At 0x0, it shouldn't render since w < 2 || h < 2 returns early.
        Assert.Equal(' ', buffer0.GetPixel(0, 0).Character);

        // 1x1
        dialog.Measure(new Size(1, 1));
        dialog.Arrange(new Rect(0, 0, 1, 1));
        var buffer1 = new VirtualBuffer(10, 10);
        dialog.Render(buffer1, 0, 0);
        Assert.Equal(' ', buffer1.GetPixel(0, 0).Character);
    }
}
