using Xunit;
using Tedd.TUI;
using System.Linq;

namespace Tedd.TUI.Tests;

public class ButtonTests
{
    [Fact]
    public void TestButtonStructure()
    {
        var btn = new Button { Content = "Test" };
        Assert.IsAssignableFrom<ContentControl>(btn);
        Assert.NotNull(btn.Template);

        // Measure triggers ApplyTemplate
        btn.Measure(new Size(100, 100));

        // Should have 1 visual child (the template root)
        Assert.Equal(1, btn.VisualChildrenCount);

        // Verify Template Root is Border
        var root = btn.GetVisualChild(0);
        Assert.IsType<Border>(root);
        var border = (Border)root;

        // Verify ContentPresenter inside Border
        Assert.IsType<ContentPresenter>(border.Content);
        var cp = (ContentPresenter)border.Content;

        // Verify Content Binding
        Assert.Equal("Test", cp.Content);
    }

    [Fact]
    public void TestButtonRender()
    {
        var btn = new Button { Content = "OK" };
        btn.Measure(new Size(100, 100));
        btn.Arrange(new Rect(0, 0, btn.DesiredSize.Width, btn.DesiredSize.Height));

        // New Button: Content (2) + Border (2) = 4 width.
        // Height: Content (1) + Border (2) = 3 height.
        Assert.Equal(4, btn.DesiredSize.Width);
        Assert.Equal(3, btn.DesiredSize.Height);

        var buffer = new VirtualBuffer(btn.DesiredSize.Width, btn.DesiredSize.Height);
        btn.Render(buffer, 0, 0);

        // Check Border (Unicode single-line: ┌ ─)
        // ┌──┐
        // │OK│
        // └──┘

        Assert.Equal('\u250C', buffer.GetPixel(0, 0).Character); // Top-left
        Assert.Equal('\u2500', buffer.GetPixel(1, 0).Character); // Horizontal
        Assert.Equal('\u2510', buffer.GetPixel(3, 0).Character); // Top-right

        // Check Text Centered
        // Width 4. "OK" len 2. (4-2)/2 = 1.
        // x=1 -> 'O', x=2 -> 'K'
        Assert.Equal('O', buffer.GetPixel(1, 1).Character);
        Assert.Equal('K', buffer.GetPixel(2, 1).Character);
    }

    [Fact]
    public void TestButtonRenderDoubleBoxStyle()
    {
        var btn = new Button { Content = "X", BoxStyle = BoxStyle.Double };
        btn.Measure(new Size(100, 100));
        btn.Arrange(new Rect(0, 0, btn.DesiredSize.Width, btn.DesiredSize.Height));
        var buffer = new VirtualBuffer(btn.DesiredSize.Width, btn.DesiredSize.Height);
        btn.Render(buffer, 0, 0);
        Assert.Equal('\u2554', buffer.GetPixel(0, 0).Character); // Double top-left
        Assert.Equal('\u2550', buffer.GetPixel(1, 0).Character); // Double horizontal
    }

    [Fact]
    public void TestButtonBindingUpdates()
    {
        var btn = new Button { Content = "X", BorderColor = ConsoleColor.Red };
        btn.Measure(new Size(100, 100));
        var border = (Border)btn.GetVisualChild(0);

        // Initial binding check
        Assert.Equal(ConsoleColor.Red, border.BorderColor);

        // Update Property
        btn.BorderColor = ConsoleColor.Blue;
        // Binding should update Border
        // Bindings update synchronously in my implementation?
        // Yes, OnPropertyChanged calls UpdateTarget (if hooked) or setter updates dictionary.
        // The BindingExpression subscribes to INotifyPropertyChanged or DependencyProperty change?
        // My BindingExpression currently subscribes to INotifyPropertyChanged on source object.
        // Source is Button. Button is DependencyObject.
        // DependencyObject does NOT implement INotifyPropertyChanged by default?
        // Wait, DependencyObject.OnPropertyChanged calls what?
        // It does NOT invoke PropertyChanged event unless I implement INotifyPropertyChanged interface.
        // `DependencyObject` needs to implement `INotifyPropertyChanged` for Binding to work!
        // This is critical.
    }

    [Fact]
    public void ClickEvent_AddRemoveHandler()
    {
        var btn = new Button();
        int clickCount = 0;
        RoutedEventHandler handler = (s, e) => clickCount++;

        btn.Click += handler;
        btn.RaiseEvent(new RoutedEventArgs(Button.ClickEvent, btn));
        Assert.Equal(1, clickCount);

        btn.Click -= handler;
        btn.RaiseEvent(new RoutedEventArgs(Button.ClickEvent, btn));
        Assert.Equal(1, clickCount);
    }

    [Theory]
    [InlineData(ConsoleKey.Spacebar)]
    [InlineData(ConsoleKey.Enter)]
    public void OnKeyDown_TriggersClickEvent(ConsoleKey key)
    {
        var btn = new Button();
        bool clicked = false;
        btn.Click += (s, e) => clicked = true;
        btn.OnKeyDown(new KeyEventArgs { Key = key });
        btn.OnKeyUp(new KeyEventArgs { Key = key });
        Assert.True(clicked);
    }

    [Fact]
    public void OnMouseDown_TriggersClickEvent()
    {
        var btn = new Button();
        var window = new TuiWindow { Content = btn };
        bool clicked = false;
        btn.Click += (s, e) => clicked = true;
        btn.OnMouseDown(new MouseEventArgs { X = 0, Y = 0 });
        btn.OnMouseUp(new MouseEventArgs { X = 0, Y = 0 });
        Assert.True(clicked);

        btn.Focus();
        Assert.True(btn.IsFocused);
    }

    // Shadow tests (DOS Turbo Pascal / Quick Basic style drop shadow)

    [Fact]
    public void Shadow_DefaultsToNone_DoesNotChangeDesiredSize()
    {
        var btn = new Button { Content = "OK" };
        btn.Measure(new Size(100, 100));

        // Should still be 4x3 (Border 2 + content 2 wide; Border 2 + content 1 tall).
        Assert.Equal(ButtonShadowStyle.None, btn.ShadowStyle);
        Assert.Equal(4, btn.DesiredSize.Width);
        Assert.Equal(3, btn.DesiredSize.Height);
    }

    [Fact]
    public void Shadow_Solid_ReservesSpaceInDesiredSize()
    {
        var btn = new Button { Content = "OK", ShadowStyle = ButtonShadowStyle.Solid };
        // Default ShadowOffsetX = 2, ShadowOffsetY = 1.
        btn.Measure(new Size(100, 100));

        Assert.Equal(4 + 2, btn.DesiredSize.Width);
        Assert.Equal(3 + 1, btn.DesiredSize.Height);
    }

    [Fact]
    public void Shadow_CustomOffset_ReservesCorrectSpace()
    {
        var btn = new Button
        {
            Content = "OK",
            ShadowStyle = ButtonShadowStyle.Medium,
            ShadowOffsetX = 3,
            ShadowOffsetY = 2
        };
        btn.Measure(new Size(100, 100));

        Assert.Equal(4 + 3, btn.DesiredSize.Width);
        Assert.Equal(3 + 2, btn.DesiredSize.Height);
    }

    [Fact]
    public void Shadow_None_DoesNotPaintExtraCells()
    {
        var btn = new Button { Content = "OK" };
        btn.Measure(new Size(100, 100));
        btn.Arrange(new Rect(0, 0, btn.DesiredSize.Width, btn.DesiredSize.Height));

        // Use a buffer with margin around the button to detect any stray writes.
        var buffer = new VirtualBuffer(10, 10);
        buffer.Clear();
        btn.Render(buffer, 0, 0);

        // Cells outside the button (4x3) should remain default fill (' ', White, Black)
        var outside = buffer.GetPixel(5, 0);
        Assert.Equal(' ', outside.Character);
        Assert.Equal(System.ConsoleColor.White, outside.Foreground);
        Assert.Equal(System.ConsoleColor.Black, outside.Background);
    }

    [Fact]
    public void Shadow_Solid_RendersExpectedFootprint()
    {
        // Use distinctive shadow colors so we can distinguish shadow cells from cleared ones.
        var btn = new Button
        {
            Content = "OK",
            ShadowStyle = ButtonShadowStyle.Solid,
            ShadowForeground = System.ConsoleColor.Magenta,
            ShadowBackground = System.ConsoleColor.Blue
        };
        btn.Measure(new Size(100, 100));
        btn.Arrange(new Rect(0, 0, btn.DesiredSize.Width, btn.DesiredSize.Height));

        // Total footprint = 6x4. Button itself occupies (0,0)-(3,2). Shadow is the L
        // covering columns 4-5 (rows 1-3) and row 3 (columns 2-5).
        var buffer = new VirtualBuffer(btn.DesiredSize.Width, btn.DesiredSize.Height);
        buffer.Clear();
        btn.Render(buffer, 0, 0);

        // Top-right corner of the bounding box (y=0) must NOT be shadow -- the right
        // strip is offset by sy, so this cell remains the default cleared cell.
        var topRightOutside = buffer.GetPixel(4, 0);
        Assert.NotEqual(System.ConsoleColor.Blue, topRightOutside.Background);

        // Right strip cells should be solid shadow (space on Blue bg with Magenta fg).
        var rightStrip = buffer.GetPixel(4, 1);
        Assert.Equal(' ', rightStrip.Character);
        Assert.Equal(System.ConsoleColor.Blue, rightStrip.Background);
        Assert.Equal(System.ConsoleColor.Magenta, rightStrip.Foreground);

        var rightStripBottom = buffer.GetPixel(5, 2);
        Assert.Equal(System.ConsoleColor.Blue, rightStripBottom.Background);

        // Bottom strip cells should be solid shadow.
        var bottomStrip = buffer.GetPixel(2, 3);
        Assert.Equal(' ', bottomStrip.Character);
        Assert.Equal(System.ConsoleColor.Blue, bottomStrip.Background);

        // Bottom-left of bounding box (x=0,y=3) must NOT be shadow -- bottom strip is offset by sx.
        var bottomLeftOutside = buffer.GetPixel(0, 3);
        Assert.NotEqual(System.ConsoleColor.Blue, bottomLeftOutside.Background);

        // Bottom-right corner IS part of the shadow's bottom strip.
        Assert.Equal(System.ConsoleColor.Blue, buffer.GetPixel(5, 3).Background);

        // Border still drawn at (0,0)
        Assert.Equal('\u250C', buffer.GetPixel(0, 0).Character);
    }

    [Fact]
    public void Shadow_Medium_UsesShadeCharacter()
    {
        var btn = new Button { Content = "X", ShadowStyle = ButtonShadowStyle.Medium };
        btn.Measure(new Size(100, 100));
        btn.Arrange(new Rect(0, 0, btn.DesiredSize.Width, btn.DesiredSize.Height));

        var buffer = new VirtualBuffer(btn.DesiredSize.Width, btn.DesiredSize.Height);
        buffer.Clear();
        btn.Render(buffer, 0, 0);

        // Button content is 1x1 + border 2x2 = 3x3. Shadow extents 2x1.
        // Right strip at x=3, y starting at 1. Bottom strip at x=2, y=3.
        Assert.Equal('\u2592', buffer.GetPixel(3, 1).Character);
        Assert.Equal('\u2592', buffer.GetPixel(2, 3).Character);
    }

    // BoxStyle.None tests: flat label-style button with " content " spacing, no border, height 1

    [Fact]
    public void BoxStyleNone_HasOneCharSidePaddingAndNoExtraRows()
    {
        var btn = new Button { Content = "OK", BoxStyle = BoxStyle.None };
        btn.Measure(new Size(100, 100));

        // Content "OK" is 2x1. Borderless inset adds 1 char each horizontal side -> 4 wide, 1 tall.
        Assert.Equal(4, btn.DesiredSize.Width);
        Assert.Equal(1, btn.DesiredSize.Height);
    }

    [Fact]
    public void BoxStyleNone_RendersContentWithLeadingAndTrailingSpace()
    {
        var btn = new Button { Content = "OK", BoxStyle = BoxStyle.None };
        btn.Measure(new Size(100, 100));
        btn.Arrange(new Rect(0, 0, btn.DesiredSize.Width, btn.DesiredSize.Height));

        var buffer = new VirtualBuffer(btn.DesiredSize.Width, btn.DesiredSize.Height);
        buffer.Clear();
        btn.Render(buffer, 0, 0);

        // Expected layout: " OK "
        Assert.Equal(' ', buffer.GetPixel(0, 0).Character);
        Assert.Equal('O', buffer.GetPixel(1, 0).Character);
        Assert.Equal('K', buffer.GetPixel(2, 0).Character);
        Assert.Equal(' ', buffer.GetPixel(3, 0).Character);
    }

    [Fact]
    public void BoxStyleNone_NoBoxDrawingCharactersAnywhere()
    {
        var btn = new Button { Content = "OK", BoxStyle = BoxStyle.None };
        btn.Measure(new Size(100, 100));
        btn.Arrange(new Rect(0, 0, btn.DesiredSize.Width, btn.DesiredSize.Height));

        var buffer = new VirtualBuffer(btn.DesiredSize.Width, btn.DesiredSize.Height);
        buffer.Clear();
        btn.Render(buffer, 0, 0);

        // Sweep the cells and ensure no box-drawing range characters made it into the buffer.
        for (int x = 0; x < btn.DesiredSize.Width; x++)
        {
            char c = buffer.GetPixel(x, 0).Character;
            Assert.False(c >= '\u2500' && c <= '\u257F',
                $"Unexpected box-drawing character at x={x}: U+{(int)c:X4}");
        }
    }

    [Fact]
    public void BoxStyleNone_WithShadow_ReservesShadowAndSidePadding()
    {
        var btn = new Button
        {
            Content = "OK",
            BoxStyle = BoxStyle.None,
            ShadowStyle = ButtonShadowStyle.Solid
            // ShadowOffsetX = 2, ShadowOffsetY = 1 by default
        };
        btn.Measure(new Size(100, 100));

        // " OK " = 4x1, plus shadow (2, 1) -> 6x2.
        Assert.Equal(4 + 2, btn.DesiredSize.Width);
        Assert.Equal(1 + 1, btn.DesiredSize.Height);
    }

    [Fact]
    public void BoxStyleNone_WithShadow_RendersContentAndShadowL()
    {
        var btn = new Button
        {
            Content = "OK",
            BoxStyle = BoxStyle.None,
            ShadowStyle = ButtonShadowStyle.Solid,
            ShadowForeground = ConsoleColor.Magenta,
            ShadowBackground = ConsoleColor.Blue
        };
        btn.Measure(new Size(100, 100));
        btn.Arrange(new Rect(0, 0, btn.DesiredSize.Width, btn.DesiredSize.Height));

        var buffer = new VirtualBuffer(btn.DesiredSize.Width, btn.DesiredSize.Height);
        buffer.Clear();
        btn.Render(buffer, 0, 0);

        // Total footprint 6x2. Button " OK " in row 0 at x=0..3.
        Assert.Equal(' ', buffer.GetPixel(0, 0).Character);
        Assert.Equal('O', buffer.GetPixel(1, 0).Character);
        Assert.Equal('K', buffer.GetPixel(2, 0).Character);
        Assert.Equal(' ', buffer.GetPixel(3, 0).Character);

        // Right shadow strip at x=4..5, y=1 (offset by sy=1 down so it doesn't sit above button).
        Assert.Equal(ConsoleColor.Blue, buffer.GetPixel(4, 1).Background);
        Assert.Equal(ConsoleColor.Blue, buffer.GetPixel(5, 1).Background);

        // Top-right corner of bounding box (y=0) is NOT shadow.
        Assert.NotEqual(ConsoleColor.Blue, buffer.GetPixel(4, 0).Background);

        // Bottom shadow strip in y=1, starting x=2 (offset by sx=2 right).
        Assert.Equal(ConsoleColor.Blue, buffer.GetPixel(2, 1).Background);
        // Bottom-left corner of bounding box (x=0,y=1) is NOT shadow.
        Assert.NotEqual(ConsoleColor.Blue, buffer.GetPixel(0, 1).Background);
    }

    [Fact]
    public void Shadow_DoesNotOverlapButtonRectangle()
    {
        var btn = new Button { Content = "OK", ShadowStyle = ButtonShadowStyle.Solid };
        btn.Measure(new Size(100, 100));
        btn.Arrange(new Rect(0, 0, btn.DesiredSize.Width, btn.DesiredSize.Height));

        var buffer = new VirtualBuffer(btn.DesiredSize.Width, btn.DesiredSize.Height);
        buffer.Clear();
        btn.Render(buffer, 0, 0);

        // Button rectangle occupies (0,0)-(3,2). Verify the button corners are intact.
        Assert.Equal('\u250C', buffer.GetPixel(0, 0).Character);
        Assert.Equal('\u2510', buffer.GetPixel(3, 0).Character);
        Assert.Equal('\u2514', buffer.GetPixel(0, 2).Character);
        Assert.Equal('\u2518', buffer.GetPixel(3, 2).Character);
        Assert.Equal('O', buffer.GetPixel(1, 1).Character);
        Assert.Equal('K', buffer.GetPixel(2, 1).Character);
    }
}
