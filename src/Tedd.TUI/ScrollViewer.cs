using System;

namespace Tedd.TUI;

/// <summary>
/// Controls when a scrollbar is shown by <see cref="ScrollViewer"/> (or <see cref="Border"/>).
/// </summary>
public enum ScrollBarVisibility
{
    /// <summary>Never show the scrollbar; content is clamped to the viewport in this axis.</summary>
    Disabled,
    /// <summary>Show the scrollbar (and reserve a row/column for it) only when the content overflows.</summary>
    Auto,
    /// <summary>Always show the scrollbar; always reserve a row/column for it.</summary>
    Visible,
}

public class ScrollViewer : UIElement
{
    protected readonly ScrollBar _verticalScrollBar;
    protected readonly ScrollBar _horizontalScrollBar;
    private UIElement? _content;

    // Resolved by MeasureOverride; consumed by ArrangeOverride/Render/HitTest/etc.
    // Necessary because Auto-mode visibility depends on the content's measured size.
    private bool _showVertical;
    private bool _showHorizontal;

    public UIElement? Content
    {
        get => _content;
        set
        {
            if (_content != value)
            {
                _content = value;
                if (_content != null)
                {
                    _content.Parent = this;
                }
                Invalidate();
            }
        }
    }

    public ScrollBarVisibility HorizontalScrollBarVisibility { get; set; } = ScrollBarVisibility.Disabled;
    public ScrollBarVisibility VerticalScrollBarVisibility { get; set; } = ScrollBarVisibility.Visible;

    /// <summary>
    /// True when the vertical scrollbar is currently shown. Resolved during the
    /// last <see cref="MeasureOverride"/>; consumers (e.g. <see cref="Table"/>)
    /// read this to know whether to reserve space for it.
    /// </summary>
    public bool IsVerticalScrollBarShown => _showVertical;

    /// <summary>
    /// True when the horizontal scrollbar is currently shown. Resolved during the
    /// last <see cref="MeasureOverride"/>.
    /// </summary>
    public bool IsHorizontalScrollBarShown => _showHorizontal;

    /// <summary>
    /// Sets the resolved scrollbar visibility flags. Used by subclasses (e.g.
    /// <see cref="Border"/>) that override <see cref="MeasureOverride"/> with their own
    /// resolution logic but need <see cref="VisualChildrenCount"/>, <see cref="GetVisualChild"/>,
    /// and the <c>IsXxxScrollBarShown</c> properties to reflect the same decision.
    /// </summary>
    protected void SetResolvedScrollBarVisibility(bool showVertical, bool showHorizontal)
    {
        _showVertical = showVertical;
        _showHorizontal = showHorizontal;
    }

    public ScrollViewer()
    {
        _verticalScrollBar = new ScrollBar { Orientation = Orientation.Vertical, Minimum = 0, Maximum = 0, Value = 0 };
        _horizontalScrollBar = new ScrollBar { Orientation = Orientation.Horizontal, Minimum = 0, Maximum = 0, Value = 0 };

        _verticalScrollBar.Parent = this;
        _horizontalScrollBar.Parent = this;

        _verticalScrollBar.ValueChanged += OnScroll;
        _horizontalScrollBar.ValueChanged += OnScroll;
    }

    public int VerticalOffset => _verticalScrollBar.Value;
    public int HorizontalOffset => _horizontalScrollBar.Value;

    public void ScrollToVerticalOffset(int offset)
    {
        _verticalScrollBar.Value = offset;
        Invalidate();
    }

    public void ScrollToHorizontalOffset(int offset)
    {
        _horizontalScrollBar.Value = offset;
        Invalidate();
    }

    private void OnScroll(object? sender, EventArgs e)
    {
        Invalidate();
    }

    public override int VisualChildrenCount
    {
        get
        {
            int count = 0;
            if (_content != null) count++;
            if (_showVertical) count++;
            if (_showHorizontal) count++;
            return count;
        }
    }

    public override UIElement GetVisualChild(int index)
    {
        // Order: Content, VScroll, HScroll. Mirrors the order used in Render so
        // hit-testing visits the visible scrollbars only when they are shown.
        if (_content != null)
        {
            if (index == 0) return _content;
            index--;
        }

        if (_showVertical)
        {
            if (index == 0) return _verticalScrollBar;
            index--;
        }

        if (_showHorizontal)
        {
            if (index == 0) return _horizontalScrollBar;
        }

        throw new ArgumentOutOfRangeException(nameof(index));
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        ResolveScrollBarsAndMeasureContent(availableSize, out int vScrollWidth, out int hScrollHeight, out Size contentSize);

        if (_showVertical)
        {
            int viewport = Math.Max(1, availableSize.Height - hScrollHeight);
            int extent = contentSize.Height;
            _verticalScrollBar.ViewportSize = viewport;
            _verticalScrollBar.Maximum = Math.Max(0, extent - viewport);
            _verticalScrollBar.Minimum = 0;
            _verticalScrollBar.Measure(new Size(vScrollWidth, viewport));
        }

        if (_showHorizontal)
        {
            int viewport = Math.Max(1, availableSize.Width - vScrollWidth);
            int extent = contentSize.Width;
            _horizontalScrollBar.ViewportSize = viewport;
            _horizontalScrollBar.Maximum = Math.Max(0, extent - viewport);
            _horizontalScrollBar.Minimum = 0;
            _horizontalScrollBar.Measure(new Size(viewport, hScrollHeight));
        }

        // Return bounded size (requested size up to available)
        return new Size(
             Math.Min(availableSize.Width, contentSize.Width + vScrollWidth),
             Math.Min(availableSize.Height, contentSize.Height + hScrollHeight)
        );
    }

    /// <summary>
    /// Resolves <see cref="_showVertical"/>/<see cref="_showHorizontal"/> for the given
    /// available size and measures the content with the appropriate constraints.
    ///
    /// Two-pass logic: first reserve space only for forced-Visible scrollbars and measure.
    /// Then promote any Auto axis to "shown" if its content overflows. Finally, if showing
    /// one scrollbar made the other axis overflow, re-promote (only once -- the second pass
    /// can't change the answer because reserving space only ever shrinks the viewport).
    /// </summary>
    private void ResolveScrollBarsAndMeasureContent(Size availableSize, out int vScrollWidth, out int hScrollHeight, out Size contentSize)
    {
        bool vForced = VerticalScrollBarVisibility == ScrollBarVisibility.Visible;
        bool hForced = HorizontalScrollBarVisibility == ScrollBarVisibility.Visible;
        bool vAuto = VerticalScrollBarVisibility == ScrollBarVisibility.Auto;
        bool hAuto = HorizontalScrollBarVisibility == ScrollBarVisibility.Auto;

        bool showV = vForced;
        bool showH = hForced;

        // Pass 1: measure with reservations only for forced-Visible scrollbars.
        contentSize = MeasureContent(availableSize, showV, showH);

        // Pass 2: promote Auto axes to "shown" if content overflows the viewport.
        if (vAuto && contentSize.Height > Math.Max(0, availableSize.Height - (showH ? 1 : 0)))
            showV = true;
        if (hAuto && contentSize.Width > Math.Max(0, availableSize.Width - (showV ? 1 : 0)))
            showH = true;

        // Pass 3: showing one scrollbar may have caused the other axis to start overflowing.
        // Re-measure and re-check Auto axes once. (Reserving space only shrinks the
        // viewport, so a single iteration is sufficient.)
        if (showV != vForced || showH != hForced)
        {
            contentSize = MeasureContent(availableSize, showV, showH);
            if (vAuto && !showV && contentSize.Height > Math.Max(0, availableSize.Height - (showH ? 1 : 0)))
            {
                showV = true;
                contentSize = MeasureContent(availableSize, showV, showH);
            }
            if (hAuto && !showH && contentSize.Width > Math.Max(0, availableSize.Width - (showV ? 1 : 0)))
            {
                showH = true;
                contentSize = MeasureContent(availableSize, showV, showH);
            }
        }

        _showVertical = showV;
        _showHorizontal = showH;

        vScrollWidth = showV ? 1 : 0;
        hScrollHeight = showH ? 1 : 0;
    }

    /// <summary>
    /// Measures <see cref="_content"/> for a given resolved scrollbar configuration.
    /// When an axis is allowed to scroll (Auto or Visible), the content is given
    /// <see cref="int.MaxValue"/> in that axis so it can report its natural extent.
    /// When an axis is Disabled, the content is clamped to the viewport.
    /// </summary>
    private Size MeasureContent(Size availableSize, bool showV, bool showH)
    {
        if (_content == null) return new Size(0, 0);

        int vScrollWidth = showV ? 1 : 0;
        int hScrollHeight = showH ? 1 : 0;

        Size contentAvailable = new Size(
            Math.Max(0, availableSize.Width - vScrollWidth),
            Math.Max(0, availableSize.Height - hScrollHeight));

        if (VerticalScrollBarVisibility != ScrollBarVisibility.Disabled)
            contentAvailable.Height = int.MaxValue;
        if (HorizontalScrollBarVisibility != ScrollBarVisibility.Disabled)
            contentAvailable.Width = int.MaxValue;

        _content.Measure(contentAvailable);
        return _content.DesiredSize;
    }

    protected override void ArrangeOverride(Size finalSize)
    {
        int vScrollWidth = _showVertical ? 1 : 0;
        int hScrollHeight = _showHorizontal ? 1 : 0;

        int viewportW = Math.Max(0, finalSize.Width - vScrollWidth);
        int viewportH = Math.Max(0, finalSize.Height - hScrollHeight);

        // Update ScrollBars max/viewport based on final arranged viewport
        if (_content != null)
        {
            if (_showVertical)
            {
                int extent = _content.DesiredSize.Height;
                _verticalScrollBar.ViewportSize = Math.Max(1, viewportH);
                _verticalScrollBar.Maximum = Math.Max(0, extent - viewportH);
            }

            if (_showHorizontal)
            {
                int extent = _content.DesiredSize.Width;
                _horizontalScrollBar.ViewportSize = Math.Max(1, viewportW);
                _horizontalScrollBar.Maximum = Math.Max(0, extent - viewportW);
            }
        }

        if (_content != null)
        {
            // When an axis is Disabled, clamp the content's arrange rect to the viewport
            // so wrappable children (e.g. Paragraph) don't reflow against an oversized
            // arrange width pulled from a non-wrapping sibling like CodeDocument.
            int arrangeW = (HorizontalScrollBarVisibility == ScrollBarVisibility.Disabled)
                ? viewportW
                : Math.Max(viewportW, _content.DesiredSize.Width);
            int arrangeH = (VerticalScrollBarVisibility == ScrollBarVisibility.Disabled)
                ? viewportH
                : Math.Max(viewportH, _content.DesiredSize.Height);

            _content.Arrange(new Rect(0, 0, arrangeW, arrangeH));
        }

        if (_showVertical)
        {
            _verticalScrollBar.Arrange(new Rect(finalSize.Width - vScrollWidth, 0, vScrollWidth, viewportH));
        }

        if (_showHorizontal)
        {
            _horizontalScrollBar.Arrange(new Rect(0, finalSize.Height - hScrollHeight, viewportW, hScrollHeight));
        }
    }

    public override void Render(VirtualBuffer buffer, int offsetX, int offsetY)
    {
        int x = RenderSize.X + offsetX;
        int y = RenderSize.Y + offsetY;

        int vScrollWidth = _showVertical ? 1 : 0;
        int hScrollHeight = _showHorizontal ? 1 : 0;
        int viewportW = Math.Max(0, RenderSize.Width - vScrollWidth);
        int viewportH = Math.Max(0, RenderSize.Height - hScrollHeight);

        if (_content != null)
        {
            buffer.PushClip(new Rect(x, y, viewportW, viewportH));

            // Pass the absolute position of this ScrollViewer minus scroll offset.
            // The child's RenderSize.X/Y is 0 (we Arrange it at origin), so its absolute
            // render position becomes (x - hOffset, y - vOffset).
            _content.Render(buffer, x - _horizontalScrollBar.Value, y - _verticalScrollBar.Value);

            buffer.PopClip();
        }

        if (_showVertical)
        {
            _verticalScrollBar.Render(buffer, x, y);
        }

        if (_showHorizontal)
        {
            _horizontalScrollBar.Render(buffer, x, y);
        }
    }

    public override void OnMouseDown(MouseEventArgs e)
    {
        // Hit-test routing for our scrollbars and content children is handled by the
        // standard tree walk in TuiWindow. Parent-bounds check there already prevents
        // clipped descendants of Content from being hit when the click is outside the
        // ScrollViewer's RenderSize, so no extra logic is needed here.
        base.OnMouseDown(e);
    }
}
