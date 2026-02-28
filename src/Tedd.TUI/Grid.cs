using System;
using System.Collections.Generic;

namespace Tedd.TUI;

public class Grid : Panel
{
    public List<RowDefinition> RowDefinitions { get; } = new List<RowDefinition>();
    public List<ColumnDefinition> ColumnDefinitions { get; } = new List<ColumnDefinition>();

    private List<RowDefinition> _implicitRows;
    private List<ColumnDefinition> _implicitCols;

    // Attached Properties
    public static readonly DependencyProperty RowProperty = DependencyProperty.Register("Row", typeof(int), typeof(Grid), 0);
    public static readonly DependencyProperty ColumnProperty = DependencyProperty.Register("Column", typeof(int), typeof(Grid), 0);
    public static readonly DependencyProperty RowSpanProperty = DependencyProperty.Register("RowSpan", typeof(int), typeof(Grid), 1);
    public static readonly DependencyProperty ColumnSpanProperty = DependencyProperty.Register("ColumnSpan", typeof(int), typeof(Grid), 1);

    public static void SetRow(UIElement element, int value) => element.SetValue(RowProperty, value);
    public static int GetRow(UIElement element) => (int)element.GetValue(RowProperty);

    public static void SetColumn(UIElement element, int value) => element.SetValue(ColumnProperty, value);
    public static int GetColumn(UIElement element) => (int)element.GetValue(ColumnProperty);

    public static void SetRowSpan(UIElement element, int value) => element.SetValue(RowSpanProperty, value);
    public static int GetRowSpan(UIElement element) => (int)element.GetValue(RowSpanProperty);

    public static void SetColumnSpan(UIElement element, int value) => element.SetValue(ColumnSpanProperty, value);
    public static int GetColumnSpan(UIElement element) => (int)element.GetValue(ColumnSpanProperty);

    protected override void OnDataContextChanged(object newValue)
    {
        base.OnDataContextChanged(newValue);
        foreach (var child in Children)
        {
            // If child doesn't have a local DataContext, it inherits.
            // The base UIElement implementation usually handles this if we implement VisualChildrenCount correctly.
            // But base implementation says:
            /*
            if (dp.IsInherited) {
               // ... iterates children ...
            }
            */
            // Since DataContext is inherited, base.OnPropertyChanged(DataContextProperty) will call OnPropertyChanged on children.
            // So we don't need to do anything manual here provided VisualChildrenCount is correct.
        }
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        // 1. Initialize Definitions if empty (default to 1x1)
        bool implicitRow = RowDefinitions.Count == 0;
        bool implicitCol = ColumnDefinitions.Count == 0;

        List<RowDefinition> rows;
        if (implicitRow)
        {
            if (_implicitRows == null) _implicitRows = new List<RowDefinition> { new RowDefinition() };
            rows = _implicitRows;
        }
        else
        {
            rows = RowDefinitions;
        }

        List<ColumnDefinition> cols;
        if (implicitCol)
        {
            if (_implicitCols == null) _implicitCols = new List<ColumnDefinition> { new ColumnDefinition() };
            cols = _implicitCols;
        }
        else
        {
            cols = ColumnDefinitions;
        }

        // Reset actual sizes
        foreach (var r in rows) r.ActualHeight = 0;
        foreach (var c in cols) c.ActualWidth = 0;

        // 2. Calculate Column Widths
        // 2a. Fixed Pixels
        foreach (var c in cols)
        {
            if (c.Width.GridUnitType == GridUnitType.Pixel)
                c.ActualWidth = (int)c.Width.Value;
        }

        // 2b. Auto
        // Filter children that are in Auto columns
        foreach (var child in Children)
        {
            int colIdx = Math.Min(GetColumn(child), cols.Count - 1);
            int colSpan = Math.Min(GetColumnSpan(child), cols.Count - colIdx);

            // Only consider span=1 for Auto sizing simplicity for now
            if (colSpan == 1)
            {
                var col = cols[colIdx];
                if (col.Width.GridUnitType == GridUnitType.Auto)
                {
                    // Measure with infinite width to get desired width
                    child.Measure(new Size(int.MaxValue, availableSize.Height));
                    col.ActualWidth = Math.Max(col.ActualWidth, child.DesiredSize.Width);
                }
            }
        }

        // Apply Min/Max constraints for Auto/Pixel
        foreach (var c in cols)
        {
            if (c.Width.GridUnitType != GridUnitType.Star)
            {
                 c.ActualWidth = Math.Max(c.MinWidth, Math.Min(c.MaxWidth, c.ActualWidth));
            }
        }

        // 2c. Star
        // Optimization: Replaced LINQ Sum() with manual loop to avoid allocation.
        // O(C) where C is column count.
        int usedWidth = 0;
        foreach (var c in cols) usedWidth += c.ActualWidth;

        int remainingWidth = Math.Max(0, availableSize.Width - usedWidth);

        // Optimization: Replaced LINQ Where().Sum() with manual loop.
        double totalStarsX = 0;
        foreach (var c in cols)
        {
            if (c.Width.GridUnitType == GridUnitType.Star)
                totalStarsX += c.Width.Value;
        }

        if (totalStarsX > 0)
        {
            foreach (var c in cols)
            {
                if (c.Width.GridUnitType == GridUnitType.Star)
                {
                    double share = c.Width.Value / totalStarsX;
                    c.ActualWidth = (int)(remainingWidth * share);
                    c.ActualWidth = Math.Max(c.MinWidth, Math.Min(c.MaxWidth, c.ActualWidth));
                }
            }
        }

        // Calculate Offsets
        int currentX = 0;
        foreach(var c in cols) { c.Offset = currentX; currentX += c.ActualWidth; }


        // 3. Calculate Row Heights
        // 3a. Fixed Pixels
        foreach (var r in rows)
        {
            if (r.Height.GridUnitType == GridUnitType.Pixel)
                r.ActualHeight = (int)r.Height.Value;
        }

        // 3b. Auto
        foreach (var child in Children)
        {
            int rowIdx = Math.Min(GetRow(child), rows.Count - 1);
            int rowSpan = Math.Min(GetRowSpan(child), rows.Count - rowIdx);

            // Only consider span=1
            if (rowSpan == 1)
            {
                var row = rows[rowIdx];
                if (row.Height.GridUnitType == GridUnitType.Auto)
                {
                    // Determine constrained width for this child
                    int colIdx = Math.Min(GetColumn(child), cols.Count - 1);
                    int colSpan = Math.Min(GetColumnSpan(child), cols.Count - colIdx);

                    int childAvailableWidth = 0;
                    for(int k=0; k<colSpan; k++) childAvailableWidth += cols[colIdx + k].ActualWidth;

                    child.Measure(new Size(childAvailableWidth, int.MaxValue));
                    row.ActualHeight = Math.Max(row.ActualHeight, child.DesiredSize.Height);
                }
            }
        }

        // Apply Min/Max
        foreach (var r in rows)
        {
            if (r.Height.GridUnitType != GridUnitType.Star)
            {
                 r.ActualHeight = Math.Max(r.MinHeight, Math.Min(r.MaxHeight, r.ActualHeight));
            }
        }

        // 3c. Star
        // Optimization: Replaced LINQ Sum() with manual loop.
        int usedHeight = 0;
        foreach (var r in rows) usedHeight += r.ActualHeight;

        int remainingHeight = Math.Max(0, availableSize.Height - usedHeight);

        // Optimization: Replaced LINQ Where().Sum() with manual loop.
        double totalStarsY = 0;
        foreach (var r in rows)
        {
            if (r.Height.GridUnitType == GridUnitType.Star)
                totalStarsY += r.Height.Value;
        }

        if (totalStarsY > 0)
        {
            foreach (var r in rows)
            {
                if (r.Height.GridUnitType == GridUnitType.Star)
                {
                    double share = r.Height.Value / totalStarsY;
                    r.ActualHeight = (int)(remainingHeight * share);
                    r.ActualHeight = Math.Max(r.MinHeight, Math.Min(r.MaxHeight, r.ActualHeight));
                }
            }
        }

        // Calculate Offsets
        int currentY = 0;
        foreach(var r in rows) { r.Offset = currentY; currentY += r.ActualHeight; }

        // 4. Final Measure for all children (to ensure correct DesiredSize based on final grid slots)
        foreach (var child in Children)
        {
             int colIdx = Math.Min(GetColumn(child), cols.Count - 1);
             int colSpan = Math.Min(GetColumnSpan(child), cols.Count - colIdx);
             int rowIdx = Math.Min(GetRow(child), rows.Count - 1);
             int rowSpan = Math.Min(GetRowSpan(child), rows.Count - rowIdx);

             int finalW = 0;
             for(int k=0; k<colSpan; k++) finalW += cols[colIdx+k].ActualWidth;

             int finalH = 0;
             for(int k=0; k<rowSpan; k++) finalH += rows[rowIdx+k].ActualHeight;

             child.Measure(new Size(finalW, finalH));
        }

        // Optimization: Replaced LINQ Sum() with manual loop.
        int totalW = 0;
        foreach (var c in cols) totalW += c.ActualWidth;

        int totalH = 0;
        foreach (var r in rows) totalH += r.ActualHeight;

        return new Size(totalW, totalH);
    }

    protected override void ArrangeOverride(Size finalSize)
    {
        // Definitions are already calculated in Measure.
        // But wait, Measure uses availableSize. Arrange uses finalSize.
        // If finalSize != DesiredSize (e.g. stretched), we might need to adjust Star rows/cols?
        // For simplicity in TUI, we assume Measure did the job or we just re-layout star if needed?
        // Usually Arrange just places things. If Grid was given more space than Desired,
        // it should probably stretch Star columns further.

        // Re-evaluating Star based on finalSize is better for "Stretch" alignment.

        bool implicitRow = RowDefinitions.Count == 0;
        bool implicitCol = ColumnDefinitions.Count == 0;

        List<RowDefinition> rows;
        if (implicitRow)
        {
            // Use existing implicit row list, ensuring it's initialized (though Measure should have done it)
            if (_implicitRows == null) _implicitRows = new List<RowDefinition> { new RowDefinition { Height = GridLength.Star } };
            rows = _implicitRows;
        }
        else
        {
            rows = RowDefinitions;
        }

        List<ColumnDefinition> cols;
        if (implicitCol)
        {
            if (_implicitCols == null) _implicitCols = new List<ColumnDefinition> { new ColumnDefinition { Width = GridLength.Star } };
            cols = _implicitCols;
        }
        else
        {
            cols = ColumnDefinitions;
        }

        // Recalculate stars if size changed
        // ... Optimization: Skip if size matches DesiredSize

        // For MVP, let's just stick to what we calculated in Measure or do a quick re-calc for Stars.
        // To be safe and correct (e.g. window resize), let's re-distribute extra space to stars.

        // Optimization: Replaced LINQ Sum() with manual loop.
        int currentTotalW = 0;
        foreach (var c in cols) currentTotalW += c.ActualWidth;

        int extraW = finalSize.Width - currentTotalW;
        if (extraW != 0)
        {
            // Optimization: Replaced LINQ Where().Sum() with manual loop.
            double totalStarsX = 0;
            foreach (var c in cols)
            {
                if (c.Width.GridUnitType == GridUnitType.Star)
                    totalStarsX += c.Width.Value;
            }

            if (totalStarsX > 0)
            {
                 int usedW = 0;
                 foreach(var c in cols)
                 {
                     if (c.Width.GridUnitType == GridUnitType.Star)
                     {
                         double share = c.Width.Value / totalStarsX;
                         // Add proportionate share of EXTRA space (can be negative if we shrank)
                         // But we calculated based on availableSize in Measure.
                         // Actually, we should recalculate from scratch based on finalSize but keep Auto/Pixel same.
                         // Simpler: Just add extra to stars.
                         c.ActualWidth += (int)(extraW * share);
                         c.ActualWidth = Math.Max(c.MinWidth, Math.Min(c.MaxWidth, c.ActualWidth));
                     }
                     usedW += c.ActualWidth;
                 }
            }
        }

        // Optimization: Replaced LINQ Sum() with manual loop.
        int currentTotalH = 0;
        foreach (var r in rows) currentTotalH += r.ActualHeight;

        int extraH = finalSize.Height - currentTotalH;
        if (extraH != 0)
        {
             // Optimization: Replaced LINQ Where().Sum() with manual loop.
             double totalStarsY = 0;
             foreach (var r in rows)
             {
                 if (r.Height.GridUnitType == GridUnitType.Star)
                     totalStarsY += r.Height.Value;
             }

             if (totalStarsY > 0)
             {
                 foreach(var r in rows)
                 {
                     if (r.Height.GridUnitType == GridUnitType.Star)
                     {
                         double share = r.Height.Value / totalStarsY;
                         r.ActualHeight += (int)(extraH * share);
                         r.ActualHeight = Math.Max(r.MinHeight, Math.Min(r.MaxHeight, r.ActualHeight));
                     }
                 }
             }
        }

        // Recalculate offsets
        int offX = 0; foreach(var c in cols) { c.Offset = offX; offX += c.ActualWidth; }
        int offY = 0; foreach(var r in rows) { r.Offset = offY; offY += r.ActualHeight; }

        foreach (var child in Children)
        {
             int colIdx = Math.Min(GetColumn(child), cols.Count - 1);
             int colSpan = Math.Min(GetColumnSpan(child), cols.Count - colIdx);
             int rowIdx = Math.Min(GetRow(child), rows.Count - 1);
             int rowSpan = Math.Min(GetRowSpan(child), rows.Count - rowIdx);

             int x = cols[colIdx].Offset;
             int y = rows[rowIdx].Offset;

             int w = 0;
             for(int k=0; k<colSpan; k++) w += cols[colIdx+k].ActualWidth;

             int h = 0;
             for(int k=0; k<rowSpan; k++) h += rows[rowIdx+k].ActualHeight;

             child.Arrange(new Rect(x, y, w, h));
        }
    }
}
