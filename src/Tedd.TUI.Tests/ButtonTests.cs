using Xunit;
using Tedd.TUI;

namespace Tedd.TUI.Tests;

public class ButtonTests
{
    [Fact]
    public void TestButtonRender()
    {
        var btn = new Button { Content = "OK" };
        btn.Measure(new Size(100, 100));
        btn.Arrange(new Rect(0, 0, btn.DesiredSize.Width, btn.DesiredSize.Height));
        
        var buffer = new VirtualBuffer(btn.DesiredSize.Width, btn.DesiredSize.Height);
        btn.Render(buffer, 0, 0);
        
        // Button adds 4 chars padding + border.
        // "OK" length 2. Size should be 6x3.
        Assert.Equal(6, btn.DesiredSize.Width);
        Assert.Equal(3, btn.DesiredSize.Height);

        // Check Border
        Assert.Equal('┌', buffer.GetPixel(0, 0).Character);
        Assert.Equal('─', buffer.GetPixel(1, 0).Character);
        
        // Check Text Centered
        // 6 width. 012345
        // Text "OK" len 2. (6-2)/2 = 2.
        // x=2 -> 'O', x=3 -> 'K'
        Assert.Equal('O', buffer.GetPixel(2, 1).Character);
        Assert.Equal('K', buffer.GetPixel(3, 1).Character);
    }
}
