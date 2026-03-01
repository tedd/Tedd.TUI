using System;

namespace Tedd.TUI;

public class Border : ScrollViewer
{
    // Wrapper for backward compatibility with existing Border usage
    public UIElement Child
    {
        get => Content;
        set => Content = value;
    }

    public UIElement Title
    {
        get;
        set
        {
            if (field != value)
            {
                if (field != null) field.Parent = null;
                field = value;
                if (field != null) field.Parent = this;
                Invalidate();
            }
        }
    }

    public HorizontalAlignment TitleAlignment
    {
        get;
        set
        {
            if (field != value)
            {
                field = value;
                Invalidate();
            }
        }
    } = HorizontalAlignment.Left;

    public UIElement StatusBar
    {
        get;
        set
        {
            if (field != value)
            {
                if (field != null) field.Parent = null;
                field = value;
                if (field != null) field.Parent = this;
                Invalidate();
            }
        }
    }

    public static readonly DependencyProperty BorderColorProperty =
        DependencyProperty.Register("BorderColor", typeof(ConsoleColor), typeof(Border), ConsoleColor.White);

    public ConsoleColor BorderColor
    {
        get { return (ConsoleColor)GetValue(BorderColorProperty); }
        set { SetValue(BorderColorProperty, value); }
    }

    public static readonly DependencyProperty BoxStyleProperty =
        DependencyProperty.Register("BoxStyle", typeof(BoxStyle), typeof(Border), BoxStyle.Single);

    public BoxStyle BoxStyle
    {
        get { return (BoxStyle)GetValue(BoxStyleProperty); }
        set { SetValue(BoxStyleProperty, value); }
    }

    public int VerticalScrollBarMarginTop
    {
        get;
        set
        {
            if (field != value)
            {
                field = value;
                Invalidate();
            }
        }
    } = 0;

    public int VerticalScrollBarMarginBottom
    {
        get;
        set
        {
            if (field != value)
            {
                field = value;
                Invalidate();
            }
        }
    } = 0;

    public int HorizontalScrollBarMarginLeft
    {
        get;
        set
        {
            if (field != value)
            {
                field = value;
                Invalidate();
            }
        }
    } = 0;

    public int HorizontalScrollBarMarginRight
    {
        get;
        set
        {
            if (field != value)
            {
                field = value;
                Invalidate();
            }
        }
    } = 0;

    public Border()
    {
        UpdateScrollBarStyle();
    }

    protected override void OnDataContextChanged(object newValue)
    {
        base.OnDataContextChanged(newValue);
        if (Title != null) Title.DataContext = newValue;
        if (StatusBar != null) StatusBar.DataContext = newValue;
    }

    public override int VisualChildrenCount
    {
        get
        {
            int count = base.VisualChildrenCount;
            if (Title != null) count++;
            if (StatusBar != null) count++;
            return count;
        }
    }

    public override UIElement GetVisualChild(int index)
    {
        int baseCount = base.VisualChildrenCount;
        if (index < baseCount)
        {
            return base.GetVisualChild(index);
        }
        index -= baseCount;

        if (Title != null)
        {
            if (index == 0) return Title;
            index--;
        }

        if (StatusBar != null)
        {
            if (index == 0) return StatusBar;
        }

        throw new ArgumentOutOfRangeException(nameof(index));
    }

    private void UpdateScrollBarStyle()
    {
        var chars = BoxDrawingChars.Get(BoxStyle);

        // Use border characters for the track to make it seamless
        _verticalScrollBar.TrackChar = chars.Vertical;
        _verticalScrollBar.Foreground = BorderColor;
        _verticalScrollBar.Background = null; // Transparent background

        _horizontalScrollBar.TrackChar = chars.Horizontal;
        _horizontalScrollBar.Foreground = BorderColor;
        _horizontalScrollBar.Background = null;
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        // Update styles on measure to ensure they match current properties
        UpdateScrollBarStyle();

        int borderW = 2;
        int borderH = 2;

        // Measure Title and StatusBar
        // They are constrained by Width - corners
        Size decorationAvailable = new Size(Math.Max(0, availableSize.Width - borderW), 1);

        if (Title != null)
        {
            Title.Measure(decorationAvailable);
        }

        if (StatusBar != null)
        {
            StatusBar.Measure(decorationAvailable);
        }

        // Measure Content
        // Content area is inside the border.
        Size contentAvailable = new Size(Math.Max(0, availableSize.Width - borderW), Math.Max(0, availableSize.Height - borderH));

        if (VerticalScrollBarVisibility) contentAvailable.Height = int.MaxValue;
        if (HorizontalScrollBarVisibility) contentAvailable.Width = int.MaxValue;

        Size contentSize = new Size(0, 0);
        if (Content != null)
        {
            Content.Measure(contentAvailable);
            contentSize = Content.DesiredSize;
        }

        // Setup ScrollBars based on Content Size vs Viewport Size
        if (VerticalScrollBarVisibility)
        {
            int viewport = Math.Max(1, availableSize.Height - borderH);
            int extent = contentSize.Height;
            _verticalScrollBar.ViewportSize = viewport;
            _verticalScrollBar.Maximum = Math.Max(0, extent - viewport);
            _verticalScrollBar.Minimum = 0;

            // Measure ScrollBar with margins taken into account
            int vScrollHeight = Math.Max(0, availableSize.Height - borderH - VerticalScrollBarMarginTop - VerticalScrollBarMarginBottom);
            _verticalScrollBar.Measure(new Size(1, vScrollHeight));
        }

        if (HorizontalScrollBarVisibility)
        {
            int viewport = Math.Max(1, availableSize.Width - borderW);
            int extent = contentSize.Width;
            _horizontalScrollBar.ViewportSize = viewport;
            _horizontalScrollBar.Maximum = Math.Max(0, extent - viewport);
            _horizontalScrollBar.Minimum = 0;

            // Measure ScrollBar with margins taken into account
            int hScrollWidth = Math.Max(0, availableSize.Width - borderW - HorizontalScrollBarMarginLeft - HorizontalScrollBarMarginRight);
            _horizontalScrollBar.Measure(new Size(hScrollWidth, 1));
        }

        // Desired size is content size + border, bounded by available
        return new Size(
            Math.Min(availableSize.Width, contentSize.Width + borderW),
            Math.Min(availableSize.Height, contentSize.Height + borderH)
        );
    }

    protected override void ArrangeOverride(Size finalSize)
    {
        int w = finalSize.Width;
        int h = finalSize.Height;

        // Arrange Content inside border
        if (Content != null)
        {
            int viewportW = Math.Max(0, w - 2);
            int viewportH = Math.Max(0, h - 2);
            // Arrange at (1,1) to account for border thickness.
            Content.Arrange(new Rect(1, 1, Math.Max(viewportW, Content.DesiredSize.Width), Math.Max(viewportH, Content.DesiredSize.Height)));
        }

        // Arrange Title
        if (Title != null)
        {
            int titleW = Math.Min(w - 2, Title.DesiredSize.Width);
            int titleX = 1;

            if (TitleAlignment == HorizontalAlignment.Center)
                titleX = Math.Max(1, (w - titleW) / 2);
            else if (TitleAlignment == HorizontalAlignment.Right)
                titleX = Math.Max(1, w - 1 - titleW);

            Title.Arrange(new Rect(titleX, 0, titleW, 1));
        }

        // Arrange StatusBar
        int statusW = 0;
        if (StatusBar != null)
        {
            statusW = Math.Min(w - 2, StatusBar.DesiredSize.Width);
            StatusBar.Arrange(new Rect(1, h - 1, statusW, 1));
        }

        // Arrange ScrollBars
        // Vertical on right edge
        if (VerticalScrollBarVisibility)
        {
            int vTop = 1 + VerticalScrollBarMarginTop;
            int vHeight = Math.Max(0, h - 2 - VerticalScrollBarMarginTop - VerticalScrollBarMarginBottom);
            _verticalScrollBar.Arrange(new Rect(w - 1, vTop, 1, vHeight));
        }

        // Horizontal on bottom edge
        if (HorizontalScrollBarVisibility)
        {
            // If StatusBar exists, HScroll starts after it
            int hLeft = 1 + HorizontalScrollBarMarginLeft + statusW;
            // Ensure we don't overlap right corner
            int hWidth = Math.Max(0, w - 1 - hLeft - HorizontalScrollBarMarginRight);

            _horizontalScrollBar.Arrange(new Rect(hLeft, h - 1, hWidth, 1));
        }
    }

    public override void Render(VirtualBuffer buffer, int offsetX, int offsetY)
    {
        int w = RenderSize.Width;
        int h = RenderSize.Height;
        int x = RenderSize.X + offsetX;
        int y = RenderSize.Y + offsetY;
        ConsoleColor c = BorderColor;
        ConsoleColor bg = Background ?? ConsoleColor.Black;

        if (w < 2 || h < 2) return;

        var chars = BoxDrawingChars.Get(BoxStyle);

        // 1. Draw Border Lines
        // Corners
        buffer.SetPixel(x, y, chars.TopLeft, c, bg);
        buffer.SetPixel(x + w - 1, y, chars.TopRight, c, bg);
        buffer.SetPixel(x, y + h - 1, chars.BottomLeft, c, bg);
        buffer.SetPixel(x + w - 1, y + h - 1, chars.BottomRight, c, bg);

        // Horizontal Top
        buffer.DrawHLine(x + 1, y, w - 2, chars.Horizontal, c, bg);

        // Horizontal Bottom
        buffer.DrawHLine(x + 1, y + h - 1, w - 2, chars.Horizontal, c, bg);

        // Vertical Left
        buffer.DrawVLine(x, y + 1, h - 2, chars.Vertical, c, bg);

        // Vertical Right
        buffer.DrawVLine(x + w - 1, y + 1, h - 2, chars.Vertical, c, bg);

        // 2. Render Children

        // Title (on top border)
        if (Title != null)
        {
            Title.Render(buffer, x, y);
        }

        // StatusBar (on bottom border)
        if (StatusBar != null)
        {
            StatusBar.Render(buffer, x, y);
        }

        // ScrollBars (on border lines)
        if (VerticalScrollBarVisibility)
            _verticalScrollBar.Render(buffer, x, y);

        if (HorizontalScrollBarVisibility)
            _horizontalScrollBar.Render(buffer, x, y);

        // Content (inside border)
        if (Content != null)
        {
            // Clip to inside border area
            buffer.PushClip(new Rect(x + 1, y + 1, w - 2, h - 2));

            // Render content at absolute position - scrollOffset
            // (Content.RenderSize already includes the (1,1) offset from Arrange)
            Content.Render(buffer, x - _horizontalScrollBar.Value, y - _verticalScrollBar.Value);

            buffer.PopClip();
        }
    }
}
