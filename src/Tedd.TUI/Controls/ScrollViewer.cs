using System;

namespace Tedd.TUI.Controls;

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

    public static readonly DependencyProperty HorizontalScrollBarVisibilityProperty =
        DependencyProperty.RegisterAttached("HorizontalScrollBarVisibility", typeof(ScrollBarVisibility), typeof(ScrollViewer), ScrollBarVisibility.Disabled);

    public static void SetHorizontalScrollBarVisibility(UIElement element, ScrollBarVisibility value)
    {
        if (element == null) throw new ArgumentNullException(nameof(element));
        element.SetValue(HorizontalScrollBarVisibilityProperty, value);
    }

    public static ScrollBarVisibility GetHorizontalScrollBarVisibility(UIElement element)
    {
        if (element == null) throw new ArgumentNullException(nameof(element));
        return (ScrollBarVisibility)element.GetValue(HorizontalScrollBarVisibilityProperty);
    }

    public ScrollBarVisibility HorizontalScrollBarVisibility
    {
        get => (ScrollBarVisibility)GetValue(HorizontalScrollBarVisibilityProperty);
        set => SetValue(HorizontalScrollBarVisibilityProperty, value);
    }

    public static readonly DependencyProperty VerticalScrollBarVisibilityProperty =
        DependencyProperty.RegisterAttached("VerticalScrollBarVisibility", typeof(ScrollBarVisibility), typeof(ScrollViewer), ScrollBarVisibility.Visible);

    public static void SetVerticalScrollBarVisibility(UIElement element, ScrollBarVisibility value)
    {
        if (element == null) throw new ArgumentNullException(nameof(element));
        element.SetValue(VerticalScrollBarVisibilityProperty, value);
    }

    public static ScrollBarVisibility GetVerticalScrollBarVisibility(UIElement element)
    {
        if (element == null) throw new ArgumentNullException(nameof(element));
        return (ScrollBarVisibility)element.GetValue(VerticalScrollBarVisibilityProperty);
    }

    public ScrollBarVisibility VerticalScrollBarVisibility
    {
        get => (ScrollBarVisibility)GetValue(VerticalScrollBarVisibilityProperty);
        set => SetValue(VerticalScrollBarVisibilityProperty, value);
    }

    // Intent: let a surface receive the whole scrolled content instead of the visible slice.
    // Why:
    // - Surfaces that can clip a sub-region on their own (the Blazor DOM grid) can then scroll
    //   without a re-render, and the off-screen text survives for find-in-page and crawlers.
    // Constraints/Invariants:
    // - Opting in is not enough on its own: the surface must also offer a
    //   VirtualBuffer.ScrollPanes channel. Flat surfaces (terminal, canvas) never do, so this
    //   property has no effect there and the ordinary clip path runs unchanged.
    // Failure modes:
    // - Pre-rendering defeats Panel.Render's clip cull, so DOM node count and render cost scale
    //   with content extent rather than viewport area. Set false on viewers over huge content.
    public static readonly DependencyProperty PrerenderContentProperty =
        DependencyProperty.RegisterAttached("PrerenderContent", typeof(bool), typeof(ScrollViewer), true);

    public static void SetPrerenderContent(UIElement element, bool value)
    {
        if (element == null) throw new ArgumentNullException(nameof(element));
        element.SetValue(PrerenderContentProperty, value);
    }

    public static bool GetPrerenderContent(UIElement element)
    {
        if (element == null) throw new ArgumentNullException(nameof(element));
        return (bool)element.GetValue(PrerenderContentProperty);
    }

    /// <summary>
    /// Whether this viewer hands its full content to a surface that can pre-render scroll
    /// regions (see <see cref="PrerenderContentProperty"/>). Defaults to true; surfaces that
    /// don't support it ignore it entirely.
    /// </summary>
    public bool PrerenderContent
    {
        get => (bool)GetValue(PrerenderContentProperty);
        set => SetValue(PrerenderContentProperty, value);
    }

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

    /// <inheritdoc/>
    /// <remarks>
    /// A viewer scrolls its own content in every axis it has not had scrolling
    /// <see cref="ScrollBarVisibility.Disabled"/> on: those axes clamp to the offered
    /// extent, a Disabled one passes the constraint straight through to the content.
    /// </remarks>
    public override bool ScrollsOwnContent(Orientation orientation) =>
        orientation == Orientation.Vertical
            ? VerticalScrollBarVisibility != ScrollBarVisibility.Disabled
            : HorizontalScrollBarVisibility != ScrollBarVisibility.Disabled;

    protected override Size MeasureOverride(Size availableSize)
    {
        ResolveScrollBarsAndMeasureContent(availableSize, out int vScrollWidth, out int hScrollHeight, out Size contentSize);

        if (_showVertical)
        {
            int viewport = Math.Max(1, availableSize.Height - hScrollHeight);
            int extent = contentSize.Height;
            _verticalScrollBar.SetLayoutMetrics(0, Math.Max(0, extent - viewport), viewport);
            _verticalScrollBar.Measure(new Size(vScrollWidth, viewport));
        }

        if (_showHorizontal)
        {
            int viewport = Math.Max(1, availableSize.Width - vScrollWidth);
            int extent = contentSize.Width;
            _horizontalScrollBar.SetLayoutMetrics(0, Math.Max(0, extent - viewport), viewport);
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

        // Content that is a viewport in its own right has no natural extent to report, so
        // it gets the real viewport instead: the frame we will occupy (an explicit
        // Width/Height when set, since a stacking parent hands us infinity on its stack
        // axis) less whatever our own scrollbars take. Handed infinity it would grow to
        // its whole content and leave its own scrollbar dead at Maximum 0, with this
        // viewer scrolling in its place.
        if (_content.ScrollsOwnContent(Orientation.Vertical))
            contentAvailable.Height = Math.Max(0, (Height > 0 ? Height : availableSize.Height) - hScrollHeight);
        if (_content.ScrollsOwnContent(Orientation.Horizontal))
            contentAvailable.Width = Math.Max(0, (Width > 0 ? Width : availableSize.Width) - vScrollWidth);

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
                _verticalScrollBar.SetLayoutMetrics(
                    0,
                    Math.Max(0, extent - viewportH),
                    Math.Max(1, viewportH));
            }

            if (_showHorizontal)
            {
                int extent = _content.DesiredSize.Width;
                _horizontalScrollBar.SetLayoutMetrics(
                    0,
                    Math.Max(0, extent - viewportW),
                    Math.Max(1, viewportW));
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

    // Intent: keep hit testing inside the same window onto Content that rendering draws.
    // Why:
    // - Render clips Content to this rectangle. Without the matching test, a click on the
    //   frame around it (a Border's line and padding gutter, a dialog's title bar) walks
    //   into whichever row the scroll offset happens to put there -- content the user
    //   cannot see, and in a scrolled dialog that is a different control every notch.
    // Constraints/Invariants:
    // - Coordinates are this element's own, so an override must agree with the rectangle
    //   its Render passes to PushClip, inset and all.
    /// <summary>
    /// The rectangle, in this element's own coordinates, that <see cref="Content"/> is
    /// clipped to. Hit testing descends into Content only for points inside it.
    /// </summary>
    protected internal virtual Rect GetContentViewport()
    {
        int vScrollWidth = _showVertical ? 1 : 0;
        int hScrollHeight = _showHorizontal ? 1 : 0;
        return new Rect(0, 0,
            Math.Max(0, RenderSize.Width - vScrollWidth),
            Math.Max(0, RenderSize.Height - hScrollHeight));
    }

    public override void Render(VirtualBuffer buffer, int offsetX, int offsetY)
    {
        int x = RenderSize.X + offsetX;
        int y = RenderSize.Y + offsetY;

        int vScrollWidth = _showVertical ? 1 : 0;
        int hScrollHeight = _showHorizontal ? 1 : 0;
        int viewportW = Math.Max(0, RenderSize.Width - vScrollWidth);
        int viewportH = Math.Max(0, RenderSize.Height - hScrollHeight);

        if (_content != null &&
            !TryRenderContentAsScrollPane(buffer, _content, x, y, viewportW, viewportH,
                                          _horizontalScrollBar.Value, _verticalScrollBar.Value))
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

    // Intent: hand the whole scrolled content to surfaces that can clip a sub-region themselves.
    // Why:
    // - Such a surface (the Blazor DOM grid) can then scroll by translating an already-built
    //   sub-tree, and the off-screen rows survive into its output for find-in-page and crawlers.
    // Constraints/Invariants:
    // - Every viewer in this hierarchy arranges Content at its own content inset -- 0 for
    //   ScrollViewer, border+padding for Border/DialogBox -- and derives its clip rect from the
    //   same inset. Reading the inset off Content.RenderSize therefore keeps the pane's viewport
    //   and the clip path it replaces in agreement by construction.
    // - The pane is seeded with the cell already at the viewport origin, because children that
    //   render with a transparent background read what they sit on via GetPixel (see the
    //   FillRect in Border.Render). A blank pane would change their colors.
    // Failure modes:
    // - Pre-rendering defeats Panel.Render's clip cull, so cost scales with content extent.
    //   Non-overflowing content stays on the clip path, which is cheaper and pixel-identical.
    /// <summary>
    /// Renders <paramref name="content"/> at full extent into a <see cref="ScrollPane"/>
    /// registered on <paramref name="buffer"/>, instead of clipping it to the viewport.
    /// </summary>
    /// <returns>
    /// True when a pane was registered and the caller is done; false when the caller must
    /// take its ordinary <see cref="VirtualBuffer.PushClip"/> path.
    /// </returns>
    protected bool TryRenderContentAsScrollPane(
        VirtualBuffer buffer, UIElement content,
        int x, int y, int viewportW, int viewportH,
        int scrollOffsetX, int scrollOffsetY)
    {
        var panes = buffer.ScrollPanes;
        if (panes == null || !PrerenderContent) return false;
        if (viewportW <= 0 || viewportH <= 0) return false;

        int extentW = content.RenderSize.Width;
        int extentH = content.RenderSize.Height;
        if (extentW <= 0 || extentH <= 0) return false;

        // Content that already fits has nothing to scroll; the clip path produces identical
        // output for less, so leave the common case alone.
        if (extentW <= viewportW && extentH <= viewportH) return false;

        var pane = new VirtualBuffer(extentW, extentH);
        pane.Clear(buffer.GetPixel(x + content.RenderSize.X, y + content.RenderSize.Y).Background);
        if (buffer.Graphics != null) pane.Graphics = new List<GraphicPlacement>();
        pane.ScrollPanes = new List<ScrollPane>(); // lets nested viewers register inside this pane

        // Cancel out the content's arranged inset so it lands at the pane's origin; the pane's
        // viewport carries that inset instead.
        content.Render(pane, -content.RenderSize.X, -content.RenderSize.Y);

        panes.Add(new ScrollPane
        {
            Viewport = new Rect(x + content.RenderSize.X, y + content.RenderSize.Y, viewportW, viewportH),
            Content = pane,
            OffsetX = scrollOffsetX,
            OffsetY = scrollOffsetY,
        });

        return true;
    }

    public override void OnMouseDown(MouseEventArgs e)
    {
        // Hit-test routing for our scrollbars and content children is handled by the
        // standard tree walk in TuiWindow. Parent-bounds check there already prevents
        // clipped descendants of Content from being hit when the click is outside the
        // ScrollViewer's RenderSize, so no extra logic is needed here.
        base.OnMouseDown(e);
    }

    /// <summary>
    /// Rows/columns scrolled per full wheel notch, analogous to the desktop
    /// "wheel scroll lines" system setting.
    /// </summary>
    public static int WheelScrollLines { get; set; } = 3;

    private WheelNotchAccumulator _wheelAccumulator;

    public override void OnMouseWheel(MouseWheelEventArgs e)
    {
        base.OnMouseWheel(e);
        if (e.Handled) return;

        // Wheel over content (a wheel over one of our scrollbars is handled by the bar
        // itself before bubbling here). Vertical scrolling wins when both axes can
        // scroll; a viewer with nothing to scroll lets the event bubble so a nested
        // viewer's ancestor can take it.
        ScrollBar? target = null;
        if (_showVertical && _verticalScrollBar.Maximum > _verticalScrollBar.Minimum)
            target = _verticalScrollBar;
        else if (_showHorizontal && _horizontalScrollBar.Maximum > _horizontalScrollBar.Minimum)
            target = _horizontalScrollBar;

        if (target == null) return;

        int notches = _wheelAccumulator.Add(e.Delta);
        if (notches != 0)
        {
            target.Value -= notches * target.SmallChange * WheelScrollLines;
        }
        e.Handled = true;
    }
}
