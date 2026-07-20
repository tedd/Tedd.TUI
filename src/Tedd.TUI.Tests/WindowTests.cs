using System;
using Xunit;
using Tedd.TUI;

namespace Tedd.TUI.Tests;

public class WindowTests
{
    private static TuiWindow CreateHost(int width = 80, int height = 25)
    {
        var host = new TuiWindow();
        host.Measure(new Size(width, height));
        host.Arrange(new Rect(0, 0, width, height));
        return host;
    }

    private static void SendMouse(TuiWindow host, RoutedEvent evt, int x, int y)
    {
        host.ProcessMouse(new MouseEventArgs(evt) { GlobalX = x, GlobalY = y });
    }

    [Fact]
    public void Properties_DefaultValues()
    {
        var w = new Window();
        Assert.Equal(string.Empty, w.Title);
        Assert.True(w.CanMove);
        Assert.True(w.CanResize);
        Assert.True(w.ShowCloseButton);
        Assert.Equal(10, w.MinWidth);
        Assert.Equal(3, w.MinHeight);
        Assert.Equal(-1, w.Left);
        Assert.Equal(-1, w.Top);
        Assert.Null(w.Content);
    }

    [Fact]
    public void Show_CentersWhenNoPositionSet()
    {
        var host = CreateHost();
        var win = new Window { Width = 20, Height = 5 };
        win.Show(host);

        Assert.Equal(win, host.Overlay);
        Assert.True(win.Visibility);
        Assert.Equal((80 - 20) / 2, win.RenderSize.X);
        Assert.Equal((25 - 5) / 2, win.RenderSize.Y);
    }

    [Fact]
    public void Show_UsesExplicitPosition()
    {
        var host = CreateHost();
        var win = new Window { Width = 20, Height = 5, Left = 3, Top = 2 };
        win.Show(host);

        Assert.Equal(3, win.RenderSize.X);
        Assert.Equal(2, win.RenderSize.Y);
    }

    [Fact]
    public void Show_ClampsPositionInsideHost()
    {
        var host = CreateHost();
        var win = new Window { Width = 20, Height = 5, Left = 200, Top = 100 };
        win.Show(host);

        Assert.Equal(80 - 20, win.RenderSize.X);
        Assert.Equal(25 - 5, win.RenderSize.Y);
    }

    [Fact]
    public void DragTitleBar_MovesWindow()
    {
        var host = CreateHost();
        var win = new Window { Width = 20, Height = 5, Left = 10, Top = 5 };
        win.Show(host);

        // Title bar cell (not corner, not close button): y = 5, x = 15
        SendMouse(host, UIElement.MouseDownEvent, 15, 5);
        SendMouse(host, UIElement.MouseMoveEvent, 20, 8);
        SendMouse(host, UIElement.MouseUpEvent, 20, 8);

        Assert.Equal(15, win.RenderSize.X);
        Assert.Equal(8, win.RenderSize.Y);
        Assert.Equal(15, win.Left);
        Assert.Equal(8, win.Top);
        // Size unchanged
        Assert.Equal(20, win.RenderSize.Width);
        Assert.Equal(5, win.RenderSize.Height);
    }

    [Fact]
    public void DragTitleBar_CanMoveFalse_DoesNotMove()
    {
        var host = CreateHost();
        var win = new Window { Width = 20, Height = 5, Left = 10, Top = 5, CanMove = false };
        win.Show(host);

        SendMouse(host, UIElement.MouseDownEvent, 15, 5);
        SendMouse(host, UIElement.MouseMoveEvent, 20, 8);
        SendMouse(host, UIElement.MouseUpEvent, 20, 8);

        Assert.Equal(10, win.RenderSize.X);
        Assert.Equal(5, win.RenderSize.Y);
    }

    [Fact]
    public void DragBottomRightCorner_Resizes()
    {
        var host = CreateHost();
        var win = new Window { Width = 20, Height = 5, Left = 10, Top = 5 };
        win.Show(host);

        // Bottom-right corner: x = 10+20-1 = 29, y = 5+5-1 = 9
        SendMouse(host, UIElement.MouseDownEvent, 29, 9);
        SendMouse(host, UIElement.MouseMoveEvent, 34, 12);
        SendMouse(host, UIElement.MouseUpEvent, 34, 12);

        Assert.Equal(25, win.RenderSize.Width);
        Assert.Equal(8, win.RenderSize.Height);
        // Position unchanged
        Assert.Equal(10, win.RenderSize.X);
        Assert.Equal(5, win.RenderSize.Y);
    }

    [Fact]
    public void DragRightEdge_ResizesWidthOnly()
    {
        var host = CreateHost();
        var win = new Window { Width = 20, Height = 5, Left = 10, Top = 5 };
        win.Show(host);

        // Right edge, mid-height: x = 29, y = 7
        SendMouse(host, UIElement.MouseDownEvent, 29, 7);
        SendMouse(host, UIElement.MouseMoveEvent, 25, 7);
        SendMouse(host, UIElement.MouseUpEvent, 25, 7);

        Assert.Equal(16, win.RenderSize.Width);
        Assert.Equal(5, win.RenderSize.Height);
    }

    [Fact]
    public void DragLeftEdge_MovesLeftAndResizes()
    {
        var host = CreateHost();
        var win = new Window { Width = 20, Height = 5, Left = 10, Top = 5 };
        win.Show(host);

        // Left edge, mid-height: x = 10, y = 7. Drag left by 4.
        SendMouse(host, UIElement.MouseDownEvent, 10, 7);
        SendMouse(host, UIElement.MouseMoveEvent, 6, 7);
        SendMouse(host, UIElement.MouseUpEvent, 6, 7);

        Assert.Equal(6, win.RenderSize.X);
        Assert.Equal(24, win.RenderSize.Width);
    }

    [Fact]
    public void Resize_RespectsMinimumSize()
    {
        var host = CreateHost();
        var win = new Window { Width = 20, Height = 5, Left = 10, Top = 5, MinWidth = 12, MinHeight = 4 };
        win.Show(host);

        // Drag bottom-right corner far up-left
        SendMouse(host, UIElement.MouseDownEvent, 29, 9);
        SendMouse(host, UIElement.MouseMoveEvent, 5, 2);
        SendMouse(host, UIElement.MouseUpEvent, 5, 2);

        Assert.Equal(12, win.RenderSize.Width);
        Assert.Equal(4, win.RenderSize.Height);
    }

    [Fact]
    public void DragLeftEdge_MinWidth_StopsPosition()
    {
        var host = CreateHost();
        var win = new Window { Width = 20, Height = 5, Left = 10, Top = 5, MinWidth = 15 };
        win.Show(host);

        // Drag left edge right past the minimum
        SendMouse(host, UIElement.MouseDownEvent, 10, 7);
        SendMouse(host, UIElement.MouseMoveEvent, 25, 7);
        SendMouse(host, UIElement.MouseUpEvent, 25, 7);

        Assert.Equal(15, win.RenderSize.Width);
        // Right edge stays at 10+20=30, so left = 30-15 = 15
        Assert.Equal(15, win.RenderSize.X);
    }

    [Fact]
    public void DragCorner_CanResizeFalse_DoesNothing()
    {
        var host = CreateHost();
        var win = new Window { Width = 20, Height = 5, Left = 10, Top = 5, CanResize = false };
        win.Show(host);

        SendMouse(host, UIElement.MouseDownEvent, 29, 9);
        SendMouse(host, UIElement.MouseMoveEvent, 34, 12);
        SendMouse(host, UIElement.MouseUpEvent, 34, 12);

        Assert.Equal(20, win.RenderSize.Width);
        Assert.Equal(5, win.RenderSize.Height);
    }

    [Fact]
    public void CloseButton_Click_ClosesAndRaisesClosed()
    {
        var host = CreateHost();
        var win = new Window { Width = 20, Height = 5, Left = 10, Top = 5 };
        bool closed = false;
        win.Closed += (s, e) => closed = true;
        win.Show(host);

        // Close button "[x]" occupies x = left+w-4 .. left+w-2 on the title row.
        SendMouse(host, UIElement.MouseDownEvent, 10 + 20 - 3, 5);

        Assert.True(closed);
        Assert.False(win.Visibility);
        Assert.Null(host.Overlay);
    }

    [Fact]
    public void CloseButton_Hidden_ClickMovesInstead()
    {
        var host = CreateHost();
        var win = new Window { Width = 20, Height = 5, Left = 10, Top = 5, ShowCloseButton = false };
        bool closed = false;
        win.Closed += (s, e) => closed = true;
        win.Show(host);

        SendMouse(host, UIElement.MouseDownEvent, 10 + 20 - 3, 5);
        SendMouse(host, UIElement.MouseUpEvent, 10 + 20 - 3, 5);

        Assert.False(closed);
        Assert.True(win.Visibility);
    }

    [Fact]
    public void Close_RemovesFromOverlayStack()
    {
        var host = CreateHost();
        var win = new Window { Width = 20, Height = 5 };
        win.Show(host);
        Assert.Equal(win, host.Overlay);

        win.Close();

        Assert.Null(host.Overlay);
        Assert.False(win.Visibility);
    }

    [Fact]
    public void Render_DrawsBorderTitleAndCloseButton()
    {
        var host = CreateHost(40, 12);
        var win = new Window { Width = 24, Height = 6, Left = 2, Top = 1, Title = "Hi" };
        win.Show(host);

        var buffer = new VirtualBuffer(40, 12);
        host.Render(buffer, 0, 0);

        var chars = BoxDrawingChars.Get(BoxStyle.Double);
        Assert.Equal(chars.TopLeft, buffer.GetPixel(2, 1).Character);
        Assert.Equal(chars.TopRight, buffer.GetPixel(2 + 24 - 1, 1).Character);
        Assert.Equal(chars.BottomLeft, buffer.GetPixel(2, 6).Character);
        Assert.Equal(chars.BottomRight, buffer.GetPixel(25, 6).Character);

        // Close button
        Assert.Equal('[', buffer.GetPixel(2 + 24 - 4, 1).Character);
        Assert.Equal('x', buffer.GetPixel(2 + 24 - 3, 1).Character);
        Assert.Equal(']', buffer.GetPixel(2 + 24 - 2, 1).Character);

        // Title appears somewhere on the top row
        bool foundTitle = false;
        for (int x = 3; x < 24; x++)
        {
            if (buffer.GetPixel(x, 1).Character == 'H' && buffer.GetPixel(x + 1, 1).Character == 'i')
            {
                foundTitle = true;
                break;
            }
        }
        Assert.True(foundTitle);
    }

    [Fact]
    public void ContentIsArrangedInsideBorder()
    {
        var host = CreateHost();
        var content = new TextBlock { Text = "hello" };
        var win = new Window { Width = 20, Height = 5, Left = 10, Top = 5, Content = content };
        win.Show(host);

        // Content sits inside the frame line (1) plus the default 1-char padding.
        Assert.Equal(2, content.RenderSize.X);
        Assert.Equal(2, content.RenderSize.Y);
        Assert.Equal(16, content.RenderSize.Width);
        Assert.Equal(1, content.RenderSize.Height);
    }

    [Fact]
    public void HostResize_KeepsPositionClamped()
    {
        var host = CreateHost();
        var win = new Window { Width = 20, Height = 5, Left = 55, Top = 18 };
        win.Show(host);
        Assert.Equal(55, win.RenderSize.X);

        // Shrink the host; the window must be pulled back inside.
        host.Measure(new Size(60, 20));
        host.Arrange(new Rect(0, 0, 60, 20));

        Assert.Equal(60 - 20, win.RenderSize.X);
        Assert.Equal(20 - 5, win.RenderSize.Y);
    }
}
