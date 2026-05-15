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

/// <summary>
/// Theme settings applied to <see cref="Image"/> controls produced by the markdown parser.
/// Extends the basic <see cref="MarkdownStyle"/> with size caps, render-mode preference,
/// and an optional <see cref="IAsciiArtRenderer"/> override so themes can swap the ASCII
/// algorithm without touching the rest of the parser.
/// </summary>
public class MarkdownImageStyle : MarkdownStyle
{
    /// <summary>Maximum width in character cells (0 = unconstrained).</summary>
    public int MaxCellWidth { get; set; } = 0;

    /// <summary>Maximum height in character cells (0 = unconstrained).</summary>
    public int MaxCellHeight { get; set; } = 0;

    /// <summary>Which render path the image should pick. Defaults to <see cref="ImageRenderMode.Auto"/>.</summary>
    public ImageRenderMode RenderMode { get; set; } = ImageRenderMode.Auto;

    /// <summary>
    /// Optional ASCII renderer override. When null, the global default
    /// (<see cref="Image.DefaultAsciiRenderer"/>) is used.
    /// </summary>
    public IAsciiArtRenderer? AsciiRenderer { get; set; }

    public MarkdownImageStyle() : base() { }

    public MarkdownImageStyle(ConsoleColor? foreground)
        : base(foreground)
    {
    }
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
    public MarkdownImageStyle Image { get; set; } = new MarkdownImageStyle(ConsoleColor.Green);
    public MarkdownStyle Bold { get; set; } = new MarkdownStyle(ConsoleColor.White, null, true);
    public MarkdownStyle Italic { get; set; } = new MarkdownStyle(ConsoleColor.Gray, null, false); // Italic not supported in Console usually, maybe specific color?
    public MarkdownStyle CodeSpan { get; set; } = new MarkdownStyle(ConsoleColor.Yellow, ConsoleColor.DarkGray);

    // Code Syntax Highlighting Theme (for CodeDocument)
    public Tedd.TUI.CodeColoring.Theme CodeTheme { get; set; } = new Tedd.TUI.CodeColoring.Theme();

    public MarkdownTheme()
    {
    }
}
