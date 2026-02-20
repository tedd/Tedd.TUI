using System;
using Xunit;
using Tedd.TUI;

namespace Tedd.TUI.Tests;

public class ProgressBarTests
{
    [Fact]
    public void Properties_DefaultValues()
    {
        var pb = new ProgressBar();
        Assert.Equal(0, pb.Minimum);
        Assert.Equal(100, pb.Maximum);
        Assert.Equal(0, pb.Value);
        Assert.Equal(ProgressBarLabelMode.None, pb.LabelMode);
    }

    [Theory]
    [InlineData(0, 100, 50)] // 50%
    [InlineData(0, 10, 2)]   // 20%
    [InlineData(0, 100, 150)] // Clamped max
    [InlineData(0, 100, -10)]   // Clamped min
    public void Render_CalculatesFilledWidthCorrectly(int min, int max, int value)
    {
        var pb = new ProgressBar
        {
            Minimum = min,
            Maximum = max,
            Value = value,
            Width = 10
        };
        pb.Measure(new Size(10, 1));
        pb.Arrange(new Rect(0, 0, 10, 1));

        var buffer = new VirtualBuffer(10, 1);
        pb.Render(buffer, 0, 0);

        // expectedFilled = (value - min) * width / (max - min)
        int range = max - min;
        if (range <= 0) range = 1;
        int val = Math.Clamp(value - min, 0, range);
        int expectedFilled = (val * 10) / range;

        // Check filled pixels
        for (int i = 0; i < expectedFilled; i++)
        {
            Assert.Equal('█', buffer.GetPixel(i, 0).Character);
        }

        // Check empty pixels
        for (int i = expectedFilled; i < 10; i++)
        {
            Assert.Equal('░', buffer.GetPixel(i, 0).Character);
        }
    }

    [Fact]
    public void Render_WithPercentLabel()
    {
        var pb = new ProgressBar
        {
            Minimum = 0,
            Maximum = 100,
            Value = 50,
            Width = 10,
            LabelMode = ProgressBarLabelMode.Percent,
            LabelPercentDecimals = 0
        };
        pb.Measure(new Size(10, 1));
        pb.Arrange(new Rect(0, 0, 10, 1));

        var buffer = new VirtualBuffer(10, 1);
        pb.Render(buffer, 0, 0);

        // "50%" should be centered
        // Length 3. Center start = (10 - 3) / 2 = 3.
        // 0123456789
        // ███50%░░░░
        // Filled: 5 chars.
        // Text at 3, 4, 5.
        // 0-2: Block
        // 3,4: Text on Filled
        // 5: Text on Empty (wait, 5 is index 5. filled is 5, so index 0-4 are filled. 5 is empty)
        // 50% is '5','0','%' at 3,4,5.
        // 3 < 5 (filled) -> '5' on filled color
        // 4 < 5 (filled) -> '0' on filled color
        // 5 >= 5 (empty) -> '%' on empty color

        Assert.Equal('5', buffer.GetPixel(3, 0).Character);
        Assert.Equal('0', buffer.GetPixel(4, 0).Character);
        Assert.Equal('%', buffer.GetPixel(5, 0).Character);
    }
}
