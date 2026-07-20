using System;

namespace Tedd.TUI.Controls.Primitives;

public class UniformGrid : Panel
{
    public static readonly DependencyProperty RowsProperty =
        DependencyProperty.Register("Rows", typeof(int), typeof(UniformGrid), 0);

    public int Rows
    {
        get => (int)(GetValue(RowsProperty) ?? 0);
        set => SetValue(RowsProperty, value);
    }

    public static readonly DependencyProperty ColumnsProperty =
        DependencyProperty.Register("Columns", typeof(int), typeof(UniformGrid), 0);

    public int Columns
    {
        get => (int)(GetValue(ColumnsProperty) ?? 0);
        set => SetValue(ColumnsProperty, value);
    }

    public static readonly DependencyProperty FirstColumnProperty =
        DependencyProperty.Register("FirstColumn", typeof(int), typeof(UniformGrid), 0);

    public int FirstColumn
    {
        get => (int)(GetValue(FirstColumnProperty) ?? 0);
        set => SetValue(FirstColumnProperty, value);
    }

    private int _computedRows = 1;
    private int _computedColumns = 1;

    protected override Size MeasureOverride(Size availableSize)
    {
        UpdateComputedValues();

        Size childAvailableSize = new Size(
            availableSize.Width / _computedColumns,
            availableSize.Height / _computedRows);

        int maxChildWidth = 0;
        int maxChildHeight = 0;

        for (int i = 0; i < VisualChildrenCount; i++)
        {
            UIElement? child = GetVisualChild(i);
            if (child != null && child.Visibility)
            {
                child.Measure(childAvailableSize);
                if (child.DesiredSize.Width > maxChildWidth)
                    maxChildWidth = child.DesiredSize.Width;
                if (child.DesiredSize.Height > maxChildHeight)
                    maxChildHeight = child.DesiredSize.Height;
            }
        }

        return new Size(maxChildWidth * _computedColumns, maxChildHeight * _computedRows);
    }

    protected override void ArrangeOverride(Size finalSize)
    {
        if (_computedColumns == 0 || _computedRows == 0) return;

        int childWidth = finalSize.Width / _computedColumns;
        int childHeight = finalSize.Height / _computedRows;

        int firstCol = FirstColumn;
        if (firstCol >= _computedColumns && _computedColumns > 0)
        {
            firstCol = 0;
        }

        int row = 0;
        int col = firstCol;

        for (int i = 0; i < VisualChildrenCount; i++)
        {
            UIElement? child = GetVisualChild(i);
            if (child != null && child.Visibility)
            {
                child.Arrange(new Rect(col * childWidth, row * childHeight, childWidth, childHeight));

                col++;
                if (col >= _computedColumns)
                {
                    col = 0;
                    row++;
                }
            }
        }
    }

    private void UpdateComputedValues()
    {
        int nonCollapsedCount = 0;
        for (int i = 0; i < VisualChildrenCount; i++)
        {
            UIElement? child = GetVisualChild(i);
            if (child != null && child.Visibility)
            {
                nonCollapsedCount++;
            }
        }

        if (nonCollapsedCount == 0)
        {
            _computedColumns = 1;
            _computedRows = 1;
            return;
        }

        int cols = Columns;
        int rows = Rows;
        int firstCol = FirstColumn;

        if (firstCol >= cols && cols > 0)
        {
            firstCol = 0;
        }

        if (rows == 0)
        {
            if (cols > 0)
            {
                rows = (nonCollapsedCount + firstCol + (cols - 1)) / cols;
            }
            else
            {
                rows = (int)Math.Ceiling(Math.Sqrt(nonCollapsedCount + firstCol));
                int diff = rows * rows - (nonCollapsedCount + firstCol);
                if (diff >= rows)
                {
                    cols = rows - 1;
                }
                else
                {
                    cols = rows;
                }
            }
        }
        else if (cols == 0)
        {
            cols = (nonCollapsedCount + firstCol + (rows - 1)) / rows;
        }

        _computedRows = rows < 1 ? 1 : rows;
        _computedColumns = cols < 1 ? 1 : cols;
    }

    protected override void OnPropertyChanged(DependencyProperty dp)
    {
        base.OnPropertyChanged(dp);
        if (dp == RowsProperty || dp == ColumnsProperty || dp == FirstColumnProperty)
        {
            Invalidate();
        }
    }
}
