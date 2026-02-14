using System.Collections.Generic;

namespace Tedd.TUI.CodeColoring;

public class Token
{
    public string Type { get; set; }
    public object Content { get; set; } // string or List<Token>
    public string? Alias { get; set; }

    // For string content
    public string? TextContent => Content as string;
    // For nested content
    public List<Token>? StreamContent => Content as List<Token>;

    public Token(string type, object content, string? alias = null)
    {
        Type = type;
        Content = content;
        Alias = alias;
    }

    public override string ToString()
    {
        return $"Token({Type}, {Content})";
    }
}
