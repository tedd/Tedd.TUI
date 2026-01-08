using System.Collections.Generic;

namespace Tedd.TUI;

public class TuiWindow : UIElement
{
    private UIElement _content;
    public UIElement Content 
    { 
        get => _content;
        set
        {
            _content = value;
            if (_content != null)
            {
                _content.Parent = this;
                _content.DataContext = this.DataContext;
            }
        }
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        if (Content != null)
        {
            Content.Measure(availableSize);
            return Content.DesiredSize;
        }
        return new Size(0, 0);
    }

    protected override void ArrangeOverride(Size finalSize)
    {
        if (Content != null)
        {
            Content.Arrange(new Rect(0, 0, finalSize.Width, finalSize.Height));
        }
    }

    public override void Render(VirtualBuffer buffer, int offsetX, int offsetY)
    {
        if (Content != null)
        {
            Content.Render(buffer, offsetX, offsetY);
        }
    }

    protected override void OnDataContextChanged(object newValue)
    {
        base.OnDataContextChanged(newValue);
        if (Content != null)
        {
            Content.DataContext = newValue;
        }
    }
}
