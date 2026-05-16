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

    protected override Size MeasureOverride(Size availableSize)
    {
        // Let the base class/template decide first.
        var baseSize = base.MeasureOverride(availableSize);

        // If a template is present, respect its desired size.
        if (TemplateRoot != null)
        {
            return baseSize;
        }

        // Ensure a non-zero thickness so the splitter is visible and hit-testable.
        var direction = GetEffectiveResizeDirection();

        int width = baseSize.Width;
        int height = baseSize.Height;

        const int splitterThickness = 1;

        if (direction == GridResizeDirection.Columns)
        {
            // Vertical splitter: fixed width, spans available height.
            width = splitterThickness;
            if (height <= 0)
            {
                height = availableSize.Height > 0 ? availableSize.Height : 1;
            }
        }
        else
        {
            // Horizontal splitter (Rows): fixed height, spans available width.
            height = splitterThickness;
            if (width <= 0)
            {
                width = availableSize.Width > 0 ? availableSize.Width : 1;
            }
        }

        return new Size(width, height);
    }

    private GridResizeDirection GetEffectiveResizeDirection()
    {
        var direction = ResizeDirection;

        if (direction != GridResizeDirection.Auto)
        {
            return direction;
        }

        // Heuristic similar to WPF's GridSplitter when Auto:
        if (HorizontalAlignment == HorizontalAlignment.Stretch &&
            VerticalAlignment != VerticalAlignment.Stretch)
        {
            return GridResizeDirection.Rows;
        }

        if (VerticalAlignment == VerticalAlignment.Stretch &&
            HorizontalAlignment != HorizontalAlignment.Stretch)
        {
            return GridResizeDirection.Columns;
        }

        if (Parent is Grid grid)
        {
            int row = Grid.GetRow(this);
            int col = Grid.GetColumn(this);

            bool rowIsAuto = row >= 0 && row < grid.RowDefinitions.Count && grid.RowDefinitions[row].Height.GridUnitType == GridUnitType.Auto;
            bool colIsAuto = col >= 0 && col < grid.ColumnDefinitions.Count && grid.ColumnDefinitions[col].Width.GridUnitType == GridUnitType.Auto;

            if (rowIsAuto && !colIsAuto) return GridResizeDirection.Rows;
            if (colIsAuto && !rowIsAuto) return GridResizeDirection.Columns;

            if (grid.RowDefinitions.Count > grid.ColumnDefinitions.Count) return GridResizeDirection.Rows;
            if (grid.ColumnDefinitions.Count > grid.RowDefinitions.Count) return GridResizeDirection.Columns;
        }

        // Fallback: prefer column-resizing (vertical splitter).
        return GridResizeDirection.Columns;
    }

    public GridSplitter()
    {
        Background = TuiColor.DarkGray;
        DragDelta += OnDragDelta;
    }

    private void OnDragDelta(object sender, DragDeltaEventArgs e)
    {
        if (Parent is not Grid grid) return;

        int row = Grid.GetRow(this);
        int col = Grid.GetColumn(this);

        GridResizeDirection direction = GetEffectiveResizeDirection();

        if (direction == GridResizeDirection.Columns)
        {
            if (col > 0 && col < grid.ColumnDefinitions.Count - 1)
            {
                var leftCol = grid.ColumnDefinitions[col - 1];
                var rightCol = grid.ColumnDefinitions[col + 1];

                double change = e.HorizontalChange;
                if (change == 0) return;

                // Clamp horizontal change so both columns stay within Min/Max and total width stays constant.
                double leftActual = leftCol.ActualWidth;
                double rightActual = rightCol.ActualWidth;

                double leftMin = leftCol.MinWidth;
                double leftMax = leftCol.MaxWidth;
                double rightMin = rightCol.MinWidth;
                double rightMax = rightCol.MaxWidth;

                // Normalize Max* if unbounded.
                if (double.IsPositiveInfinity(leftMax)) leftMax = double.MaxValue;
                if (double.IsPositiveInfinity(rightMax)) rightMax = double.MaxValue;

                double maxPositiveChange = double.PositiveInfinity;
                double maxNegativeChange = double.NegativeInfinity;

                // How much we can move splitter to the right (positive change):
                // - left column can grow up to leftMax
                // - right column can shrink down to rightMin
                double leftCanGrow = leftMax - leftActual;
                double rightCanShrink = rightActual - rightMin;
                maxPositiveChange = Math.Min(leftCanGrow, rightCanShrink);

                // How much we can move splitter to the left (negative change):
                // - left column can shrink down to leftMin
                // - right column can grow up to rightMax
                double leftCanShrink = leftActual - leftMin;
                double rightCanGrow = rightMax - rightActual;
                maxNegativeChange = -Math.Min(leftCanShrink, rightCanGrow);

                // Clamp requested change into allowed range.
                if (change > maxPositiveChange) change = maxPositiveChange;
                if (change < maxNegativeChange) change = maxNegativeChange;
                if (change == 0) return;

                double newLeftWidth = leftActual + change;
                double newRightWidth = rightActual - change;

                leftCol.Width = new GridLength((int)newLeftWidth, GridUnitType.Pixel);
                rightCol.Width = new GridLength((int)newRightWidth, GridUnitType.Pixel);
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

                // Clamp vertical change so both rows stay within Min/Max and total height stays constant.
                double topActual = topRow.ActualHeight;
                double bottomActual = bottomRow.ActualHeight;

                double topMin = topRow.MinHeight;
                double topMax = topRow.MaxHeight;
                double bottomMin = bottomRow.MinHeight;
                double bottomMax = bottomRow.MaxHeight;

                // Normalize Max* if unbounded.
                if (double.IsPositiveInfinity(topMax)) topMax = double.MaxValue;
                if (double.IsPositiveInfinity(bottomMax)) bottomMax = double.MaxValue;

                double maxPositiveChange = double.PositiveInfinity;
                double maxNegativeChange = double.NegativeInfinity;

                // How much we can move splitter down (positive change):
                // - top row can grow up to topMax
                // - bottom row can shrink down to bottomMin
                double topCanGrow = topMax - topActual;
                double bottomCanShrink = bottomActual - bottomMin;
                maxPositiveChange = Math.Min(topCanGrow, bottomCanShrink);

                // How much we can move splitter up (negative change):
                // - top row can shrink down to topMin
                // - bottom row can grow up to bottomMax
                double topCanShrink = topActual - topMin;
                double bottomCanGrow = bottomMax - bottomActual;
                maxNegativeChange = -Math.Min(topCanShrink, bottomCanGrow);

                // Clamp requested change into allowed range.
                if (change > maxPositiveChange) change = maxPositiveChange;
                if (change < maxNegativeChange) change = maxNegativeChange;
                if (change == 0) return;

                double newTopHeight = topActual + change;
                double newBottomHeight = bottomActual - change;

                // Adjust row heights
                topRow.Height = new GridLength((int)newTopHeight, GridUnitType.Pixel);
                bottomRow.Height = new GridLength((int)newBottomHeight, GridUnitType.Pixel);
                grid.Invalidate();
            }
        }
    }

    public override void Render(VirtualBuffer buffer, int offsetX, int offsetY)
    {
        base.Render(buffer, offsetX, offsetY);

        int x = RenderSize.X + offsetX;
        int y = RenderSize.Y + offsetY;

        // Inherit Foreground/Background if not set? Thumb is a Control so it has Background/BorderBrush etc.
        // We will just draw a filled block for the splitter if no template is defined.
        if (TemplateRoot == null)
        {
            TuiColor bg = Background ?? TuiColor.DarkGray;
            TuiColor fg = Foreground;

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
