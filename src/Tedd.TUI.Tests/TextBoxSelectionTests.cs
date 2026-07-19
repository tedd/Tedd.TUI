using System;
using Xunit;
using Tedd.TUI;
using Tedd.TUI.Tests.TestInfrastructure;

namespace Tedd.TUI.Tests;

/// <summary>
/// Mouse drag selection and clipboard behavior. The tests drive the full window
/// pipeline (hit testing, capture, tunneling/bubbling) through
/// <see cref="ControlTestHost"/> — no direct handler calls.
/// </summary>
/// <remarks>
/// <see cref="Clipboard"/> is process-wide static state, so every test class touching
/// it shares the "ClipboardState" collection to opt out of xUnit's cross-class
/// parallelism. Each test resets the provider and buffer in the constructor.
/// </remarks>
[Collection("ClipboardState")]
public class TextBoxSelectionTests : IDisposable
{
    public TextBoxSelectionTests()
    {
        Clipboard.Provider = null;
        Clipboard.SetText(string.Empty);
    }

    public void Dispose()
    {
        Clipboard.Provider = null;
        Clipboard.SetText(string.Empty);
    }

    private sealed class FakeClipboard : IClipboard
    {
        public string? StoredText;

        public string? GetText() => StoredText;
        public void SetText(string text) => StoredText = text;
    }

    private static (ControlTestHost Host, TextBox TextBox) CreateHost(
        string text = "Hello World",
        int width = 15,
        bool isPassword = false)
    {
        var tb = new TextBox
        {
            Text = text,
            Width = width,
            IsPassword = isPassword,
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top
        };
        var panel = new StackPanel();
        panel.AddChild(tb);
        panel.AddChild(new TextBlock { Text = "below" });
        var host = new ControlTestHost(panel, 20, 4);
        return (host, tb);
    }

    [Fact]
    public void MouseDrag_Forward_SelectsText()
    {
        var (host, tb) = CreateHost();

        host.MouseDown(2, 0);
        host.MouseMove(7, 0);
        host.MouseUp(7, 0);

        Assert.True(tb.HasSelection);
        Assert.Equal(2, tb.SelectionStart);
        Assert.Equal(5, tb.SelectionLength);
        Assert.Equal("llo W", tb.SelectedText);
        Assert.Equal(7, tb.CaretIndex);
        Assert.True(tb.IsFocused);
    }

    [Fact]
    public void MouseDrag_Backward_SelectsText()
    {
        var (host, tb) = CreateHost();

        host.MouseDown(7, 0);
        host.MouseMove(2, 0);
        host.MouseUp(2, 0);

        Assert.Equal(2, tb.SelectionStart);
        Assert.Equal(5, tb.SelectionLength);
        Assert.Equal("llo W", tb.SelectedText);
        Assert.Equal(2, tb.CaretIndex);
    }

    [Fact]
    public void MouseDrag_PastTextEnd_ClampsToTextLength()
    {
        var (host, tb) = CreateHost(); // "Hello World" = 11 chars, width 15

        host.MouseDown(2, 0);
        host.MouseMove(14, 0);
        host.MouseUp(14, 0);

        Assert.Equal(2, tb.SelectionStart);
        Assert.Equal(9, tb.SelectionLength);
        Assert.Equal("llo World", tb.SelectedText);
    }

    [Fact]
    public void MouseDrag_OutsideControl_ContinuesViaCapture()
    {
        var (host, tb) = CreateHost();

        host.MouseDown(5, 0);
        Assert.Same(tb, host.Window.CapturedElement);

        // Drag below the control (onto the sibling row) and past the left edge:
        // capture keeps routing moves to the textbox, X clamps into the text.
        host.MouseMove(8, 2);
        Assert.Equal(8, tb.CaretIndex);

        host.MouseMove(-3, 1);
        Assert.Equal(0, tb.CaretIndex);
        Assert.Equal("Hello", tb.SelectedText);

        host.MouseUp(-3, 1);
        Assert.Null(host.Window.CapturedElement);
        Assert.Equal(0, tb.SelectionStart);
        Assert.Equal(5, tb.SelectionLength);
    }

    [Fact]
    public void Click_WithoutDrag_PlacesCaretWithoutSelection()
    {
        var (host, tb) = CreateHost();

        host.Click(3, 0);

        Assert.False(tb.HasSelection);
        Assert.Equal(string.Empty, tb.SelectedText);
        Assert.Equal(3, tb.CaretIndex);
    }

    [Fact]
    public void Click_AfterDragSelection_CollapsesSelection()
    {
        var (host, tb) = CreateHost();

        host.MouseDown(2, 0);
        host.MouseMove(7, 0);
        host.MouseUp(7, 0);
        Assert.True(tb.HasSelection);

        host.Click(4, 0);

        Assert.False(tb.HasSelection);
        Assert.Equal(4, tb.CaretIndex);
    }

    [Fact]
    public void MouseDrag_ScrolledText_MapsThroughScrollOffset()
    {
        // 26 chars in a 10-wide box; caret starts at the end (26), so the first
        // visible char is index 17. A press at x=2 must hit index 19, and the
        // following move maps through the re-scrolled viewport (start becomes 10).
        var (host, tb) = CreateHost(text: "ABCDEFGHIJKLMNOPQRSTUVWXYZ", width: 10);

        host.MouseDown(2, 0);
        Assert.Equal(19, tb.CaretIndex);

        host.MouseMove(5, 0);
        host.MouseUp(5, 0);

        Assert.Equal(15, tb.CaretIndex);
        Assert.Equal("PQRS", tb.SelectedText);
    }

    [Fact]
    public void Render_SelectionHighlighted()
    {
        var (host, tb) = CreateHost();

        host.MouseDown(2, 0);
        host.MouseMove(7, 0);
        host.MouseUp(7, 0);

        var buffer = host.Render();

        // Selected cells render inverted (cyan), the caret cell gray, the rest keeps
        // the focused background.
        for (int x = 2; x < 7; x++)
        {
            Assert.Equal(TuiColor.Cyan, buffer.GetPixel(x, 0).Background);
            Assert.Equal(TuiColor.Black, buffer.GetPixel(x, 0).Foreground);
        }
        Assert.Equal(TuiColor.Gray, buffer.GetPixel(7, 0).Background);
        Assert.Equal(TuiColor.DarkBlue, buffer.GetPixel(0, 0).Background);
        Assert.Equal(TuiColor.DarkBlue, buffer.GetPixel(8, 0).Background);
    }

    [Fact]
    public void CtrlC_CopiesSelectionToClipboard()
    {
        var fake = new FakeClipboard();
        Clipboard.Provider = fake;
        var (host, tb) = CreateHost();

        host.MouseDown(2, 0);
        host.MouseMove(7, 0);
        host.MouseUp(7, 0);
        host.PressKey(ConsoleKey.C, '\x03', ConsoleModifiers.Control);

        Assert.Equal("llo W", fake.StoredText);
        Assert.Equal("llo W", Clipboard.GetText());
        Assert.Equal("Hello World", tb.Text); // copy does not modify
        Assert.True(tb.HasSelection);         // ... nor deselect
    }

    [Fact]
    public void CtrlInsert_CopiesSelectionToClipboard()
    {
        var (host, tb) = CreateHost();

        host.MouseDown(0, 0);
        host.MouseMove(5, 0);
        host.MouseUp(5, 0);
        host.PressKey(ConsoleKey.Insert, '\0', ConsoleModifiers.Control);

        Assert.Equal("Hello", Clipboard.GetText());
    }

    [Fact]
    public void CtrlX_CutsSelection()
    {
        var (host, tb) = CreateHost();

        host.MouseDown(5, 0);
        host.MouseMove(11, 0);
        host.MouseUp(11, 0);
        host.PressKey(ConsoleKey.X, '\x18', ConsoleModifiers.Control);

        Assert.Equal(" World", Clipboard.GetText());
        Assert.Equal("Hello", tb.Text);
        Assert.False(tb.HasSelection);
        Assert.Equal(5, tb.CaretIndex);
    }

    [Fact]
    public void CtrlV_PastesReplacingSelection()
    {
        Clipboard.SetText("Brave New");
        var (host, tb) = CreateHost();

        host.MouseDown(0, 0);
        host.MouseMove(5, 0);
        host.MouseUp(5, 0);
        host.PressKey(ConsoleKey.V, '\x16', ConsoleModifiers.Control);

        Assert.Equal("Brave New World", tb.Text);
        Assert.False(tb.HasSelection);
        Assert.Equal(9, tb.CaretIndex);
    }

    [Fact]
    public void Paste_SanitizesMultiLineClipboardText()
    {
        Clipboard.SetText("one\r\ntwo\tthree\x01");
        var (host, tb) = CreateHost(text: "");

        host.Click(0, 0);
        host.PressKey(ConsoleKey.V, '\x16', ConsoleModifiers.Control);

        Assert.Equal("one two three", tb.Text);
    }

    [Fact]
    public void CtrlA_SelectsAll()
    {
        var (host, tb) = CreateHost();

        host.Click(3, 0);
        host.PressKey(ConsoleKey.A, '\x01', ConsoleModifiers.Control);

        Assert.Equal(0, tb.SelectionStart);
        Assert.Equal(11, tb.SelectionLength);
        Assert.Equal("Hello World", tb.SelectedText);
    }

    [Fact]
    public void Typing_ReplacesSelection()
    {
        var (host, tb) = CreateHost();

        host.MouseDown(0, 0);
        host.MouseMove(5, 0);
        host.MouseUp(5, 0);
        host.PressKey(ConsoleKey.Z, 'Z');

        Assert.Equal("Z World", tb.Text);
        Assert.False(tb.HasSelection);
        Assert.Equal(1, tb.CaretIndex);
    }

    [Fact]
    public void Backspace_DeletesSelection()
    {
        var (host, tb) = CreateHost();

        host.MouseDown(2, 0);
        host.MouseMove(7, 0);
        host.MouseUp(7, 0);
        host.PressKey(ConsoleKey.Backspace, '\b');

        Assert.Equal("Heorld", tb.Text);
        Assert.Equal(2, tb.CaretIndex);
        Assert.False(tb.HasSelection);
    }

    [Fact]
    public void Delete_DeletesSelection()
    {
        var (host, tb) = CreateHost();

        host.MouseDown(2, 0);
        host.MouseMove(7, 0);
        host.MouseUp(7, 0);
        host.PressKey(ConsoleKey.Delete);

        Assert.Equal("Heorld", tb.Text);
        Assert.Equal(2, tb.CaretIndex);
    }

    [Fact]
    public void ShiftArrows_ExtendSelection()
    {
        var (host, tb) = CreateHost();

        host.Click(2, 0);
        host.KeyDown(ConsoleKey.RightArrow, '\0', ConsoleModifiers.Shift);
        host.KeyDown(ConsoleKey.RightArrow, '\0', ConsoleModifiers.Shift);
        host.KeyDown(ConsoleKey.RightArrow, '\0', ConsoleModifiers.Shift);

        Assert.Equal("llo", tb.SelectedText);

        host.KeyDown(ConsoleKey.LeftArrow, '\0', ConsoleModifiers.Shift);
        Assert.Equal("ll", tb.SelectedText);

        // Plain arrow collapses the selection to its edge.
        host.KeyDown(ConsoleKey.LeftArrow);
        Assert.False(tb.HasSelection);
        Assert.Equal(2, tb.CaretIndex);
    }

    [Fact]
    public void PasswordBox_CopyAndCut_DoNothing()
    {
        var fake = new FakeClipboard();
        Clipboard.Provider = fake;
        var (host, tb) = CreateHost(text: "secret", isPassword: true);

        host.MouseDown(0, 0);
        host.MouseMove(6, 0);
        host.MouseUp(6, 0);
        Assert.Equal("secret", tb.SelectedText); // selection itself is allowed

        host.PressKey(ConsoleKey.C, '\x03', ConsoleModifiers.Control);
        Assert.Null(fake.StoredText);
        Assert.Equal(string.Empty, Clipboard.GetText());

        host.PressKey(ConsoleKey.X, '\x18', ConsoleModifiers.Control);
        Assert.Null(fake.StoredText);
        Assert.Equal("secret", tb.Text); // cut is fully suppressed
    }

    [Fact]
    public void DragBetweenTextBoxes_CaptureKeepsSelectionInPressedBox()
    {
        var first = new TextBox { Text = "Alpha", Width = 8, HorizontalAlignment = HorizontalAlignment.Left };
        var second = new TextBox { Text = "Omega", Width = 8, HorizontalAlignment = HorizontalAlignment.Left };
        var panel = new StackPanel();
        panel.AddChild(first);
        panel.AddChild(second);
        var host = new ControlTestHost(panel, 12, 4);

        // Press in the first box, drag down into the second box's row.
        host.MouseDown(1, 0);
        host.MouseMove(4, 1);
        host.MouseUp(4, 1);

        Assert.Equal("lph", first.SelectedText);
        Assert.False(second.HasSelection);
        Assert.True(first.IsFocused);
    }

    [Fact]
    public void ProgrammaticTextChange_ClearsSelection()
    {
        var (host, tb) = CreateHost();

        host.MouseDown(2, 0);
        host.MouseMove(7, 0);
        host.MouseUp(7, 0);
        Assert.True(tb.HasSelection);

        tb.Text = "Replaced";

        Assert.False(tb.HasSelection);
        Assert.Equal(8, tb.CaretIndex);
    }

    [Fact]
    public void SelectApi_ClampsAndSelects()
    {
        var (_, tb) = CreateHost();

        tb.Select(6, 100);
        Assert.Equal(6, tb.SelectionStart);
        Assert.Equal(5, tb.SelectionLength);
        Assert.Equal("World", tb.SelectedText);

        tb.ClearSelection();
        Assert.False(tb.HasSelection);

        tb.SelectAll();
        Assert.Equal("Hello World", tb.SelectedText);
    }
}
