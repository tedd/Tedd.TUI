using System;

namespace Tedd.TUI;

public class ScrollViewer : UIElement
{
    protected readonly ScrollBar _verticalScrollBar;
    protected readonly ScrollBar _horizontalScrollBar;
    private UIElement? _content;

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

    public bool HorizontalScrollBarVisibility { get; set; } = false; // By default hidden? Or Auto? Let's say explicit for now.
    public bool VerticalScrollBarVisibility { get; set; } = true;

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
            if (VerticalScrollBarVisibility) count++;
            if (HorizontalScrollBarVisibility) count++;
            return count;
        }
    }

    public override UIElement GetVisualChild(int index)
    {
        // Simple mapping, order: Content, VScroll, HScroll
        if (_content != null)
        {
            if (index == 0) return _content;
            index--;
        }

        if (VerticalScrollBarVisibility)
        {
            if (index == 0) return _verticalScrollBar;
            index--;
        }

        if (HorizontalScrollBarVisibility)
        {
            if (index == 0) return _horizontalScrollBar;
        }

        throw new ArgumentOutOfRangeException(nameof(index));
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        // 1. Measure content with infinite space in scrolling directions
        // If Vertical scroll enabled, height is infinite.
        // If Horizontal scroll enabled, width is infinite.

        Size contentAvailable = availableSize;
        if (VerticalScrollBarVisibility) contentAvailable.Height = int.MaxValue;
        if (HorizontalScrollBarVisibility) contentAvailable.Width = int.MaxValue;

        // Reserve space for scrollbars if they are going to be visible?
        // Basic logic: Measure content, see if it fits. If not, show scrollbars.
        // For now, simple logic: If Visibility is True, reserve space.

        int vScrollWidth = VerticalScrollBarVisibility ? 1 : 0;
        int hScrollHeight = HorizontalScrollBarVisibility ? 1 : 0;

        contentAvailable.Width = Math.Max(0, contentAvailable.Width - vScrollWidth);
        contentAvailable.Height = Math.Max(0, contentAvailable.Height - hScrollHeight);

        Size contentSize = new Size(0, 0);
        if (_content != null)
        {
            _content.Measure(contentAvailable);
            contentSize = _content.DesiredSize;
        }

        // 2. Setup ScrollBars
        if (VerticalScrollBarVisibility)
        {
            int viewport = Math.Max(1, availableSize.Height - hScrollHeight);
            int extent = contentSize.Height;
            _verticalScrollBar.ViewportSize = viewport;
            _verticalScrollBar.Maximum = Math.Max(0, extent - viewport);
            _verticalScrollBar.Minimum = 0;
            _verticalScrollBar.Measure(new Size(vScrollWidth, viewport));
        }

        if (HorizontalScrollBarVisibility)
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

    protected override void ArrangeOverride(Size finalSize)
    {
        int vScrollWidth = VerticalScrollBarVisibility ? 1 : 0;
        int hScrollHeight = HorizontalScrollBarVisibility ? 1 : 0;

        int viewportW = Math.Max(0, finalSize.Width - vScrollWidth);
        int viewportH = Math.Max(0, finalSize.Height - hScrollHeight);

        // Update ScrollBars max/viewport based on final arranged viewport
        if (_content != null)
        {
            if (VerticalScrollBarVisibility)
            {
                int extent = _content.DesiredSize.Height;
                _verticalScrollBar.ViewportSize = Math.Max(1, viewportH);
                _verticalScrollBar.Maximum = Math.Max(0, extent - viewportH);
            }

            if (HorizontalScrollBarVisibility)
            {
                int extent = _content.DesiredSize.Width;
                _horizontalScrollBar.ViewportSize = Math.Max(1, viewportW);
                _horizontalScrollBar.Maximum = Math.Max(0, extent - viewportW);
            }
        }

        // Arrange Content
        // We arrange it at (0,0) relative to us?
        // Or do we modify render offset?
        // Typically Arrange sets the slot. 
        // If we want clipping, we Arrange it to its DesiredSize (larger than viewport).
        // Then in Render, we use clip and offset.
        if (_content != null)
        {
            // Give it what it wants, so it renders fully (internally) or as much as it wants.
            // But we must place it.
            // We can place it at -ScrollOffset?
            // No, Arrange expects relative coordinates to Parent.
            // If we place at negative coords, they might be clipped by system if system clipped parent?
            // TUI Layout: Render uses x = RenderSize.X + offsetX.
            // If we Arrange at (0,0), RenderSize.X is 0.
            // In Render(), we will pass (x - ScrollValues, y - ScrollValues).
            _content.Arrange(new Rect(0, 0, Math.Max(viewportW, _content.DesiredSize.Width), Math.Max(viewportH, _content.DesiredSize.Height)));
        }

        // Arrange ScrollBars
        if (VerticalScrollBarVisibility)
        {
            _verticalScrollBar.Arrange(new Rect(finalSize.Width - vScrollWidth, 0, vScrollWidth, viewportH));
        }

        if (HorizontalScrollBarVisibility)
        {
            _horizontalScrollBar.Arrange(new Rect(0, finalSize.Height - hScrollHeight, viewportW, hScrollHeight));
        }
    }

    public override void Render(VirtualBuffer buffer, int offsetX, int offsetY)
    {
        int x = RenderSize.X + offsetX;
        int y = RenderSize.Y + offsetY;

        int vScrollWidth = VerticalScrollBarVisibility ? 1 : 0;
        int hScrollHeight = HorizontalScrollBarVisibility ? 1 : 0;
        int viewportW = Math.Max(0, RenderSize.Width - vScrollWidth);
        int viewportH = Math.Max(0, RenderSize.Height - hScrollHeight);

        // Render Content with Clip
        if (_content != null)
        {
            // Push Clip
            buffer.PushClip(new Rect(x, y, viewportW, viewportH));

            int contentX = x - _horizontalScrollBar.Value;
            int contentY = y - _verticalScrollBar.Value;

            // We need to render the content at shifted position.
            // But _content.Render uses its RenderSize.X/Y which we set to 0 in Arrange.
            // So _content.Render(buffer, contentX, contentY) works if Arrange X,Y were 0.
            // Note: Render(buffer, offX, offY) calculates pos = RenderSize.X + offX.
            // Since RenderSize.X is 0 (relative to ScrollViewer), pos becomes 0 + contentX.
            // Correct.

            // We pass the global offset (x, y) adjusted by scroll.
            // Wait, Render(buffer, offsetX, offsetY).
            // Parent calls ScrollViewer.Render(buffer, parentX, parentY).
            // ScrollViewer.Render calls _content.Render(..., x - scroll, y - scroll).
            // _content.Render adds its RenderSize.X (0).
            // Result: parentX + (x - scroll) = wrong?
            // x IS (RenderSize.X + offsetX). i.e. Absolute X of ScrollViewer.
            // We want absolute X of content to be AbsoluteX_SV - Scroll.
            // Param of Render is "offsetX".
            // _content.Render(buffer, currentOffsetX, currentOffsetY).
            // absolute = _content.RenderSize.X + currentOffsetX.
            // We want absolute = x - scroll.
            // 0 + currentOffsetX = x - scroll.
            // currentOffsetX = x - scroll.
            // So we pass (x - scroll, y - scroll).

            // X is absolute position of ScrollViewer TopLeft.
            // So we pass that absolute position minus scroll.

            // Wait, does Render take "Offset from Parent" or "Absolute Offset"?
            // TUIWindow.Render calls Content.Render(buffer, 0, 0).
            // UIElement.Render calculates x = RenderSize.X + offsetX.
            // If TuiWindow.Content is at 0,0. x = 0+0 = 0.
            // If TuiWindow calls Render(buffer, 10, 10). x = 0+10 = 10.
            // So offsetX/Y are cumulative offsets (usually 0 if RenderSize is absolute?).
            // If RenderSize is relative to Parent, then offsetX/Y must be absolute position of Parent?
            // Check TUIWindow.Arrange: Content.Arrange(0,0...).
            // ScrollViewer.Arrange: Content.Arrange(0,0...).

            // So RenderSize is relative to Parent.
            // UIElement.Render(buffer, offsetX, offsetY) -> x = RelativeX + offsetX.
            // Meaning offsetX must be "Absolute position of Parent".
            // Yes.

            // So inside ScrollViewer.Render:
            // x (Absolute SV X) = RenderSize.X (Relative SV X) + offsetX (Absolute Parent X).
            // We want to verify this.

            // So for Child Content:
            // We call _content.Render(buffer, absolute_SV_X - scrollX, absolute_SV_Y - scrollY).
            // Then child calculates: child_abs_X = child.RentX (0) + (absolute_SV_X - scrollX).
            // = absolute_SV_X - scrollX. Correct.

            _content.Render(buffer, x - _horizontalScrollBar.Value, y - _verticalScrollBar.Value);

            buffer.PopClip();
        }

        // Render ScrollBars (no scroll offset, no clip usually, or clip to SV bounds)
        if (VerticalScrollBarVisibility)
        {
            // They are children, so they need Absolute Parent X (which is x).
            _verticalScrollBar.Render(buffer, x, y);
        }

        if (HorizontalScrollBarVisibility)
        {
            _horizontalScrollBar.Render(buffer, x, y);
        }
    }

    public override void OnMouseDown(MouseEventArgs e)
    {
        // Hit test and dispatch?
        // UIElement.OnMouseDown is called if this element was hit.
        // But InputHitTest logic usually finds the deepest leaf.
        // If we added ScrollBars as children (GetVisualChild), HitTestRecursive might find them.
        // If it finds ScrollBar, it calls ScrollBar.OnMouseDown directly.
        // If it finds Content, it calls Content.OnMouseDown.

        // HOWEVER, TuiWindow.InputHitTestRecursive likely returns ScrollViewer if Content doesn't handle it?
        // Or if we are the container.

        // If the user clicks on ScrollBar, TuiWindow should route it there IF we exposed them in GetVisualChild.
        // We did expose them.

        // So we might not need to do anything here if HitTest works.
        // But wait, if Content is clipped, HitTest must respect clip!
        // TuiWindow.HitTestRecursive:
        // if (x >= element.RenderSize.X ... )

        // It does NOT respect clipping automatically.
        // We need to override HitTest or rely on TuiWindow being smart? TuiWindow is simple.
        // If we click outside the viewport (on a scrolled out button),
        // The button's RenderSize/X/Y relative to ScrollViewer might be negative or huge.
        // HitTest logic:
        // localX = x - element.RenderSize.X.
        // If Button is at (0, -10) relative to SV.
        // Click at (5, 5) relative to SV.
        // Button HitTest: localX = 5 - 0 = 5. localY = 5 - (-10) = 15.
        // If Button contains (5, 15), it returns Button.
        // BUT visually it is clipped.
        // We MUST prevent HitTest from finding clipped items.

        // We can't easily interface with HitTest since it's in TuiWindow (or recursive).
        // Check TuiWindow.InputHitTestRecursive:
        // It iterates children.

        // Ideally ScrollViewer should override HitTest or we update TuiWindow to respect Valid/Clipped area?
        // OR, simpler:
        // We assume HitTest finds the child.
        // But if the child is outside the ScrollViewer bounds, we shouldn't interact.
        // But HitTest starts at Root.
        // If I click at (100, 100), and ScrollViewer is at (0,0) sized 50x50.
        // InputHitTestRecursive checks ScrollViewer bounds.
        // If (100,100) is outside SV, it returns null (or checks siblings).
        // So global clip is handled by Parent check.

        // But checking children INSIDE ScrollViewer:
        // SV is 50x50. Child is at (0, 0) size 50x200.
        // Click at (10, 40). Inside SV. Check Child.
        // Child at (0,0) contains (10,40). Return Child.
        // Click at (10, 60). Outside SV.
        // SV check: y=60 > height=50. SV returns null.
        // So Child is not checked.

        // ISSUE: If Child is at (0, -20).
        // Click at (10, 10). Inside SV.
        // Check Child. localY = 10 - (-20) = 30.
        // Child contains (10, 30).
        // Visually: The point (10,30) of Child is at screen (10,10). Visible.
        // Correct.

        // So standard HitTest works fine for "Clipped by Parent Bounds" logic.
        // Because Parent Bounds check happens before checking children.

        base.OnMouseDown(e);
    }
}
