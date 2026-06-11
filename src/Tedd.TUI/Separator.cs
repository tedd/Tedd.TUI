using System;

namespace Tedd.TUI;

public class Separator : Control
{
    public Separator()
    {
        Focusable = false;
        // Default template draws a horizontal line
        Template = new ControlTemplate(parent =>
        {
            var border = new Border
            {
                BoxStyle = BoxStyle.None, // No outer border
                Height = 1,
            };

            return border;
        });
    }

    public override void Render(VirtualBuffer buffer, int offsetX, int offsetY)
    {
        base.Render(buffer, offsetX, offsetY);

        int x = RenderSize.X + offsetX;
        int y = RenderSize.Y + offsetY;

        // Draw a line across the width
        char c = '\u2500'; // light horizontal line

        buffer.DrawHLine(x, y, RenderSize.Width, c, Foreground, Background ?? TuiColor.Gray);
    }
}
