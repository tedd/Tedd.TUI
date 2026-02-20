using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Linq;
using Tedd.TUI;
using Tedd.TUI.CodeColoring;

namespace Tedd.TUI.Markdown;

public class MarkdownParser
{
    private readonly MarkdownTheme _theme;

    public MarkdownParser(MarkdownTheme theme)
    {
        _theme = theme;
    }

    public FlowDocument Parse(string markdown)
    {
        var doc = new FlowDocument();
        if (string.IsNullOrEmpty(markdown)) return doc;

        var lines = markdown.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None).ToList();
        var blocks = ParseBlocks(lines);

        foreach (var block in blocks)
        {
            var element = RenderBlock(block);
            if (element != null)
            {
                doc.AddChild(element);
            }
        }

        return doc;
    }

    private UIElement? RenderBlock(Block block)
    {
        if (block is HeaderBlock header)
        {
            var p = new Paragraph();
            // Apply header style
            var style = GetHeaderStyle(header.Level);
            // Parse inline content
            AddInlineContent(p, header.Text, style);
            return p;
        }
        else if (block is ParagraphBlock para)
        {
            var p = new Paragraph();
            AddInlineContent(p, para.Text.ToString(), _theme.Paragraph);
            return p;
        }
        else if (block is ListBlock list)
        {
            var stack = new StackPanel { Orientation = Orientation.Vertical };
            foreach (var item in list.Items)
            {
                var itemPanel = new Paragraph(); // Use Paragraph for wrapping list item text
                // Bullet
                var bullet = new TextBlock
                {
                    Text = _theme.BulletCharacter + " ",
                    Foreground = _theme.List.Foreground ?? ConsoleColor.White
                };
                itemPanel.AddChild(bullet);

                // Content
                AddInlineContent(itemPanel, item, _theme.List);
                stack.AddChild(itemPanel);
            }
            return stack;
        }
        else if (block is CodeBlock code)
        {
            var cd = new CodeDocument();
            cd.Theme = _theme.CodeTheme;
            cd.SetCode(code.Code, code.Language);

            // Wrap in a Border? Theme has CodeBlock style (colors).
            // CodeDocument doesn't support Border/Padding natively yet (it inherits StackPanel).
            // We can wrap it in a border if we had one.
            // Or just set background? CodeDocument constructs lines.
            // Let's return cd for now.
            return cd;
        }
        else if (block is QuoteBlock quote)
        {
            var p = new Paragraph();
            // Add quote marker
            var marker = new TextBlock { Text = "│ ", Foreground = _theme.Quote.Foreground ?? ConsoleColor.DarkGray };
            p.AddChild(marker);

            AddInlineContent(p, quote.Text.ToString(), _theme.Quote);
            return p;
        }
        else if (block is TableBlock tableBlock)
        {
            var table = new Table();
            table.ShowHeader = true;
            table.HeaderForeground = _theme.Header4.Foreground ?? ConsoleColor.White;

            table.ShowBorder = _theme.Table.ShowBorder;
            table.ShowVerticalLines = _theme.Table.ShowVerticalLines;
            table.ShowHorizontalLines = _theme.Table.ShowHorizontalLines;
            table.BorderStyle = _theme.Table.BorderStyle;

            // Define Columns
            if (tableBlock.Headers != null)
            {
                foreach (var h in tableBlock.Headers)
                {
                    table.Columns.Add(new TableColumn { Header = h, Width = GridLength.Auto });
                }
            }

            // Rows
            foreach (var rowData in tableBlock.Rows)
            {
                var row = new TableRow();
                foreach (var cellText in rowData)
                {
                    // Parse cell content (Inline)
                    // We need to render Inline to a UIElement.
                    // Table cell expects UIElement.
                    // We can use a Paragraph (wrapping) or TextBlock?
                    // Usually cells don't wrap in simple tables, or they do?
                    // Let's use Paragraph for cell content to support links etc.
                    var cellP = new Paragraph();
                    AddInlineContent(cellP, cellText, _theme.Paragraph);
                    row.AddCell(cellP);
                }
                table.AddRow(row);
            }

            // Return table wrapped to ensure layout?
            // Table is UIElement.
            return table;
        }

        return null;
    }

    private MarkdownStyle GetHeaderStyle(int level)
    {
        return level switch
        {
            1 => _theme.Header1,
            2 => _theme.Header2,
            3 => _theme.Header3,
            4 => _theme.Header4,
            5 => _theme.Header5,
            _ => _theme.Header6
        };
    }

    private void AddInlineContent(Paragraph p, string text, MarkdownStyle baseStyle)
    {
        // Tokenize text into UI Elements
        var tokens = ParseInline(text);
        foreach (var token in tokens)
        {
            if (token is TextToken tt)
            {
                // We need to split text into words for Paragraph wrapping
                // Or let Paragraph handle it?
                // My Paragraph implementation takes UIElement children and measures them.
                // If I add one big TextBlock, it won't wrap.
                // So I MUST split by space here.

                var words = tt.Text.Split(' ');
                for (int i = 0; i < words.Length; i++)
                {
                    var word = words[i];
                    if (string.IsNullOrEmpty(word) && i < words.Length - 1)
                    {
                        // Multiple spaces? Or split caused empty entry.
                        // Render space.
                    }

                    // Apply style mixing?
                    // tt.Style (from inline) vs baseStyle (from block)
                    // Inline wins.
                    var fg = tt.Style?.Foreground ?? baseStyle.Foreground ?? ConsoleColor.Gray;
                    var bg = tt.Style?.Background ?? baseStyle.Background;

                    var tb = new TextBlock
                    {
                        Text = word + (i < words.Length - 1 ? " " : ""), // Add space back except last
                        Foreground = fg,
                        Background = bg
                    };
                    p.AddChild(tb);
                }
            }
            else if (token is LinkToken lt)
            {
                var link = new Hyperlink
                {
                    Text = lt.Text,
                    Url = lt.Url,
                    Foreground = _theme.Link.Foreground ?? ConsoleColor.Blue
                };
                // Ensure space handling? Links usually distinctive.
                p.AddChild(link);
            }
            else if (token is ImageToken it)
            {
                var img = new Image
                {
                    AltText = it.AltText,
                    Source = it.Url,
                    Foreground = _theme.Image.Foreground ?? ConsoleColor.Green
                };
                p.AddChild(img);
            }
        }
    }

    // --- Block Parsing ---

    private abstract class Block { }
    private class HeaderBlock : Block { public int Level; public string Text; }
    private class ParagraphBlock : Block { public StringBuilder Text = new StringBuilder(); }
    private class ListBlock : Block { public List<string> Items = new List<string>(); }
    private class CodeBlock : Block { public string Code; public string Language; }
    private class QuoteBlock : Block { public StringBuilder Text = new StringBuilder(); }
    private class TableBlock : Block { public List<string> Headers; public List<List<string>> Rows = new List<List<string>>(); }

    private List<Block> ParseBlocks(List<string> lines)
    {
        var blocks = new List<Block>();
        Block? currentBlock = null;

        for (int i = 0; i < lines.Count; i++)
        {
            string line = lines[i];
            string trimmed = line.Trim();

            if (string.IsNullOrEmpty(trimmed))
            {
                if (currentBlock != null)
                {
                    blocks.Add(currentBlock);
                    currentBlock = null;
                }
                continue;
            }

            // Code Block (Fence)
            if (trimmed.StartsWith("```") || trimmed.StartsWith("~~~"))
            {
                if (currentBlock != null) { blocks.Add(currentBlock); currentBlock = null; }

                var lang = trimmed.Trim('`', '~').Trim();
                var codeLines = new List<string>();
                i++; // Skip fence
                while (i < lines.Count)
                {
                    if (lines[i].Trim().StartsWith("```") || lines[i].Trim().StartsWith("~~~"))
                        break;
                    codeLines.Add(lines[i]);
                    i++;
                }
                blocks.Add(new CodeBlock { Language = lang, Code = string.Join("\n", codeLines) });
                continue;
            }

            // Header
            if (trimmed.StartsWith("#"))
            {
                if (currentBlock != null) { blocks.Add(currentBlock); currentBlock = null; }
                int level = 0;
                while (level < trimmed.Length && trimmed[level] == '#') level++;
                blocks.Add(new HeaderBlock { Level = level, Text = trimmed.Substring(level).Trim() });
                continue;
            }

            // List
            if (trimmed.StartsWith("- ") || trimmed.StartsWith("* ") || trimmed.StartsWith("+ "))
            {
                if (currentBlock is not ListBlock)
                {
                    if (currentBlock != null) blocks.Add(currentBlock);
                    currentBlock = new ListBlock();
                }
                ((ListBlock)currentBlock).Items.Add(trimmed.Substring(2));
                continue;
            }

            // Quote
            if (trimmed.StartsWith(">"))
            {
                if (currentBlock is not QuoteBlock)
                {
                    if (currentBlock != null) blocks.Add(currentBlock);
                    currentBlock = new QuoteBlock();
                }
                // Append text (handle multiline quotes)
                var q = (QuoteBlock)currentBlock;
                string content = trimmed.TrimStart('>', ' ');
                if (q.Text.Length > 0)
                {
                    q.Text.Append(' ');
                }
                q.Text.Append(content);
                continue;
            }

            // Table
            if (trimmed.StartsWith("|"))
            {
                // Check if it's a table start (header + separator)
                // We need to look ahead for separator `|---|`
                if (currentBlock == null && i + 1 < lines.Count && lines[i+1].Trim().StartsWith("|") && lines[i+1].Contains("-"))
                {
                    var table = new TableBlock();
                    table.Headers = ParseTableLine(line);
                    i++; // Skip header
                    i++; // Skip separator (assumed exists)
                    // Read rows
                    while (i < lines.Count && lines[i].Trim().StartsWith("|"))
                    {
                        table.Rows.Add(ParseTableLine(lines[i]));
                        i++;
                    }
                    i--; // Backtrack one since loop overshot
                    blocks.Add(table);
                    continue;
                }
                // If not a valid table start, treat as paragraph?
            }

            // Paragraph
            if (currentBlock is ParagraphBlock pb)
            {
                pb.Text.Append(' ');
                pb.Text.Append(trimmed);
            }
            else
            {
                if (currentBlock != null) blocks.Add(currentBlock);
                var pbNew = new ParagraphBlock();
                pbNew.Text.Append(trimmed);
                currentBlock = pbNew;
            }
        }

        if (currentBlock != null) blocks.Add(currentBlock);
        return blocks;
    }

    private List<string> ParseTableLine(string line)
    {
        // Split by | but ignore escaped? Simple split for now.
        var parts = line.Split('|');
        // First and last might be empty if line starts/ends with |
        var result = new List<string>();
        foreach (var p in parts)
        {
            if (!string.IsNullOrWhiteSpace(p)) result.Add(p.Trim());
        }
        return result;
    }

    // --- Inline Parsing ---

    private abstract class Token { }
    private class TextToken : Token { public string Text; public MarkdownStyle Style; }
    private class LinkToken : Token { public string Text; public string Url; }
    private class ImageToken : Token { public string AltText; public string Url; }

    private List<Token> ParseInline(string text)
    {
        var tokens = new List<Token>();
        if (string.IsNullOrEmpty(text)) return tokens;

        int i = 0;
        while (i < text.Length)
        {
            // Check for Image ![alt](url)
            if (text[i] == '!' && i + 1 < text.Length && text[i+1] == '[')
            {
                var match = MatchLink(text, i + 1); // Skip !
                if (match != null)
                {
                    tokens.Add(new ImageToken { AltText = match.Text, Url = match.Url });
                    i = match.NextIndex;
                    continue;
                }
            }

            // Check for Link [text](url)
            if (text[i] == '[')
            {
                var match = MatchLink(text, i);
                if (match != null)
                {
                    tokens.Add(new LinkToken { Text = match.Text, Url = match.Url });
                    i = match.NextIndex;
                    continue;
                }
            }

            // Check for Bold **
            if (i + 1 < text.Length && text[i] == '*' && text[i+1] == '*')
            {
                int end = text.IndexOf("**", i + 2);
                if (end != -1)
                {
                    string content = text.Substring(i + 2, end - (i + 2));
                    tokens.Add(new TextToken { Text = content, Style = _theme.Bold });
                    i = end + 2;
                    continue;
                }
            }

            // Check for Code `
            if (text[i] == '`')
            {
                int end = text.IndexOf('`', i + 1);
                if (end != -1)
                {
                    string content = text.Substring(i + 1, end - (i + 1));
                    tokens.Add(new TextToken { Text = content, Style = _theme.CodeSpan });
                    i = end + 1;
                    continue;
                }
            }

            // Plain text
            // Consume until next special char
            int nextSpecial = FindNextSpecial(text, i + 1);
            if (nextSpecial == -1) nextSpecial = text.Length;

            // Include current char if it wasn't a match
            tokens.Add(new TextToken { Text = text.Substring(i, nextSpecial - i), Style = null });
            i = nextSpecial;
        }

        return tokens;
    }

    private int FindNextSpecial(string text, int start)
    {
        // Find [ or ! or * or `
        char[] chars = { '[', '!', '*', '`' };
        return text.IndexOfAny(chars, start);
    }

    private class LinkMatch { public string Text; public string Url; public int NextIndex; }

    private LinkMatch? MatchLink(string text, int startBracket)
    {
        // Expect [Text](Url)
        int endBracket = text.IndexOf(']', startBracket);
        if (endBracket == -1) return null;

        if (endBracket + 1 < text.Length && text[endBracket + 1] == '(')
        {
            int endParen = text.IndexOf(')', endBracket + 2);
            if (endParen != -1)
            {
                return new LinkMatch
                {
                    Text = text.Substring(startBracket + 1, endBracket - startBracket - 1),
                    Url = text.Substring(endBracket + 2, endParen - endBracket - 2),
                    NextIndex = endParen + 1
                };
            }
        }
        return null;
    }
}
