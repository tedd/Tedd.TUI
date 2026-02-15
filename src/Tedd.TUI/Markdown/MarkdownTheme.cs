using System;
using System.Text.Json.Serialization;
using Tedd.TUI.CodeColoring;
using Tedd.TUI; // For BoxStyle

namespace Tedd.TUI.Markdown;

public class MarkdownStyle
{
    public ConsoleColor? Foreground { get; set; }
    public ConsoleColor? Background { get; set; }
    public bool IsBold { get; set; }
    public bool IsUnderline { get; set; }

    public MarkdownStyle() { }

    public MarkdownStyle(ConsoleColor? foreground = null, ConsoleColor? background = null, bool isBold = false, bool isUnderline = false)
    {
        Foreground = foreground;
        Background = background;
        IsBold = isBold;
        IsUnderline = isUnderline;
    }
}

public class MarkdownTableStyle
{
    public bool ShowBorder { get; set; } = true;
    public bool ShowVerticalLines { get; set; } = true;
    public bool ShowHorizontalLines { get; set; } = true;
    public BoxStyle BorderStyle { get; set; } = BoxStyle.Heavy;
}

public class MarkdownTheme
{
    // Block Styles
    public MarkdownStyle Header1 { get; set; } = new MarkdownStyle(ConsoleColor.Magenta, null, true);
    public MarkdownStyle Header2 { get; set; } = new MarkdownStyle(ConsoleColor.Cyan, null, true);
    public MarkdownStyle Header3 { get; set; } = new MarkdownStyle(ConsoleColor.Yellow, null, true);
    public MarkdownStyle Header4 { get; set; } = new MarkdownStyle(ConsoleColor.White, null, true);
    public MarkdownStyle Header5 { get; set; } = new MarkdownStyle(ConsoleColor.Gray, null, true);
    public MarkdownStyle Header6 { get; set; } = new MarkdownStyle(ConsoleColor.DarkGray, null, true);

    public MarkdownStyle Paragraph { get; set; } = new MarkdownStyle(ConsoleColor.Gray);
    public MarkdownStyle Quote { get; set; } = new MarkdownStyle(ConsoleColor.DarkGray, null, false); // Usually indented with a bar
    public MarkdownStyle CodeBlock { get; set; } = new MarkdownStyle(ConsoleColor.Gray, ConsoleColor.DarkBlue); // Or distinct background

    // List Styles
    public MarkdownStyle List { get; set; } = new MarkdownStyle(ConsoleColor.White);
    public string BulletCharacter { get; set; } = "•"; // Unicode bullet

    // Table Style
    public MarkdownTableStyle Table { get; set; } = new MarkdownTableStyle();

    // Inline Styles
    public MarkdownStyle Link { get; set; } = new MarkdownStyle(ConsoleColor.Blue, null, false, true);
    public MarkdownStyle Image { get; set; } = new MarkdownStyle(ConsoleColor.Green);
    public MarkdownStyle Bold { get; set; } = new MarkdownStyle(ConsoleColor.White, null, true);
    public MarkdownStyle Italic { get; set; } = new MarkdownStyle(ConsoleColor.Gray, null, false); // Italic not supported in Console usually, maybe specific color?
    public MarkdownStyle CodeSpan { get; set; } = new MarkdownStyle(ConsoleColor.Yellow, ConsoleColor.DarkGray);

    // Code Syntax Highlighting Theme (for CodeDocument)
    public Tedd.TUI.CodeColoring.Theme CodeTheme { get; set; } = new Tedd.TUI.CodeColoring.Theme();

    public MarkdownTheme()
    {
    }
}
