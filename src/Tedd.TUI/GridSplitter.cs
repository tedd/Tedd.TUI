using System;

namespace Tedd.TUI;

public enum GridResizeDirection
{
    Auto,
    Columns,
    Rows
}

public class GridSplitter : Thumb
{
    public static readonly DependencyProperty ResizeDirectionProperty =
        DependencyProperty.Register("ResizeDirection", typeof(GridResizeDirection), typeof(GridSplitter), GridResizeDirection.Auto);

    public GridResizeDirection ResizeDirection
    {
        get => (GridResizeDirection)GetValue(ResizeDirectionProperty);
        set => SetValue(ResizeDirectionProperty, value);
    }

    public GridSplitter()
    {
        Background = ConsoleColor.DarkGray;
        DragDelta += OnDragDelta;
    }

    private void OnDragDelta(object sender, DragDeltaEventArgs e)
    {
        if (Parent is not Grid grid) return;

        int row = Grid.GetRow(this);
        int col = Grid.GetColumn(this);

        GridResizeDirection direction = ResizeDirection;

        if (direction == GridResizeDirection.Auto)
        {
            if (HorizontalAlignment == HorizontalAlignment.Stretch && (VerticalAlignment != VerticalAlignment.Stretch || ActualWidth > ActualHeight))
            {
                direction = GridResizeDirection.Rows;
            }
            else if (VerticalAlignment == VerticalAlignment.Stretch && (HorizontalAlignment != HorizontalAlignment.Stretch || ActualWidth <= ActualHeight))
            {
                direction = GridResizeDirection.Columns;
            }
            else if (RenderSize.Width > RenderSize.Height)
            {
                direction = GridResizeDirection.Rows;
            }
            else
            {
                direction = GridResizeDirection.Columns;
            }
        }

        if (direction == GridResizeDirection.Columns)
        {
            if (col > 0 && col < grid.ColumnDefinitions.Count - 1)
            {
                var leftCol = grid.ColumnDefinitions[col - 1];
                var rightCol = grid.ColumnDefinitions[col + 1];

                double change = e.HorizontalChange;
                if (change == 0) return;

                int newLeftWidth = Math.Max(0, leftCol.ActualWidth + (int)change);
                int newRightWidth = Math.Max(0, rightCol.ActualWidth - (int)change);

                leftCol.Width = new GridLength(newLeftWidth, GridUnitType.Pixel);
                rightCol.Width = new GridLength(newRightWidth, GridUnitType.Pixel);
                grid.Invalidate();
            }
        }
        else if (direction == GridResizeDirection.Rows)
        {
            if (row > 0 && row < grid.RowDefinitions.Count - 1)
            {
                var topRow = grid.RowDefinitions[row - 1];
                var bottomRow = grid.RowDefinitions[row + 1];

                double change = e.VerticalChange;
                if (change == 0) return;

                int newTopHeight = Math.Max(0, topRow.ActualHeight + (int)change);
                int newBottomHeight = Math.Max(0, bottomRow.ActualHeight - (int)change);

                topRow.Height = new GridLength(newTopHeight, GridUnitType.Pixel);
                bottomRow.Height = new GridLength(newBottomHeight, GridUnitType.Pixel);
                grid.Invalidate();
            }
        }
    }

    // We need to access ActualWidth/ActualHeight for auto-direction logic if not stretched,
    // RenderSize is available via UIElement.
    private int ActualWidth => RenderSize.Width;
    private int ActualHeight => RenderSize.Height;

    public override void Render(VirtualBuffer buffer, int offsetX, int offsetY)
    {
        base.Render(buffer, offsetX, offsetY);

        int x = RenderSize.X + offsetX;
        int y = RenderSize.Y + offsetY;

        // Inherit Foreground/Background if not set? Thumb is a Control so it has Background/BorderBrush etc.
        // We will just draw a filled block for the splitter if no template is defined.
        if (TemplateRoot == null)
        {
            ConsoleColor bg = Background ?? ConsoleColor.DarkGray;
            ConsoleColor fg = Foreground;

            for (int j = 0; j < RenderSize.Height; j++)
            {
                for (int i = 0; i < RenderSize.Width; i++)
                {
                    buffer.SetPixel(x + i, y + j, ' ', fg, bg);
                }
            }
        }
    }
}
