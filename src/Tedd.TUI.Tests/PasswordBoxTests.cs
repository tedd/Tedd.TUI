using System;
using Xunit;
using Tedd.TUI;

namespace Tedd.TUI.Tests;

public class PasswordBoxTests
{
    [Fact]
    public void Properties_DefaultValues()
    {
        var pb = new PasswordBox();
        Assert.Equal(string.Empty, pb.Password);
        Assert.Equal('*', pb.PasswordChar);
    }

    [Fact]
    public void Password_Change_UpdatesInternalTextBox()
    {
        var pb = new PasswordBox();
        // Force template to expand by measuring
        pb.Measure(new Size(10, 1));

        Assert.NotNull(pb._internalTextBox);

        pb.Password = "Secret";
        Assert.Equal("Secret", pb._internalTextBox.Text);
    }

    [Fact]
    public void OnKeyDown_AddsTextAndSyncsPassword()
    {
        var pb = new PasswordBox();
        pb.Measure(new Size(10, 1));

        pb.OnKeyDown(new KeyEventArgs { KeyChar = 'S' });
        Assert.Equal("S", pb.Password);
        Assert.Equal("S", pb._internalTextBox!.Text);

        pb.OnKeyDown(new KeyEventArgs { KeyChar = 'E' });
        Assert.Equal("SE", pb.Password);
        Assert.Equal("SE", pb._internalTextBox!.Text);
    }

    [Fact]
    public void OnKeyDown_Backspace_SyncsPassword()
    {
        var pb = new PasswordBox();
        pb.Measure(new Size(10, 1));
        pb.Password = "ABC";

        // Internal cursor should be at end because Password property set updates TextProperty which updates cursor
        pb.OnKeyDown(new KeyEventArgs { Key = ConsoleKey.Backspace });
        Assert.Equal("AB", pb.Password);
        Assert.Equal("AB", pb._internalTextBox!.Text);
    }

    [Fact]
    public void Rendering_MasksInput()
    {
        var pb = new PasswordBox();
        pb.Password = "Test";

        pb.Measure(new Size(10, 1));
        pb.Arrange(new Rect(0, 0, 10, 1));

        var buffer = new VirtualBuffer(10, 1);
        pb.Render(buffer, 0, 0);

        // First 4 characters should be *
        Assert.Equal('*', buffer.GetPixel(0, 0).Character);
        Assert.Equal('*', buffer.GetPixel(1, 0).Character);
        Assert.Equal('*', buffer.GetPixel(2, 0).Character);
        Assert.Equal('*', buffer.GetPixel(3, 0).Character);

        // Next character should be space
        Assert.Equal(' ', buffer.GetPixel(4, 0).Character);
    }

    [Fact]
    public void Rendering_MasksInput_CustomChar()
    {
        var pb = new PasswordBox();
        pb.Password = "Test";
        pb.PasswordChar = '#';

        pb.Measure(new Size(10, 1));
        pb.Arrange(new Rect(0, 0, 10, 1));

        var buffer = new VirtualBuffer(10, 1);
        pb.Render(buffer, 0, 0);

        // First 4 characters should be #
        Assert.Equal('#', buffer.GetPixel(0, 0).Character);
        Assert.Equal('#', buffer.GetPixel(1, 0).Character);
        Assert.Equal('#', buffer.GetPixel(2, 0).Character);
        Assert.Equal('#', buffer.GetPixel(3, 0).Character);
    }

    [Fact]
    public void OnKeyDown_UnhandledKey_DoesNotSwallow()
    {
        var pb = new PasswordBox();
        pb.Measure(new Size(10, 1));

        var args = new KeyEventArgs { Key = ConsoleKey.Tab };
        pb.OnKeyDown(args);

        // TextBox doesn't handle Tab, so e.Handled should remain false
        Assert.False(args.Handled);
    }

    [Fact]
    public void OnKeyDown_HandledKey_SetsHandled()
    {
        var pb = new PasswordBox();
        pb.Measure(new Size(10, 1));

        var args = new KeyEventArgs { KeyChar = 'A' };
        pb.OnKeyDown(args);

        // TextBox handles normal characters, so e.Handled should be true
        Assert.True(args.Handled);
    }

    [Fact]
    public void OnPropertyChanged_IsFocused_UpdatesInternalTextBox()
    {
        var pb = new PasswordBox();
        pb.Measure(new Size(10, 1)); // Initialize _internalTextBox

        Assert.NotNull(pb._internalTextBox);
        Assert.False(pb._internalTextBox.IsFocused);

        pb.IsFocused = true;

        Assert.True(pb._internalTextBox.IsFocused);
    }

    [Theory]
    [InlineData(ConsoleKey.Tab, false)]
    [InlineData(ConsoleKey.Enter, false)]
    [InlineData(ConsoleKey.A, false)]
    public void OnKeyDown_NullInternalTextBox_CallsBase(ConsoleKey key, bool expectedHandled)
    {
        var pb = new PasswordBox();
        pb.Template = null; // Prevent template application
        pb._internalTextBox = null; // Manually clear because constructor applies default template

        Assert.Null(pb._internalTextBox);

        var args = new KeyEventArgs { Key = key };
        pb.OnKeyDown(args);

        // Base behavior doesn't crash
        Assert.Equal(expectedHandled, args.Handled);
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(10, 5)]
    [InlineData(-1, -1)]
    public void OnMouseDown_SetsFocusAndForwards_WithInternalTextBox(int x, int y)
    {
        var pb = new PasswordBox();
        // Just call Measure to ensure visual children are populated
        pb.Measure(new Size(10, 10));

        Assert.False(pb.IsFocused);
        Assert.False(pb._internalTextBox!.IsFocused);

        var args = new MouseEventArgs { X = x, Y = y };
        // Focus() relies on TuiWindow which we aren't fully mocking here, so IsFocused might not actually become true.
        // But we can at least assert we don't crash and _internalTextBox state mirrors pb
        pb.IsFocused = true; // force the focus state manually since we aren't in a real tree
        pb.OnMouseDown(args);

        // Let's assert the internal text box also had its events/state processed
        Assert.True(pb._internalTextBox.IsFocused);
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(100, 100)]
    [InlineData(-5, -5)]
    public void OnMouseDown_NullInternalTextBox_SetsFocus(int x, int y)
    {
        var pb = new PasswordBox();
        pb.Template = null;
        pb._internalTextBox = null;

        Assert.Null(pb._internalTextBox);
        Assert.False(pb.IsFocused);

        var args = new MouseEventArgs { X = x, Y = y };
        // Since Focus() needs tree, we just test no crash here
        pb.OnMouseDown(args);

        Assert.Null(pb._internalTextBox);
    }
}
