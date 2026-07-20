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

    /// <summary>
    /// Directory used by <see cref="Image"/> elements to resolve relative <c>Source</c> paths.
    /// Forwarded onto every <see cref="Image"/> the parser creates.
    /// </summary>
    public string? BaseDirectory { get; set; }

    public MarkdownParser(MarkdownTheme theme)
    {
        _theme = theme;
    }

    public FlowDocument Parse(string markdown)
    {
        var doc = new FlowDocument();
        if (string.IsNullOrEmpty(markdown)) return doc;

        // Optimization: Span slicing replaces String.Split array allocations O(1) allocation instead of O(n)
        var lines = new List<string>();
        foreach (var line in markdown.AsSpan().EnumerateLines())
        {
            lines.Add(line.ToString());
        }
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
                    Foreground = _theme.List.Foreground ?? TuiColor.White
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
            // Fall back to the theme-level default language when the fence omitted one.
            // Useful for blog content where ``` fences carry no language tag and the
            // surrounding context implies a single language (e.g. C# for a .NET blog).
            string effectiveLanguage = string.IsNullOrEmpty(code.Language)
                ? (_theme.DefaultCodeLanguage ?? "")
                : code.Language;
            cd.SetCode(code.Code, effectiveLanguage);
            return cd;
        }
        else if (block is SpacerBlock)
        {
            // A single-row blank acts as a paragraph separator. Width=1 keeps StackPanel
            // from collapsing the element to zero height (empty Text measures to 0,0).
            return new TextBlock { Text = " " };
        }
        else if (block is QuoteBlock quote)
        {
            var p = new Paragraph();
            // Add quote marker
            var marker = new TextBlock { Text = "│ ", Foreground = _theme.Quote.Foreground ?? TuiColor.DarkGray };
            p.AddChild(marker);

            AddInlineContent(p, quote.Text.ToString(), _theme.Quote);
            return p;
        }
        else if (block is TableBlock tableBlock)
        {
            var table = new Table();
            table.ShowHeader = true;
            table.HeaderForeground = _theme.Header4.Foreground ?? TuiColor.White;

            table.ShowBorder = _theme.Table.ShowBorder;
            table.ShowVerticalLines = _theme.Table.ShowVerticalLines;
            table.ShowHorizontalLines = _theme.Table.ShowHorizontalLines;
            table.BorderStyle = _theme.Table.BorderStyle;

            if (_theme.Table.HeaderBackground.HasValue)
                table.HeaderBackground = _theme.Table.HeaderBackground.Value;

            // A markdown table is meant to read as one uniform surface. Cells fall back to the
            // header background when the markdown theme sets no explicit cell background, and
            // that header background itself may come from the ambient TuiTheme's Table style
            // (e.g. TurboPascal paints headers cyan). Read the *resolved* value here and drive
            // every part of the chrome from it, or the pieces diverge:
            //   - the whole-table fill (Table.Background) covers any sub-cell gaps,
            //   - the interior dividers and row separators (GridLineBackground) sit on the same
            //     field instead of the theme's own GridLineBackground (TurboPascal's blue),
            //   - the divider glyphs (GridLineForeground) use the header foreground so they stay
            //     visible; the theme's GridLineForeground is tuned for its own background and can
            //     collide with the cell color (TurboPascal's cyan-on-cyan would vanish).
            TuiColor cellBackground = _theme.Table.CellBackground ?? table.HeaderBackground;
            table.Background = cellBackground;
            table.GridLineBackground = cellBackground;
            table.GridLineForeground = table.HeaderForeground;

            var cellMarkdownStyle = new MarkdownStyle(
                foreground: _theme.Table.CellForeground ?? _theme.Header4.Foreground ?? TuiColor.White,
                background: cellBackground);

            // Define Columns
            if (tableBlock.Headers != null)
            {
                for (int i = 0; i < tableBlock.Headers.Count; i++)
                {
                    var h = tableBlock.Headers[i];
                    // Last column uses Star width to fill remaining space and ensure cells extend to table edge.
                    var width = (i == tableBlock.Headers.Count - 1)
                        ? GridLength.Star
                        : GridLength.Auto;
                    table.Columns.Add(new TableColumn { Header = h, Width = width });
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
                    // Set cell background so the entire cell rect is painted, not just glyphs.
                    if (cellMarkdownStyle.Background.HasValue)
                        cellP.Background = cellMarkdownStyle.Background.Value;
                    AddInlineContent(cellP, cellText, cellMarkdownStyle);
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

                // Optimization: Span slicing replaces String.Split array allocations O(1) allocation instead of O(n)
                ReadOnlySpan<char> span = tt.Text.AsSpan();
                int start = 0;
                while (start < span.Length)
                {
                    int end = span.Slice(start).IndexOf(' ');
                    string word;
                    bool isLast = false;

                    if (end == -1)
                    {
                        word = span.Slice(start).ToString();
                        isLast = true;
                        start = span.Length;
                    }
                    else
                    {
                        word = span.Slice(start, end).ToString();
                        start += end + 1;
                    }

                    if (string.IsNullOrEmpty(word) && !isLast)
                    {
                        // Multiple spaces? Or split caused empty entry.
                        // Render space.
                    }

                    // Apply style mixing?
                    // tt.Style (from inline) vs baseStyle (from block)
                    // Inline wins.
                    var fg = tt.Style?.Foreground ?? baseStyle.Foreground ?? TuiColor.Gray;
                    var bg = tt.Style?.Background ?? baseStyle.Background;

                    var tb = new TextBlock
                    {
                        Text = word + (isLast ? "" : " "), // Add space back except last
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
                    Foreground = _theme.Link.Foreground ?? TuiColor.Blue
                };
                if (baseStyle.Background.HasValue)
                    link.Background = baseStyle.Background.Value;
                // Ensure space handling? Links usually distinctive.
                p.AddChild(link);
            }
            else if (token is ImageToken it)
            {
                var imgStyle = _theme.Image;
                var img = new Image
                {
                    AltText = it.AltText,
                    Source = it.Url,
                    Foreground = imgStyle.Foreground ?? TuiColor.Green,
                    MaxCellWidth = imgStyle.MaxCellWidth,
                    MaxCellHeight = imgStyle.MaxCellHeight,
                    RenderMode = imgStyle.RenderMode,
                    AsciiRenderer = imgStyle.AsciiRenderer,
                    BaseDirectory = BaseDirectory
                };
                if (imgStyle.Background.HasValue)
                {
                    img.Background = imgStyle.Background;
                }
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
    /// <summary>Marks a vertical spacer (one rendered blank row) caused by one or more
    /// blank lines in the source. Coalesced so a run of N blank lines yields one spacer.</summary>
    private class SpacerBlock : Block { }

    private List<Block> ParseBlocks(List<string> lines)
    {
        var blocks = new List<Block>();
        Block? currentBlock = null;
        bool inBlankRun = false;

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
                // Coalesce consecutive blank lines into a single spacer block, but only
                // when there's already preceding content. This produces visual paragraph
                // spacing without growing the gap with each extra blank line in source.
                if (!inBlankRun && blocks.Count > 0 && blocks[blocks.Count - 1] is not SpacerBlock)
                {
                    blocks.Add(new SpacerBlock());
                }
                inBlankRun = true;
                continue;
            }
            inBlankRun = false;

            // Code Block (Fence)
            if (trimmed.StartsWith("```") || trimmed.StartsWith("~~~"))
            {
                if (currentBlock != null) { blocks.Add(currentBlock); currentBlock = null; }

                char fenceChar = trimmed[0];
                int fenceLen = 0;
                while (fenceLen < trimmed.Length && trimmed[fenceLen] == fenceChar) fenceLen++;
                string fenceStr = new string(fenceChar, fenceLen);

                string afterFence = trimmed.Substring(fenceLen);

                // Heuristic: spec-compliant fences put the language identifier directly after
                // the fence chars (e.g. ```csharp). WordPress-style exports use ``` like inline
                // code spans (e.g. ``` some code ```), where any text following a space is
                // actually content. Use the leading space as the discriminator.
                bool firstLineIsContent = afterFence.Length > 0 && afterFence[0] == ' ';

                // Single-line code block: same line contains both an opening and a closing
                // fence (e.g. ``` foo ```). Take the text between them as the code.
                int inlineCloseIdx = afterFence.LastIndexOf(fenceStr, StringComparison.Ordinal);
                if (inlineCloseIdx >= 0)
                {
                    string inlineContent = afterFence.Substring(0, inlineCloseIdx).Trim();
                    blocks.Add(new CodeBlock { Language = "", Code = inlineContent });
                    continue;
                }

                string lang = firstLineIsContent ? "" : afterFence.Trim();
                var codeLines = new List<string>();
                if (firstLineIsContent)
                {
                    string firstLine = afterFence.Trim();
                    if (firstLine.Length > 0) codeLines.Add(firstLine);
                }

                i++; // Skip opening fence line
                while (i < lines.Count)
                {
                    string contentLine = lines[i];
                    string contentTrimmed = contentLine.Trim();

                    // Spec-compliant closing fence: line consists entirely of fence chars
                    // (length >= the opening fence). Prevents content lines that merely
                    // start with ``` from ending the block prematurely.
                    if (contentTrimmed.Length >= fenceLen && contentTrimmed.All(c => c == fenceChar))
                    {
                        break;
                    }

                    // Lenient closing fence: a content line that ends with ``` (common in
                    // WordPress-exported markdown). Strip the trailing fence and keep the
                    // preceding text as the last line of code.
                    if (contentTrimmed.EndsWith(fenceStr, StringComparison.Ordinal))
                    {
                        int idx = contentLine.LastIndexOf(fenceStr, StringComparison.Ordinal);
                        string lastLine = idx >= 0 ? contentLine.Substring(0, idx).TrimEnd() : contentLine;
                        if (lastLine.Length > 0) codeLines.Add(lastLine);
                        i++;
                        break;
                    }

                    codeLines.Add(contentLine);
                    i++;
                }

                blocks.Add(new CodeBlock { Language = lang, Code = string.Join("\n", codeLines) });
                continue;
            }

            // Header (CommonMark ATX: 1-6 # chars followed by space or end-of-line).
            // Requiring a space after the # rejects CSS selectors and other lines that
            // happen to start with '#' (e.g. WordPress-exported `#arrayTable1 {` blocks).
            if (trimmed.StartsWith("#"))
            {
                int level = 0;
                while (level < trimmed.Length && trimmed[level] == '#') level++;
                bool isAtxHeading = level >= 1 && level <= 6
                    && (level == trimmed.Length || trimmed[level] == ' ');

                if (isAtxHeading)
                {
                    if (currentBlock != null) { blocks.Add(currentBlock); currentBlock = null; }
                    blocks.Add(new HeaderBlock { Level = level, Text = trimmed.Substring(level).Trim() });
                    continue;
                }
                // else: not a heading -- fall through to paragraph handling below.
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

            // Table.
            //
            // Two acceptable header forms:
            //   GFM-strict:  | Col1 | Col2 |    (leading + trailing pipe)
            //   WordPress:   Col1 | Col2 | Col3 (no leading pipe, optional trailing)
            //
            // Detection rule: current line contains at least one '|' AND the next
            // line is a separator row (only '-', '|', ':', whitespace; with at
            // least one '-' and one '|'). Continuation rows are any subsequent
            // lines that contain '|'.
            if (currentBlock == null
                && trimmed.Contains('|')
                && i + 1 < lines.Count
                && IsTableSeparator(lines[i + 1]))
            {
                var table = new TableBlock();
                table.Headers = ParseTableLine(line);
                int columnCount = table.Headers.Count;

                i++; // skip header
                i++; // skip separator

                while (i < lines.Count && lines[i].Trim().Contains('|'))
                {
                    var cells = ParseTableLine(lines[i]);
                    // Pad short rows so every row has the same column count
                    // (WordPress sometimes drops the trailing empty cell).
                    while (cells.Count < columnCount) cells.Add(string.Empty);
                    table.Rows.Add(cells);
                    i++;
                }
                i--; // backtrack so the outer loop's i++ lands on the next line
                blocks.Add(table);
                continue;
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

    /// <summary>
    /// True when <paramref name="line"/> is a markdown table separator row -- one
    /// composed only of '-', '|', ':', and whitespace, with at least one '-' and
    /// one '|'. Allows e.g. <c>|---|---|</c>, <c>---|---|---</c>, or <c>:---:</c>.
    /// </summary>
    private static bool IsTableSeparator(string line)
    {
        string trimmed = line.Trim();
        if (trimmed.Length == 0) return false;
        bool hasDash = false;
        bool hasPipe = false;
        for (int i = 0; i < trimmed.Length; i++)
        {
            char c = trimmed[i];
            if (c == '-') hasDash = true;
            else if (c == '|') hasPipe = true;
            else if (c != ':' && c != ' ' && c != '\t') return false;
        }
        return hasDash && hasPipe;
    }

    /// <summary>
    /// Splits a markdown table row into cells. Strips a single optional leading
    /// and trailing '|' (GFM-strict syntax) but preserves empty cells in between
    /// so that `| a |  | c |` correctly produces three cells (`a`, ``, `c`).
    /// </summary>
    private List<string> ParseTableLine(string line)
    {
        // Trim trailing whitespace (covers the WordPress two-trailing-spaces line
        // break) but keep leading whitespace -- it's part of cell content if the
        // line starts inside a cell.
        ReadOnlySpan<char> span = line.AsSpan().TrimEnd();
        int start = 0;
        int end = span.Length;

        // Strip optional leading/trailing pipe (the GFM border characters).
        if (start < end && span[start] == '|') start++;
        if (end > start && span[end - 1] == '|') end--;

        var result = new List<string>();
        int cellStart = start;
        for (int i = start; i < end; i++)
        {
            if (span[i] == '|')
            {
                result.Add(span.Slice(cellStart, i - cellStart).Trim().ToString());
                cellStart = i + 1;
            }
        }
        // Final cell (always added, even if empty -- it's a real cell position).
        result.Add(span.Slice(cellStart, end - cellStart).Trim().ToString());

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
            if (text[i] == '!' && i + 1 < text.Length && text[i + 1] == '[')
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
            if (i + 1 < text.Length && text[i] == '*' && text[i + 1] == '*')
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
        ReadOnlySpan<char> chars = ['[', '!', '*', '`'];
        int index = text.AsSpan(start).IndexOfAny(chars);
        return index == -1 ? -1 : start + index;
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
