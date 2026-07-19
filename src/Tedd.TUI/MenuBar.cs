using System;

namespace Tedd.TUI;

public class MenuBar : StackPanel
{
    public MenuBar()
    {
        Orientation = Orientation.Horizontal;
        VerticalAlignment = VerticalAlignment.Top;
    }

    public override void Render(VirtualBuffer buffer, int offsetX, int offsetY)
    {
        int x = RenderSize.X + offsetX;
        int y = RenderSize.Y + offsetY;

        // Draw background strip. No local Background is set in the constructor so the
        // active theme can style it; unthemed bars keep the classic gray strip.
        var bg = Background ?? TuiColor.Gray;
        var fg = Foreground;
        for (int i = 0; i < RenderSize.Width; i++)
        {
            for (int j = 0; j < RenderSize.Height; j++)
            {
                buffer.SetPixel(x + i, y + j, ' ', fg, bg);
            }
        }

        base.Render(buffer, offsetX, offsetY);
    }
}
