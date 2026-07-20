namespace Tedd.TUI.Media;

/// <summary>
/// Box drawing style: single (light), double lines, heavy lines (Unicode U+2500–U+25FF),
/// or none (no border drawn at all).
/// </summary>
public enum BoxStyle
{
    /// <summary>Light single-line box drawing (─ │ ┌ ┐ └ ┘).</summary>
    Single,

    /// <summary>Double-line box drawing (═ ║ ╔ ╗ ╚ ╝).</summary>
    Double,

    /// <summary>Heavy single-line box drawing (━ ┃ ┏ ┓ ┗ ┛).</summary>
    Heavy,

    /// <summary>
    /// No border lines and zero border thickness. Consumers should treat this as
    /// "draw no border characters and reserve no border space". For controls like
    /// <see cref="Button"/> this produces a flat label-style appearance.
    /// </summary>
    None
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
    /// Heavy:  ┏ ┓ ┗ ┛ ━ ┃ (U+250F, U+2513, U+2517, U+251B, U+2501, U+2503).
    /// </summary>
    public static BoxDrawingChars Get(BoxStyle style)
    {
        return style switch
        {
            BoxStyle.Single => new BoxDrawingChars(
                '\u250C', '\u2510', '\u2514', '\u2518', '\u2500', '\u2502'),
            BoxStyle.Double => new BoxDrawingChars(
                '\u2554', '\u2557', '\u255A', '\u255D', '\u2550', '\u2551'),
            BoxStyle.Heavy => new BoxDrawingChars(
                '\u250F', '\u2513', '\u2517', '\u251B', '\u2501', '\u2503'),
            BoxStyle.None => new BoxDrawingChars(' ', ' ', ' ', ' ', ' ', ' '),
            _ => Get(BoxStyle.Single)
        };
    }

    /// <summary>
    /// Cross / four-way junction for interior grid lines (e.g. table row separators).
    /// Matches the weight of <see cref="Get"/> for Single, Double, and Heavy.
    /// </summary>
    public static char GetInteriorCross(BoxStyle style) => style switch
    {
        BoxStyle.Single => '\u253C', // ┼
        BoxStyle.Double => '\u256C', // ╬
        BoxStyle.Heavy => '\u254B', // ╋
        _ => '\u253C'
    };

    /// <summary>
    /// Box-drawing chars for interior rules when <paramref name="style"/> is <see cref="BoxStyle.None"/>
    /// (falls back to light single so grid lines still render).
    /// </summary>
    public static BoxDrawingChars GetInterior(BoxStyle style) =>
        Get(style == BoxStyle.None ? BoxStyle.Single : style);
}
