using Xunit;
using Tedd.TUI;

namespace Tedd.TUI.Tests;

public class TuiWindowTests
{
    [Fact]
    public void TestWindowRender()
    {
        var window = new TuiWindow();
        var tb = new TextBlock { Text = "Window Content" };
        window.Content = tb;

        window.Measure(new Size(20, 1));
        window.Arrange(new Rect(0, 0, 20, 1));

        var buffer = new VirtualBuffer(20, 1);
        window.Render(buffer, 0, 0);

        Assert.Equal('W', buffer.GetPixel(0, 0).Character);
    }

    [Fact]
    public void TestWindowDataContextInheritance()
    {
        var window = new TuiWindow();
        var tb = new TextBlock();
        window.Content = tb;

        window.DataContext = "Test Context";

        Assert.Equal("Test Context", tb.DataContext);
    }
}
