using System;
using Xunit;
using Tedd.TUI;

namespace Tedd.TUI.Tests;

public class CheckBoxTests
{
    [Fact]
    public void Properties_DefaultValues()
    {
        var cb = new CheckBox();
        Assert.Equal(false, cb.IsChecked);
        Assert.Null(cb.Content);
        Assert.True(cb.Focusable);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void IsChecked_Set_ReturnsValue(bool value)
    {
        var cb = new CheckBox();
        cb.IsChecked = value;
        Assert.Equal(value, cb.IsChecked);
    }

    [Theory]
    [InlineData("Test")]
    [InlineData("Longer Text")]
    [InlineData("")]
    public void Measure_CalculatesCorrectSize(string text)
    {
        var cb = new CheckBox { Content = text };
        cb.Measure(new Size(100, 100));
        // [x] Text -> 4 chars + text length
        Assert.Equal(4 + text.Length, cb.DesiredSize.Width);
        Assert.Equal(1, cb.DesiredSize.Height);
    }

    [Fact]
    public void Render_Unchecked()
    {
        var cb = new CheckBox { Content = "A", IsChecked = false };
        cb.Measure(new Size(10, 1));
        cb.Arrange(new Rect(0, 0, 5, 1));

        var buffer = new VirtualBuffer(5, 1);
        cb.Render(buffer, 0, 0);

        // Expected: "[ ] A"
        Assert.Equal('[', buffer.GetPixel(0, 0).Character);
        Assert.Equal(' ', buffer.GetPixel(1, 0).Character);
        Assert.Equal(']', buffer.GetPixel(2, 0).Character);
        Assert.Equal('A', buffer.GetPixel(4, 0).Character);
    }

    [Fact]
    public void Render_Checked()
    {
        var cb = new CheckBox { Content = "A", IsChecked = true };
        cb.Measure(new Size(10, 1));
        cb.Arrange(new Rect(0, 0, 5, 1));

        var buffer = new VirtualBuffer(5, 1);
        cb.Render(buffer, 0, 0);

        // Expected: "[√] A"
        Assert.Equal('[', buffer.GetPixel(0, 0).Character);
        Assert.Equal('√', buffer.GetPixel(1, 0).Character);
        Assert.Equal(']', buffer.GetPixel(2, 0).Character);
        Assert.Equal('A', buffer.GetPixel(4, 0).Character);
    }

    [Fact]
    public void OnMouseDown_TogglesState()
    {
        var cb = new CheckBox();
        Assert.Equal(false, cb.IsChecked);

        cb.OnMouseDown(new MouseEventArgs { X = 0, Y = 0 });
        cb.OnMouseUp(new MouseEventArgs { X = 0, Y = 0 });
        Assert.Equal(true, cb.IsChecked);

        cb.OnMouseDown(new MouseEventArgs { X = 0, Y = 0 });
        cb.OnMouseUp(new MouseEventArgs { X = 0, Y = 0 });
        Assert.Equal(false, cb.IsChecked);
    }

    [Theory]
    [InlineData(ConsoleKey.Spacebar)]
    [InlineData(ConsoleKey.Enter)]
    public void OnKeyDown_TogglesState(ConsoleKey key)
    {
        var cb = new CheckBox();
        Assert.Equal(false, cb.IsChecked);

        cb.OnKeyDown(new KeyEventArgs { Key = key });
        cb.OnKeyUp(new KeyEventArgs { Key = key });
        Assert.Equal(true, cb.IsChecked);

        cb.OnKeyDown(new KeyEventArgs { Key = key });
        cb.OnKeyUp(new KeyEventArgs { Key = key });
        Assert.Equal(false, cb.IsChecked);
    }

    [Fact]
    public void IsChecked_Changes_RaisesRoutedEvents()
    {
        var cb = new CheckBox();
        int checkedCount = 0;
        int uncheckedCount = 0;

        RoutedEventHandler checkedHandler = (s, e) => checkedCount++;
        RoutedEventHandler uncheckedHandler = (s, e) => uncheckedCount++;

        cb.Checked += checkedHandler;
        cb.Unchecked += uncheckedHandler;

        cb.IsChecked = true;
        Assert.Equal(1, checkedCount);
        Assert.Equal(0, uncheckedCount);

        cb.IsChecked = false;
        Assert.Equal(1, checkedCount);
        Assert.Equal(1, uncheckedCount);

        cb.Checked -= checkedHandler;
        cb.Unchecked -= uncheckedHandler;

        cb.IsChecked = true;
        Assert.Equal(1, checkedCount); // No change

        cb.IsChecked = false;
        Assert.Equal(1, uncheckedCount); // No change
    }

    [Fact]
    public void Properties_SetAndGet()
    {
        var cb = new CheckBox();

        cb.FocusedForeground = ConsoleColor.Red;
        Assert.Equal(ConsoleColor.Red, cb.FocusedForeground);

        cb.BracketColor = ConsoleColor.Blue;
        Assert.Equal(ConsoleColor.Blue, cb.BracketColor);

        cb.UncheckedChar = '-';
        Assert.Equal('-', cb.UncheckedChar);
    }

    [Fact]
    public void CheckedEvent_BubblesUpLogicalTree()
    {
        var panel = new StackPanel();
        var cb = new CheckBox();
        panel.AddChild(cb);

        bool panelCheckedRaised = false;
        panel.AddHandler(CheckBox.CheckedEvent, new RoutedEventHandler((s, e) => panelCheckedRaised = true));

        cb.IsChecked = true;
        Assert.True(panelCheckedRaised);
    }
}
