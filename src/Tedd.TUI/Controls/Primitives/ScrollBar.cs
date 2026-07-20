using System;

namespace Tedd.TUI.Controls.Primitives;

// Intent: Provide a scrollbar control for navigation within a range (scrolling content).
// Why:
// - Enable scrolling for large content (Lists, Text, potentially Tables).
// - Provide visual feedback of position and proportion (thumb size).
// Constraints/Invariants:
// - Value is always clamped between Minimum and Maximum.
// - Rendered thumb size is proportional if ViewportSize is set.
// Verification:
// - Verified manually in Demo app ("Scroll" tab).
public class ScrollBar : UIElement
{
    public ScrollBar()
    {
        // Default size?
    }

    public int Value
    {
        get;
        set
        {
            int newVal = Math.Clamp(value, Minimum, Maximum);
            if (field != newVal)
            {
                field = newVal;
                ValueChanged?.Invoke(this, EventArgs.Empty);
                Invalidate();
            }
        }
    }

    public int Minimum
    {
        get;
        set
        {
            if (field != value)
            {
                field = value;
                // Re-clamp value?
                Value = Value; // will clamp
                Invalidate();
            }
        }
    } = 0;

    public int Maximum
    {
        get;
        set
        {
            if (field != value)
            {
                field = value;
                Value = Value; // will clamp
                Invalidate();
            }
        }
    } = 100;

    public int SmallChange { get; set; } = 1;
    public int LargeChange { get; set; } = 10;
    public int ViewportSize { get; set; } = 1;

    public Orientation Orientation { get; set; } = Orientation.Vertical;

    public char TrackChar { get; set; } = '░';
    public char ThumbChar { get; set; } = '█';
    public char UpArrowChar { get; set; } = '▲';
    public char DownArrowChar { get; set; } = '▼';
    public char LeftArrowChar { get; set; } = '◄';
    public char RightArrowChar { get; set; } = '►';

    public event EventHandler ValueChanged;

    protected override Size MeasureOverride(Size availableSize)
    {
        if (Orientation == Orientation.Vertical)
        {
            return new Size(1, availableSize.Height);
        }
        else
        {
            return new Size(availableSize.Width, 1);
        }
    }

    public override void Render(VirtualBuffer buffer, int offsetX, int offsetY)
    {
        int x = RenderSize.X + offsetX;
        int y = RenderSize.Y + offsetY;
        int w = RenderSize.Width;
        int h = RenderSize.Height;

        char arrow1 = (Orientation == Orientation.Vertical) ? UpArrowChar : LeftArrowChar;
        char arrow2 = (Orientation == Orientation.Vertical) ? DownArrowChar : RightArrowChar;
        char trackChar = TrackChar;
        char thumbChar = ThumbChar;

        TuiColor fg = Foreground;
        TuiColor bg = Background ?? buffer.GetPixel(x, y).Background;

        int trackLen = (Orientation == Orientation.Vertical) ? h : w;

        if (trackLen < 2) return;

        // Draw Arrows
        buffer.SetPixel(x, y, arrow1, fg, bg);
        if (Orientation == Orientation.Vertical)
            buffer.SetPixel(x, y + h - 1, arrow2, fg, bg);
        else
            buffer.SetPixel(x + w - 1, y, arrow2, fg, bg);

        int innerLen = trackLen - 2;
        if (innerLen > 0)
        {
            // Calculate thumb size and position
            // Range of values is [Minimum, Maximum]
            long range = (long)Maximum - Minimum;
            long contentSize = range + ViewportSize;

            int thumbSize = 1;
            if (contentSize > 0)
                thumbSize = (int)Math.Max(1, (long)innerLen * ViewportSize / contentSize);

            // Cap thumb size to innerLen
            if (thumbSize > innerLen) thumbSize = innerLen;

            int availableSlide = innerLen - thumbSize;
            int thumbPos = 0;
            if (range > 0)
            {
                thumbPos = (int)((long)availableSlide * (Value - Minimum) / range);
            }

            // Draw Track and Thumb
            for (int i = 0; i < innerLen; i++)
            {
                int px = (Orientation == Orientation.Vertical) ? x : x + 1 + i;
                int py = (Orientation == Orientation.Vertical) ? y + 1 + i : y;

                if (i >= thumbPos && i < thumbPos + thumbSize)
                    buffer.SetPixel(px, py, thumbChar, fg, bg);
                else
                    buffer.SetPixel(px, py, trackChar, fg, bg);
            }
        }
    }

    private bool _isDragging;
    private double _dragAnchorF; // Fractional global mouse position (drag axis) when drag started
    private int _dragStartValue; // Value when drag started

    public override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);

        int w = RenderSize.Width;
        int h = RenderSize.Height;
        int clickPos = (Orientation == Orientation.Vertical) ? e.Y : e.X;
        int maxPos = (Orientation == Orientation.Vertical) ? h : w;

        if (clickPos == 0) // Arrow 1
        {
            Value -= SmallChange;
        }
        else if (clickPos == maxPos - 1) // Arrow 2
        {
            Value += SmallChange;
        }
        else // Track
        {
            int innerLen = maxPos - 2;
            if (innerLen <= 0) return;

            long range = (long)Maximum - Minimum;
            long contentSize = range + ViewportSize;

            int thumbSize = 1;
            if (contentSize > 0)
                thumbSize = (int)Math.Max(1, (long)innerLen * ViewportSize / contentSize);
            if (thumbSize > innerLen) thumbSize = innerLen;

            int availableSlide = innerLen - thumbSize;
            int thumbPos = 0;
            if (range > 0 && availableSlide > 0)
            {
                thumbPos = (int)((long)availableSlide * (Value - Minimum) / range);
            }

            int clickTrackPos = clickPos - 1;

            // Check if clicked ON Thumb
            if (clickTrackPos >= thumbPos && clickTrackPos < thumbPos + thumbSize)
            {
                // Start Drag. The anchor is kept in fractional global coordinates so the
                // drag keeps working wherever the pointer goes (mouse is captured) and so
                // sub-cell precision from pixel-based hosts maps to fine-grained scrolling.
                _isDragging = true;
                _dragAnchorF = (Orientation == Orientation.Vertical) ? e.GlobalYF : e.GlobalXF;
                _dragStartValue = Value;

                var root = GetRoot() as TuiWindow;
                root?.CaptureMouse(this);
            }
            else if (clickTrackPos < thumbPos)
                Value -= LargeChange;
            else if (clickTrackPos >= thumbPos + thumbSize)
                Value += LargeChange;
        }
        e.Handled = true;
    }

    public override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        if (_isDragging)
        {
            int w = RenderSize.Width;
            int h = RenderSize.Height;
            int maxPos = (Orientation == Orientation.Vertical) ? h : w;
            int innerLen = maxPos - 2;

            if (innerLen <= 0) return;

            // Calculate Thumb Size again
            long range = (long)Maximum - Minimum;
            long contentSize = range + ViewportSize;
            int thumbSize = 1;
            if (contentSize > 0)
                thumbSize = (int)Math.Max(1, (long)innerLen * ViewportSize / contentSize);
            if (thumbSize > innerLen) thumbSize = innerLen;
            int availableSlide = innerLen - thumbSize;

            if (availableSlide > 0 && range > 0)
            {
                // Delta in fractional global cells along the drag axis. Global coordinates
                // (rather than local) so the mapping is unaffected by where the captured
                // pointer currently is relative to the bar.
                double currentPosF = (Orientation == Orientation.Vertical) ? e.GlobalYF : e.GlobalXF;
                double deltaCells = currentPosF - _dragAnchorF;

                // thumbPos = availableSlide * (Value - Min) / Range
                // => DeltaValue = DeltaCells * Range / availableSlide
                // Rounded to nearest so sub-cell movement scrolls line by line once the
                // accumulated distance crosses half a line's worth of track.
                int deltaValue = (int)Math.Round(deltaCells * range / availableSlide);

                Value = _dragStartValue + deltaValue;
            }
        }
        e.Handled = true;
    }

    public override void OnMouseUp(MouseEventArgs e)
    {
        base.OnMouseUp(e);
        if (_isDragging)
        {
            _isDragging = false;
            var root = GetRoot() as TuiWindow;
            root?.ReleaseMouseCapture();
        }
        e.Handled = true;
    }

    private WheelNotchAccumulator _wheelAccumulator;

    public override void OnMouseWheel(MouseWheelEventArgs e)
    {
        base.OnMouseWheel(e);
        if (e.Handled) return;
        if (Maximum <= Minimum) return; // nothing to scroll; let an ancestor take the wheel

        int notches = _wheelAccumulator.Add(e.Delta);
        if (notches != 0)
        {
            // Wheel up (positive delta) scrolls toward Minimum, one notch = the same
            // distance as WheelScrollLines clicks on the arrow buttons.
            Value -= notches * SmallChange * ScrollViewer.WheelScrollLines;
        }
        e.Handled = true;
    }

}
