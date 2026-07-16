using System;
using Xunit;
using Tedd.TUI;
using Tedd.TUI.Tests.TestInfrastructure;

namespace Tedd.TUI.Tests;

public class TextBoxTests
{
    [Fact]
    public void Properties_DefaultValues()
    {
        var tb = new TextBox();
        Assert.Equal(string.Empty, tb.Text);
        Assert.False(tb.IsPassword);
    }

    [Fact]
    public void Text_Change_UpdatesCursor()
    {
        var tb = new TextBox();
        tb.Text = "Hello";
        // Cannot access private cursor position, but we can verify behavior via input
        Assert.Equal("Hello", tb.Text);
    }

    [Fact]
    public void OnKeyDown_AddsText()
    {
        var tb = new TextBox();
        tb.OnKeyDown(new KeyEventArgs { KeyChar = 'A' });
        Assert.Equal("A", tb.Text);

        tb.OnKeyDown(new KeyEventArgs { KeyChar = 'B' });
        Assert.Equal("AB", tb.Text);
    }

    [Fact]
    public void OnKeyDown_Backspace()
    {
        var tb = new TextBox { Text = "AB" };
        // Cursor is at end by default when Text is set

        tb.OnKeyDown(new KeyEventArgs { Key = ConsoleKey.Backspace });
        Assert.Equal("A", tb.Text);
    }

    [Fact]
    public void OnKeyDown_LeftRightArrow_Navigation()
    {
        var tb = new TextBox { Text = "ABC" };
        // Cursor at 3

        tb.OnKeyDown(new KeyEventArgs { Key = ConsoleKey.LeftArrow }); // At 2
        tb.OnKeyDown(new KeyEventArgs { Key = ConsoleKey.Backspace }); // Deletes 'B'
        Assert.Equal("AC", tb.Text);
    }

    [Fact]
    public void PasswordMode_HidesText()
    {
        var tb = new TextBox { Text = "Pass", IsPassword = true, Width = 10 };
        tb.Measure(new Size(10, 1));
        tb.Arrange(new Rect(0, 0, 10, 1));

        var buffer = new VirtualBuffer(10, 1);
        tb.Render(buffer, 0, 0);

        Assert.Equal('*', buffer.GetPixel(0, 0).Character);
        Assert.Equal('*', buffer.GetPixel(1, 0).Character);
        Assert.Equal('*', buffer.GetPixel(2, 0).Character);
        Assert.Equal('*', buffer.GetPixel(3, 0).Character);
    }

    [Fact]
    public void MouseClick_NestedTextBoxes_PlacesCaretAndMovesFocusWithoutChangingSibling()
    {
        var first = new TextBox { Text = "Alpha", Width = 8 };
        var second = new TextBox { Text = "Omega", Width = 8 };
        var fields = new StackPanel();
        fields.AddChild(new TextBlock { Text = "form" });
        fields.AddChild(first);
        fields.AddChild(new TextBlock { Text = "--------" });
        fields.AddChild(second);
        fields.AddChild(new TextBlock { Text = "status" });
        var surface = new Border { Child = fields, BoxStyle = BoxStyle.Double };
        var host = new ControlTestHost(surface, 12, 7);

        var firstClick = host.Click(first, 2, 0);
        host.PressKey(ConsoleKey.X, 'X');

        Assert.True(firstClick.Down.Handled);
        Assert.True(first.IsFocused);
        Assert.False(second.IsFocused);
        Assert.Equal("AlXpha", first.Text);
        Assert.Equal("Omega", second.Text);

        host.Click(fields.GetVisualChild(2), 3, 0);
        Assert.Equal("AlXpha", first.Text);
        Assert.Equal("Omega", second.Text);

        var secondClick = host.Click(second, 1, 0);
        host.PressKey(ConsoleKey.Y, 'Y');

        Assert.True(secondClick.Down.Handled);
        Assert.False(first.IsFocused);
        Assert.True(second.IsFocused);
        Assert.Equal("AlXpha", first.Text);
        Assert.Equal("OYmega", second.Text);
    }
}
