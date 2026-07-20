namespace Tedd.TUI.Controls;

/// <summary>
/// Visual style for the drop shadow rendered behind a <see cref="Button"/>.
/// Inspired by classic DOS UI toolkits (Turbo Vision, Quick Basic, Turbo Pascal),
/// where dialog buttons cast an L-shaped shadow to the right and below.
/// </summary>
public enum ButtonShadowStyle
{
    /// <summary>No shadow is rendered. The button has no extra footprint. This is the default.</summary>
    None,

    /// <summary>
    /// Solid block of <see cref="Button.ShadowBackground"/> color (typically black or dark gray).
    /// The shadow cells are filled with spaces using the shadow background color, producing a
    /// solid void behind the button. This is the most "DOS-authentic" look.
    /// </summary>
    Solid,

    /// <summary>Light shade pattern (░ U+2591). Subtle stippled shadow.</summary>
    Light,

    /// <summary>Medium shade pattern (▒ U+2592). Classic Turbo Vision shadow density.</summary>
    Medium,

    /// <summary>Dark shade pattern (▓ U+2593). Heavy stippled shadow.</summary>
    Dark,

    /// <summary>
    /// Reads the existing buffer contents under the shadow region and re-draws each cell
    /// with foreground/background remapped to the shadow color. This mimics the classic
    /// Turbo Vision behavior where the underlying dialog content "shows through" but is
    /// dimmed, giving a translucent shadow effect.
    /// </summary>
    Cast,

    /// <summary>
    /// Truecolor alpha-aware drop shadow: blends a semi-transparent
    /// <see cref="Button.ShadowBackground"/> over each underlying cell using Porter-Duff
    /// "over" so the original glyph and color remain visible but darkened. Use this on
    /// truecolor surfaces (Windows Terminal, modern Linux terminals, Blazor DOM) where
    /// per-cell alpha is honored end-to-end. Falls back gracefully on 16-color renderers
    /// because the blended color is quantized to the nearest <see cref="ConsoleColor"/>.
    /// </summary>
    Translucent
}
