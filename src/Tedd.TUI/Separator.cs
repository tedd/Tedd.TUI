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
        if (TemplateRoot != null)
        {
            base.Render(buffer, offsetX, offsetY);
            return;
        }

        Thickness padding = Padding;

        int width = Math.Max(0, RenderSize.Width - padding.Left - padding.Right);
        int height = Math.Max(0, RenderSize.Height - padding.Top - padding.Bottom);
        if (width <= 0 || height <= 0) return;

        int x = RenderSize.X + offsetX + padding.Left;
        int y = RenderSize.Y + offsetY + padding.Top;

        // Draw a line across the width
        char c = '\u2500'; // light horizontal line

        if (Background.HasValue)
        {
            buffer.DrawHLine(x, y, width, c, Foreground, Background.Value);
        }
        else
        {
            for (int i = 0; i < width; i++)
            {
                var bg = buffer.GetPixel(x + i, y).Background;
                buffer.SetPixel(x + i, y, c, Foreground, bg);
            }
        }
    }
}
