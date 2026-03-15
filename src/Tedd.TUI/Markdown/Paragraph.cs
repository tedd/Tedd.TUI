using System;
using System.Collections.Generic;
using Tedd.TUI;

namespace Tedd.TUI.Markdown;

public class Paragraph : UIElement
{
    private readonly List<UIElement> _children = [];
    public IList<UIElement> Children => _children;

    public void AddChild(UIElement child)
    {
        _children.Add(child);
        child.Parent = this;
    }

    public override int VisualChildrenCount => _children.Count;

    public override UIElement GetVisualChild(int index)
    {
        if (index < 0 || index >= _children.Count) throw new ArgumentOutOfRangeException(nameof(index));
        return _children[index];
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        int maxWidth = availableSize.Width;
        int currentX = 0;
        int currentY = 0;
        int currentRowHeight = 0;
        int maxLineWidth = 0;

        foreach (var child in _children)
        {
            // Measure child with no constraint on width, so we know its natural size
            child.Measure(new Size(int.MaxValue, availableSize.Height));
            Size childSize = child.DesiredSize;

            // Check if wrapping is needed
            // If currentX > 0 (not at start of line) AND adding child exceeds maxWidth
            if (currentX > 0 && currentX + childSize.Width > maxWidth)
            {
                // Wrap to next line
                currentX = 0;
                currentY += currentRowHeight;
                currentRowHeight = 0;
            }

            // Update current row stats
            currentX += childSize.Width;
            currentRowHeight = Math.Max(currentRowHeight, childSize.Height);
            maxLineWidth = Math.Max(maxLineWidth, currentX);
        }

        // Add height of last row
        int totalHeight = currentY + currentRowHeight;

        return new Size(maxLineWidth, totalHeight);
    }

    protected override void ArrangeOverride(Size finalSize)
    {
        int maxWidth = finalSize.Width;
        int currentX = 0;
        int currentY = 0;
        int currentRowHeight = 0;

        // We need to look ahead or store row heights?
        // Actually, for Arrange, we need to know the height of the CURRENT row to align things vertically if needed?
        // But for simple TUI text flow, usually top alignment or center is fine.
        // Let's assume Top alignment in row for now.
        // Wait, if we have different heights (e.g. image vs text), we need to know row height to advance Y correctly.
        // The Measure logic updated currentRowHeight incrementally. We can do the same here.
        // BUT, if we wrap, we need to know the height of the *completed* row before advancing Y.
        // The previous row's height was determined by the max height of items in that row.
        // So we must buffer items in the current row, calculate max height, then arrange them?
        // Yes.

        List<UIElement> currentRowChildren = [];
        int currentRowWidth = 0;

        foreach (var child in _children)
        {
            Size childSize = child.DesiredSize;

            // Check wrapping
            if (currentRowWidth > 0 && currentRowWidth + childSize.Width > maxWidth)
            {
                // Arrange previous row
                ArrangeRow(currentRowChildren, currentX, currentY, currentRowHeight);

                // Move to next line
                currentY += currentRowHeight;
                currentRowChildren.Clear();
                currentRowWidth = 0;
                currentRowHeight = 0;
                currentX = 0; // Reset X for new row
            }

            // Add to current row
            currentRowChildren.Add(child);
            currentRowWidth += childSize.Width;
            currentRowHeight = Math.Max(currentRowHeight, childSize.Height);
        }

        // Arrange last row
        if (currentRowChildren.Count > 0)
        {
            ArrangeRow(currentRowChildren, currentX, currentY, currentRowHeight);
        }
    }

    private void ArrangeRow(List<UIElement> children, int xStart, int yStart, int rowHeight)
    {
        int x = xStart;
        foreach (var child in children)
        {
            // Align child within the row? Top, Center, Bottom?
            // Default to Top (yStart)
            // If we want Center/Bottom, we use child.VerticalAlignment later?
            // For now, simple layout: Place at (x, yStart)

            // Note: child.Arrange takes a Rect relative to parent.
            // We use DesiredSize for width/height.
            child.Arrange(new Rect(x, yStart, child.DesiredSize.Width, child.DesiredSize.Height));

            x += child.DesiredSize.Width;
        }
    }

    public override void Render(VirtualBuffer buffer, int offsetX, int offsetY)
    {
        int x = RenderSize.X + offsetX;
        int y = RenderSize.Y + offsetY;

        foreach (var child in _children)
        {
            child.Render(buffer, x, y);
        }
    }
}
