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
        DragDelta += OnDragDelta;
    }

    private void OnDragDelta(object sender, DragDeltaEventArgs e)
    {
        if (Parent is not Grid grid)
            return;

        var direction = ResizeDirection;
        if (direction == GridResizeDirection.Auto)
        {
            direction = RenderSize.Width <= RenderSize.Height ? GridResizeDirection.Columns : GridResizeDirection.Rows;
        }

        if (direction == GridResizeDirection.Columns)
        {
            int col = Grid.GetColumn(this);
            if (col > 0 && col < grid.ColumnDefinitions.Count - 1)
            {
                var prev = grid.ColumnDefinitions[col - 1];
                var next = grid.ColumnDefinitions[col + 1];

                double dx = e.HorizontalChange;
                int w1 = prev.ActualWidth;
                int w2 = next.ActualWidth;

                if (prev.Width.GridUnitType == GridUnitType.Star && next.Width.GridUnitType == GridUnitType.Star)
                {
                    double totalStars = prev.Width.Value + next.Width.Value;
                    double newW1 = Math.Max(0, w1 + dx);
                    double newW2 = Math.Max(0, w2 - dx);
                    if (newW1 + newW2 > 0)
                    {
                        double ratio = newW1 / (newW1 + newW2);
                        prev.Width = new GridLength(totalStars * ratio, GridUnitType.Star);
                        next.Width = new GridLength(totalStars * (1 - ratio), GridUnitType.Star);
                    }
                }
                else if (prev.Width.GridUnitType == GridUnitType.Pixel && next.Width.GridUnitType == GridUnitType.Star)
                {
                    var newPrevWidth = Math.Max(0, w1 + dx);
                    newPrevWidth = Math.Max(prev.MinWidth, Math.Min(prev.MaxWidth, newPrevWidth));
                    prev.Width = new GridLength(newPrevWidth, GridUnitType.Pixel);
                }
                else if (prev.Width.GridUnitType == GridUnitType.Star && next.Width.GridUnitType == GridUnitType.Pixel)
                {
                    var newNextWidth = Math.Max(0, w2 - dx);
                    newNextWidth = Math.Max(next.MinWidth, Math.Min(next.MaxWidth, newNextWidth));
                    next.Width = new GridLength(newNextWidth, GridUnitType.Pixel);
                }
                else // Pixel and Pixel, or Auto
                {
                    var newPrevWidth = Math.Max(0, w1 + dx);
                    newPrevWidth = Math.Max(prev.MinWidth, Math.Min(prev.MaxWidth, newPrevWidth));
                    prev.Width = new GridLength(newPrevWidth, GridUnitType.Pixel);

                    var newNextWidth = Math.Max(0, w2 - dx);
                    newNextWidth = Math.Max(next.MinWidth, Math.Min(next.MaxWidth, newNextWidth));
                    next.Width = new GridLength(newNextWidth, GridUnitType.Pixel);
                }

                grid.Invalidate();
            }
        }
        else if (direction == GridResizeDirection.Rows)
        {
            int row = Grid.GetRow(this);
            if (row > 0 && row < grid.RowDefinitions.Count - 1)
            {
                var prev = grid.RowDefinitions[row - 1];
                var next = grid.RowDefinitions[row + 1];

                double dy = e.VerticalChange;
                double h1 = prev.ActualHeight;
                double h2 = next.ActualHeight;

                // Clamp dy so that both rows respect MinHeight/MaxHeight and keep a consistent total height.
                double min1 = Math.Max(prev.MinHeight, 0);
                double max1 = double.IsNaN(prev.MaxHeight) || double.IsInfinity(prev.MaxHeight) ? double.PositiveInfinity : prev.MaxHeight;
                double min2 = Math.Max(next.MinHeight, 0);
                double max2 = double.IsNaN(next.MaxHeight) || double.IsInfinity(next.MaxHeight) ? double.PositiveInfinity : next.MaxHeight;

                double dyLower = Math.Max(min1 - h1, h2 - max2);
                double dyUpper = Math.Min(max1 - h1, h2 - min2);

                double clampedDy;
                if (dyLower <= dyUpper)
                {
                    if (dy < dyLower)
                        clampedDy = dyLower;
                    else if (dy > dyUpper)
                        clampedDy = dyUpper;
                    else
                        clampedDy = dy;
                }
                else
                {
                    clampedDy = 0;
                }

                if (prev.Height.GridUnitType == GridUnitType.Star && next.Height.GridUnitType == GridUnitType.Star)
                {
                    double totalStars = prev.Height.Value + next.Height.Value;
                    double newH1 = h1 + clampedDy;
                    double newH2 = h2 - clampedDy;
                    if (newH1 + newH2 > 0)
                    {
                        double ratio = newH1 / (newH1 + newH2);
                        prev.Height = new GridLength(totalStars * ratio, GridUnitType.Star);
                        next.Height = new GridLength(totalStars * (1 - ratio), GridUnitType.Star);
                    }
                }
                else if (prev.Height.GridUnitType == GridUnitType.Pixel && next.Height.GridUnitType == GridUnitType.Star)
                {
                    prev.Height = new GridLength(h1 + clampedDy, GridUnitType.Pixel);
                }
                else if (prev.Height.GridUnitType == GridUnitType.Star && next.Height.GridUnitType == GridUnitType.Pixel)
                {
                    next.Height = new GridLength(h2 - clampedDy, GridUnitType.Pixel);
                }
                else
                {
                    prev.Height = new GridLength(h1 + clampedDy, GridUnitType.Pixel);
                    next.Height = new GridLength(h2 - clampedDy, GridUnitType.Pixel);
                }

                grid.Invalidate();
            }
        }
    }
}
