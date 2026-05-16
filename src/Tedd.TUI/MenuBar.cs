using System;

namespace Tedd.TUI;

public class MenuBar : StackPanel
{
    public MenuBar()
    {
        Orientation = Orientation.Horizontal;
        Background = TuiColor.Gray;
        VerticalAlignment = VerticalAlignment.Top;
    }

    public override void Render(VirtualBuffer buffer, int offsetX, int offsetY)
    {
        int x = RenderSize.X + offsetX;
        int y = RenderSize.Y + offsetY;

        // Draw background strip
        if (Background.HasValue)
        {
            for (int i = 0; i < RenderSize.Width; i++)
            {
                for (int j = 0; j < RenderSize.Height; j++)
                {
                    buffer.SetPixel(x + i, y + j, ' ', TuiColor.Black, Background.Value);
                }
            }
        }

        base.Render(buffer, offsetX, offsetY);
    }
}
