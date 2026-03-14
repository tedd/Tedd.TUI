using System;
using System.Collections.Specialized;

namespace Tedd.TUI;

public class ItemsPresenter : UIElement
{
    private Panel? _panel;

    protected override void OnTemplatedParentChanged()
    {
        base.OnTemplatedParentChanged();
        ApplyTemplate();
    }

    private void ApplyTemplate()
    {
        if (_panel != null)
        {
            _panel.Parent = null;
            _panel = null;
        }

        if (TemplatedParent is ItemsControl itemsControl && itemsControl.ItemsPanel != null)
        {
            _panel = itemsControl.ItemsPanel.LoadContent(itemsControl) as Panel;
            if (_panel != null)
            {
                _panel.Parent = this;
                // Inform the ItemsControl about its panel if needed, but for now we just hold it.
                itemsControl.ItemsPresenter = this;
                itemsControl.ItemsPanelRoot = _panel;
                PopulatePanel(itemsControl);
            }
        }
        Invalidate();
    }

    internal void PopulatePanel(ItemsControl itemsControl)
    {
        if (_panel == null) return;
        _panel.Children.Clear();

        int index = 0;
        foreach (var item in itemsControl.Items)
        {
            UIElement container;
            if (itemsControl.IsItemItsOwnContainerOverride(item))
            {
                container = (UIElement)item;
            }
            else
            {
                container = itemsControl.GetContainerForItemCore();
                itemsControl.PrepareContainerForItemOverride(container, item, index);
            }
            _panel.Children.Add(container);
            index++;
        }
    }

    public override int VisualChildrenCount => _panel != null ? 1 : 0;

    public override UIElement GetVisualChild(int index)
    {
        if (_panel != null && index == 0) return _panel;
        throw new ArgumentOutOfRangeException(nameof(index));
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        if (_panel != null)
        {
            _panel.Measure(availableSize);
            return _panel.DesiredSize;
        }
        return new Size(0, 0);
    }

    protected override void ArrangeOverride(Size finalSize)
    {
        if (_panel != null)
        {
            _panel.Arrange(new Rect(0, 0, finalSize.Width, finalSize.Height));
        }
    }

    public override void Render(VirtualBuffer buffer, int offsetX, int offsetY)
    {
        if (_panel != null)
        {
            int x = RenderSize.X + offsetX;
            int y = RenderSize.Y + offsetY;
            _panel.Render(buffer, x, y);
        }
    }
}
