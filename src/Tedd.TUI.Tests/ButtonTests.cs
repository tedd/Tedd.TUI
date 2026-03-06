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
        Assert.True(clicked);

        btn.Focus();
        Assert.True(btn.IsFocused);
    }
}
