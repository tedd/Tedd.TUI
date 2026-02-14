using System;
using System.Collections.Generic;
using System.Linq;

namespace Tedd.TUI.CodeColoring;

public class CodeDocument : StackPanel
{
    private Theme? _theme;
    public Theme Theme
    {
        get => _theme ?? Theme.Default;
        set => _theme = value;
    }

    private StackPanel? _currentLinePanel;

    public CodeDocument()
    {
        Orientation = Orientation.Vertical;
    }

    public void SetCode(string code, string language)
    {
        // Reset
        Children.Clear(); // StackPanel exposes Children via property but as IList usually read-only?
        // Tedd.TUI.StackPanel: public IList<UIElement> Children => _children;
        // List<T> supports Clear().
        Children.Clear();

        if (string.IsNullOrEmpty(code)) return;

        // Initialize first line
        _currentLinePanel = new StackPanel { Orientation = Orientation.Horizontal, Height = 1 };
        AddChild(_currentLinePanel);

        var grammar = LanguageRegistry.GetGrammar(language);
        if (grammar == null)
        {
            // Render plain text
            RenderText(code, "text");
            return;
        }

        var tokens = PrismTokenizer.Tokenize(code, grammar);

        foreach (var token in tokens)
        {
            RenderToken(token, "text");
        }
    }

    private void RenderToken(Token token, string parentType)
    {
        // Determine effective type for this token
        // If this token is "text", it inherits parentType.
        // If it has a specific type (e.g. "keyword"), it overrides parentType (unless we want to merge styles, which we can't easily do with ConsoleColor).
        // So we use token.Type unless it is "text".

        string type = token.Type == "text" ? parentType : token.Type;

        if (token.Content is List<Token> nestedTokens)
        {
            foreach (var nested in nestedTokens)
            {
                RenderToken(nested, type);
            }
        }
        else if (token.Content is string text)
        {
            RenderText(text, type);
        }
        else if (token.TextContent != null) // Fallback if Content is string but not matched by pattern above (should cover string)
        {
             RenderText(token.TextContent, type);
        }
    }

    private void RenderText(string text, string type)
    {
        if (string.IsNullOrEmpty(text)) return;

        int start = 0;
        int length = text.Length;

        while (start < length)
        {
            int newlineIndex = text.IndexOfAny(new[] { '\r', '\n' }, start);

            if (newlineIndex == -1)
            {
                // No more newlines, add remaining text
                string part = text.Substring(start);
                AddSpan(_currentLinePanel, part, type);
                break;
            }

            // Add text before newline
            if (newlineIndex > start)
            {
                string part = text.Substring(start, newlineIndex - start);
                AddSpan(_currentLinePanel, part, type);
            }

            // Handle newline
            // Check for \r\n
            if (text[newlineIndex] == '\r' && newlineIndex + 1 < length && text[newlineIndex + 1] == '\n')
            {
                // Windows newline, skip 2 chars
                start = newlineIndex + 2;
            }
            else
            {
                // \r or \n
                start = newlineIndex + 1;
            }

            // Start new line
            _currentLinePanel = new StackPanel { Orientation = Orientation.Horizontal, Height = 1 };
            AddChild(_currentLinePanel);
        }
    }

    private void AddSpan(StackPanel line, string text, string type)
    {
        if (string.IsNullOrEmpty(text)) return;

        var color = Theme.GetColor(type);

        var span = new TextBlock
        {
            Text = text,
            Foreground = color
        };
        line.AddChild(span);
    }
}
