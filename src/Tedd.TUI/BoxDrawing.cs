namespace Tedd.TUI;

/// <summary>
/// Box drawing style: single (light) or double lines (Unicode U+2500–U+25FF).
/// </summary>
public enum BoxStyle
{
    /// <summary>Light single-line box drawing (─ │ ┌ ┐ └ ┘).</summary>
    Single,

    /// <summary>Double-line box drawing (═ ║ ╔ ╗ ╚ ╝).</summary>
    Double
}

/// <summary>
/// Unicode box-drawing characters for a given <see cref="BoxStyle"/>.
/// </summary>
public readonly struct BoxDrawingChars
{
    public char TopLeft { get; }
    public char TopRight { get; }
    public char BottomLeft { get; }
    public char BottomRight { get; }
    public char Horizontal { get; }
    public char Vertical { get; }

    public BoxDrawingChars(char tl, char tr, char bl, char br, char h, char v)
    {
        TopLeft = tl;
        TopRight = tr;
        BottomLeft = bl;
        BottomRight = br;
        Horizontal = h;
        Vertical = v;
    }

    /// <summary>
    /// Returns the six box-drawing characters for the given style.
    /// Single: ┌ ┐ └ ┘ ─ │ (U+250C, U+2510, U+2514, U+2518, U+2500, U+2502).
    /// Double: ╔ ╗ ╚ ╝ ═ ║ (U+2554, U+2557, U+255A, U+255D, U+2550, U+2551).
    /// </summary>
    public static BoxDrawingChars Get(BoxStyle style)
    {
        return style switch
        {
            BoxStyle.Single => new BoxDrawingChars(
                '\u250C', '\u2510', '\u2514', '\u2518', '\u2500', '\u2502'),
            BoxStyle.Double => new BoxDrawingChars(
                '\u2554', '\u2557', '\u255A', '\u255D', '\u2550', '\u2551'),
            _ => Get(BoxStyle.Single)
        };
    }
}
