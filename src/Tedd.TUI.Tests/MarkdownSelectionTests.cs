using System;
using Xunit;
using Tedd.TUI;
using Tedd.TUI.Markdown;
using Tedd.TUI.Media;

namespace Tedd.TUI.Tests;

/// <summary>
/// Covers mouse-drag text selection and Ctrl+C copy on <see cref="MarkdownView"/>.
/// </summary>
public class MarkdownSelectionTests
{
    private static MarkdownView Layout(string text, int w = 40, int h = 12)
    {
        var md = new MarkdownView { Text = text };
        md.Measure(new Size(w, h));
        md.Arrange(new Rect(0, 0, w, h));
        return md;
    }

    private static void Drag(MarkdownView md, int x0, int y0, int x1, int y1)
    {
        md.OnMouseDown(new MouseEventArgs(UIElement.MouseDownEvent) { X = x0, Y = y0 });
        md.OnMouseMove(new MouseEventArgs(UIElement.MouseMoveEvent) { X = x1, Y = y1 });
        md.OnMouseUp(new MouseEventArgs(UIElement.MouseUpEvent) { X = x1, Y = y1 });
    }

    [Fact]
    public void Drag_Selects_Prose_On_A_Single_Line()
    {
        var md = Layout("Hello selectable world.");
        // Columns 0..4 inclusive -> "Hello".
        Drag(md, 0, 0, 4, 0);
        Assert.Equal("Hello", md.SelectedText);
    }

    [Fact]
    public void Drag_Selects_Across_Multiple_Lines()
    {
        var md = Layout("Hello selectable world.\n\nSecond line here.");
        Drag(md, 6, 0, 4, 2);
        Assert.Equal("selectable world.\n\nSecon", md.SelectedText);
    }

    [Fact]
    public void Reversed_Drag_Yields_Same_Selection()
    {
        var md = Layout("Hello selectable world.");
        Drag(md, 4, 0, 0, 0);
        Assert.Equal("Hello", md.SelectedText);
    }

    [Fact]
    public void Click_Without_Drag_Selects_Nothing()
    {
        var md = Layout("Hello selectable world.");
        Drag(md, 3, 0, 3, 0);
        Assert.Equal(string.Empty, md.SelectedText);
    }

    [Fact]
    public void CopySelection_Places_Text_On_Clipboard()
    {
        var md = Layout("Hello selectable world.");
        Drag(md, 0, 0, 4, 0);
        md.CopySelection();
        Assert.Equal("Hello", Clipboard.GetText());
    }

    [Fact]
    public void CtrlC_Copies_Selection()
    {
        Clipboard.SetText("stale");
        var md = Layout("Hello selectable world.");
        Drag(md, 0, 0, 4, 0);
        md.OnKeyDown(new KeyEventArgs(UIElement.KeyDownEvent)
        {
            Key = ConsoleKey.C,
            Modifiers = ConsoleModifiers.Control
        });
        Assert.Equal("Hello", Clipboard.GetText());
    }

    [Fact]
    public void Selection_Excludes_Code_Block_Content()
    {
        // Dragging across a fenced code block must not pull in the highlighted code text;
        // code blocks carry their own copy button and are skipped by prose selection.
        var md = Layout("intro\n\n```csharp\nsecret_code_token\n```\n\noutro", 40, 20);
        Drag(md, 0, 0, 39, 19);
        Assert.DoesNotContain("secret_code_token", md.SelectedText);
        Assert.Contains("intro", md.SelectedText);
        Assert.Contains("outro", md.SelectedText);
    }

    [Fact]
    public void Refresh_Clears_Selection()
    {
        var md = Layout("Hello selectable world.");
        Drag(md, 0, 0, 4, 0);
        Assert.NotEqual(string.Empty, md.SelectedText);

        md.Text = "Different text now.";
        Assert.Equal(string.Empty, md.SelectedText);
    }

    [Fact]
    public void Selection_Highlight_Paints_Selection_Background()
    {
        var md = Layout("Hello selectable world.");
        Drag(md, 0, 0, 4, 0);

        var buf = new VirtualBuffer(40, 12);
        md.Render(buf, 0, 0);

        // Cells 0..4 on row 0 should carry the selection background.
        for (int x = 0; x <= 4; x++)
            Assert.Equal(TuiColor.DarkCyan, buf.GetPixel(x, 0).Background);
        // Cell just past the selection should not.
        Assert.NotEqual(TuiColor.DarkCyan, buf.GetPixel(5, 0).Background);
    }
}
