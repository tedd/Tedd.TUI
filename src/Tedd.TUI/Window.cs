using System;

namespace Tedd.TUI;

/// <summary>
/// A floating window overlay with a border, title bar and optional close button.
/// The window can be moved by dragging its title bar and resized by dragging its
/// edges or corners (bottom edge, left/right edges, and all four corners; the top
/// row is reserved for the title-bar move gesture except at the corners).
/// Shown on top of a <see cref="TuiWindow"/> via the overlay mechanism.
/// </summary>
public class Window : UIElement
{
    /// <summary>
    /// Gets or sets the content element displayed inside the window.
    /// </summary>
    public UIElement Content
    {
        get;
        set
        {
            field = value;
            if (field != null)
            {
                field.Parent = this;
            }
        }
    }

    public override int VisualChildrenCount => Content != null ? 1 : 0;

    public override UIElement GetVisualChild(int index)
    {
        if (Content != null && index == 0) return Content;
        throw new ArgumentOutOfRangeException(nameof(index));
    }

    /// <summary>
    /// Title displayed in the window's title bar.
    /// </summary>
    public static readonly DependencyProperty TitleProperty =
        DependencyProperty.Register("Title", typeof(string), typeof(Window), string.Empty);

    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    /// <summary>
    /// Border color for the window frame.
    /// </summary>
    public static readonly DependencyProperty BorderColorProperty =
        DependencyProperty.Register("BorderColor", typeof(TuiColor), typeof(Window), TuiColor.White);

    public TuiColor BorderColor
    {
        get => (TuiColor)GetValue(BorderColorProperty);
        set => SetValue(BorderColorProperty, value);
    }

    /// <summary>
    /// Title bar foreground color.
    /// </summary>
    public static readonly DependencyProperty TitleColorProperty =
        DependencyProperty.Register("TitleColor", typeof(TuiColor), typeof(Window), TuiColor.Yellow);

    public TuiColor TitleColor
    {
        get => (TuiColor)GetValue(TitleColorProperty);
        set => SetValue(TitleColorProperty, value);
    }

    /// <summary>
    /// Background color of the window.
    /// </summary>
    public static readonly DependencyProperty BackgroundColorProperty =
        DependencyProperty.Register("BackgroundColor", typeof(TuiColor), typeof(Window), TuiColor.Black);

    public TuiColor BackgroundColor
    {
        get => (TuiColor)GetValue(BackgroundColorProperty);
        set => SetValue(BackgroundColorProperty, value);
    }

    /// <summary>
    /// Box drawing style (Single or Double lines).
    /// </summary>
    public static readonly DependencyProperty BoxStyleProperty =
        DependencyProperty.Register("BoxStyle", typeof(BoxStyle), typeof(Window), BoxStyle.Double);

    public BoxStyle BoxStyle
    {
        get => (BoxStyle)GetValue(BoxStyleProperty);
        set => SetValue(BoxStyleProperty, value);
    }

    /// <summary>
    /// Space between the window frame and its Content, in addition to the border
    /// itself. Defaults to one character on every side so content never sits
    /// flush against the frame.
    /// </summary>
    public static readonly DependencyProperty PaddingProperty =
        DependencyProperty.Register("Padding", typeof(Thickness), typeof(Window), new Thickness(1));

    public Thickness Padding
    {
        get => (Thickness)GetValue(PaddingProperty);
        set => SetValue(PaddingProperty, value);
    }

    /// <summary>
    /// When true (default) the window can be moved by dragging its title bar.
    /// </summary>
    public static readonly DependencyProperty CanMoveProperty =
        DependencyProperty.Register("CanMove", typeof(bool), typeof(Window), true);

    public bool CanMove
    {
        get => (bool)GetValue(CanMoveProperty);
        set => SetValue(CanMoveProperty, value);
    }

    /// <summary>
    /// When true (default) the window can be resized by dragging its edges or corners.
    /// </summary>
    public static readonly DependencyProperty CanResizeProperty =
        DependencyProperty.Register("CanResize", typeof(bool), typeof(Window), true);

    public bool CanResize
    {
        get => (bool)GetValue(CanResizeProperty);
        set => SetValue(CanResizeProperty, value);
    }

    /// <summary>
    /// When true (default) a close button is rendered at the right end of the title bar.
    /// </summary>
    public static readonly DependencyProperty ShowCloseButtonProperty =
        DependencyProperty.Register("ShowCloseButton", typeof(bool), typeof(Window), true);

    public bool ShowCloseButton
    {
        get => (bool)GetValue(ShowCloseButtonProperty);
        set => SetValue(ShowCloseButtonProperty, value);
    }

    /// <summary>
    /// Minimum width the window can be resized to (including borders).
    /// </summary>
    public static readonly DependencyProperty MinWidthProperty =
        DependencyProperty.Register("MinWidth", typeof(int), typeof(Window), 10);

    public int MinWidth
    {
        get => (int)GetValue(MinWidthProperty);
        set => SetValue(MinWidthProperty, value);
    }

    /// <summary>
    /// Minimum height the window can be resized to (including borders).
    /// </summary>
    public static readonly DependencyProperty MinHeightProperty =
        DependencyProperty.Register("MinHeight", typeof(int), typeof(Window), 3);

    public int MinHeight
    {
        get => (int)GetValue(MinHeightProperty);
        set => SetValue(MinHeightProperty, value);
    }

    /// <summary>
    /// Left position within the host window. Negative (default) means auto-center.
    /// </summary>
    public static readonly DependencyProperty LeftProperty =
        DependencyProperty.Register("Left", typeof(int), typeof(Window), -1);

    public int Left
    {
        get => (int)GetValue(LeftProperty);
        set => SetValue(LeftProperty, value);
    }

    /// <summary>
    /// Top position within the host window. Negative (default) means auto-center.
    /// </summary>
    public static readonly DependencyProperty TopProperty =
        DependencyProperty.Register("Top", typeof(int), typeof(Window), -1);

    public int Top
    {
        get => (int)GetValue(TopProperty);
        set => SetValue(TopProperty, value);
    }

    /// <summary>Raised after the window has been closed (removed from the host overlay).</summary>
    public event EventHandler? Closed;

    /// <summary>
    /// Pushes this window as an overlay on <paramref name="host"/> and shows it.
    /// </summary>
    public void Show(TuiWindow host)
    {
        ArgumentNullException.ThrowIfNull(host);
        host.PushOverlay(this);
        Show();
    }

    /// <summary>
    /// Shows the window. The window must already be attached to a host
    /// (via <see cref="TuiWindow.PushOverlay"/> or <see cref="Show(TuiWindow)"/>).
    /// Positions at <see cref="Left"/>/<see cref="Top"/>, or centered when unset,
    /// and moves focus to the first focusable element inside.
    /// </summary>
    public virtual void Show()
    {
        Visibility = true;
        if (GetRoot() is TuiWindow root)
        {
            ArrangeInHost(new Size(root.RenderSize.Width, root.RenderSize.Height));
            root.FocusFirstIn(this);
            root.Invalidate();
        }
    }

    /// <summary>
    /// Closes the window: hides it, removes it from the host overlay stack and
    /// raises <see cref="Closed"/>.
    /// </summary>
    public virtual void Close()
    {
        Visibility = false;
        var root = GetRoot() as TuiWindow;
        root?.RemoveOverlay(this);
        Closed?.Invoke(this, EventArgs.Empty);
        root?.Invalidate();
    }

    /// <summary>
    /// Measures against the host size and arranges at the explicit position
    /// (clamped inside the host) or centered when no position is set. Called by
    /// <see cref="TuiWindow"/> on every layout pass and by drag operations.
    /// </summary>
    internal void ArrangeInHost(Size hostSize)
    {
        Measure(hostSize);
        int w = Math.Min(DesiredSize.Width, hostSize.Width);
        int h = Math.Min(DesiredSize.Height, hostSize.Height);
        int left = Left;
        int top = Top;
        int x = left < 0 ? Math.Max(0, (hostSize.Width - w) / 2) : Math.Clamp(left, 0, Math.Max(0, hostSize.Width - w));
        int y = top < 0 ? Math.Max(0, (hostSize.Height - h) / 2) : Math.Clamp(top, 0, Math.Max(0, hostSize.Height - h));
        Arrange(new Rect(x, y, w, h));
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        int desiredWidth = Width > 0 ? Width : 40;
        int desiredHeight = Height > 0 ? Height : 10;

        Thickness padding = Padding;
        int insetW = 2 + padding.Left + padding.Right;
        int insetH = 2 + padding.Top + padding.Bottom;

        if (Content != null)
        {
            Size contentAvailable = new Size(
                Math.Max(0, desiredWidth - insetW),
                Math.Max(0, desiredHeight - insetH)
            );

            Content.Measure(contentAvailable);
            Size contentSize = Content.DesiredSize;

            if (Width <= 0)
            {
                int titleWidth = (Title?.Length ?? 0) + 4;
                desiredWidth = Math.Max(contentSize.Width + insetW, titleWidth);
            }

            if (Height <= 0)
            {
                desiredHeight = contentSize.Height + insetH;
            }
        }

        desiredWidth = Math.Max(desiredWidth, MinWidth);
        desiredHeight = Math.Max(desiredHeight, MinHeight);
        return new Size(desiredWidth, desiredHeight);
    }

    protected override void ArrangeOverride(Size finalSize)
    {
        Thickness padding = Padding;
        Content?.Arrange(new Rect(
            1 + padding.Left,
            1 + padding.Top,
            Math.Max(0, finalSize.Width - 2 - padding.Left - padding.Right),
            Math.Max(0, finalSize.Height - 2 - padding.Top - padding.Bottom)
        ));
    }

    public override void Render(VirtualBuffer buffer, int offsetX, int offsetY)
    {
        if (!Visibility) return;

        int w = RenderSize.Width;
        int h = RenderSize.Height;
        int x = RenderSize.X + offsetX;
        int y = RenderSize.Y + offsetY;

        if (w < 2 || h < 2) return;

        var borderColor = BorderColor;
        var titleColor = TitleColor;
        var bgColor = BackgroundColor;
        var chars = BoxDrawingChars.Get(BoxStyle);
        string title = Title ?? string.Empty;
        bool closeButton = ShowCloseButton && w >= 8;

        // Fill background
        for (int row = 1; row < h - 1; row++)
        {
            for (int col = 1; col < w - 1; col++)
            {
                buffer.SetPixel(x + col, y + row, ' ', TuiColor.White, bgColor);
            }
        }

        // Corners
        buffer.SetPixel(x, y, chars.TopLeft, borderColor, bgColor);
        buffer.SetPixel(x + w - 1, y, chars.TopRight, borderColor, bgColor);
        buffer.SetPixel(x, y + h - 1, chars.BottomLeft, borderColor, bgColor);
        buffer.SetPixel(x + w - 1, y + h - 1, chars.BottomRight, borderColor, bgColor);

        // Top border with centered title; the close button occupies the last three
        // cells before the top-right corner ("[x]").
        int reservedRight = closeButton ? 3 : 0;
        int titleMaxLen = w - 4 - reservedRight;
        if (title.Length > titleMaxLen && titleMaxLen > 0)
        {
            title = title.Substring(0, titleMaxLen);
        }

        int titleStart = (w - title.Length - 2) / 2;

        for (int i = 1; i < w - 1; i++)
        {
            if (closeButton && i >= w - 4 && i <= w - 2)
            {
                char c = i == w - 4 ? '[' : i == w - 2 ? ']' : 'x';
                buffer.SetPixel(x + i, y, c, titleColor, bgColor);
            }
            else if (!string.IsNullOrEmpty(title) && i == titleStart && titleMaxLen > 0)
            {
                buffer.SetPixel(x + i, y, ' ', titleColor, bgColor);
                i++;
                int titleEnd = closeButton ? w - 5 : w - 1;
                for (int ti = 0; ti < title.Length && i < titleEnd; ti++, i++)
                {
                    buffer.SetPixel(x + i, y, title[ti], titleColor, bgColor);
                }
                if (i < titleEnd)
                {
                    buffer.SetPixel(x + i, y, ' ', titleColor, bgColor);
                }
                else
                {
                    i--; // The for-loop increment steps past the last written cell.
                }
            }
            else
            {
                buffer.SetPixel(x + i, y, chars.Horizontal, borderColor, bgColor);
            }
        }

        // Bottom border
        for (int i = 1; i < w - 1; i++)
        {
            buffer.SetPixel(x + i, y + h - 1, chars.Horizontal, borderColor, bgColor);
        }

        // Left/Right borders
        for (int i = 1; i < h - 1; i++)
        {
            buffer.SetPixel(x, y + i, chars.Vertical, borderColor, bgColor);
            buffer.SetPixel(x + w - 1, y + i, chars.Vertical, borderColor, bgColor);
        }

        Content?.Render(buffer, x, y);
    }

    private enum DragZone
    {
        None,
        Move,
        East,
        West,
        South,
        SouthEast,
        SouthWest,
        NorthEast,
        NorthWest
    }

    private DragZone _dragZone = DragZone.None;
    private int _dragStartGlobalX, _dragStartGlobalY;
    private int _startLeft, _startTop, _startWidth, _startHeight;

    private DragZone GetZone(int localX, int localY, int width, int height)
    {
        bool canResize = CanResize;
        if (localY == 0)
        {
            if (canResize && localX == 0) return DragZone.NorthWest;
            if (canResize && localX == width - 1) return DragZone.NorthEast;
            return CanMove ? DragZone.Move : DragZone.None;
        }
        if (localY == height - 1)
        {
            if (!canResize) return DragZone.None;
            if (localX == 0) return DragZone.SouthWest;
            if (localX == width - 1) return DragZone.SouthEast;
            return DragZone.South;
        }
        if (canResize && localX == 0) return DragZone.West;
        if (canResize && localX == width - 1) return DragZone.East;
        return DragZone.None;
    }

    public override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        if (e.Handled) return;

        var root = GetRoot() as TuiWindow;
        if (root == null) return;

        var origin = PointToScreen(new Point(0, 0));
        int localX = e.GlobalX - origin.X;
        int localY = e.GlobalY - origin.Y;
        int w = RenderSize.Width;
        int h = RenderSize.Height;

        if (ShowCloseButton && w >= 8 && localY == 0 && localX >= w - 4 && localX <= w - 2)
        {
            e.Handled = true;
            Close();
            return;
        }

        var zone = GetZone(localX, localY, w, h);
        if (zone == DragZone.None) return;

        _dragZone = zone;
        _dragStartGlobalX = e.GlobalX;
        _dragStartGlobalY = e.GlobalY;
        _startLeft = RenderSize.X;
        _startTop = RenderSize.Y;
        _startWidth = w;
        _startHeight = h;
        root.CaptureMouse(this);
        e.Handled = true;
    }

    public override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        if (_dragZone == DragZone.None) return;

        var root = GetRoot() as TuiWindow;
        if (root?.CapturedElement != this) return;

        int dx = e.GlobalX - _dragStartGlobalX;
        int dy = e.GlobalY - _dragStartGlobalY;

        int left = _startLeft;
        int top = _startTop;
        int width = _startWidth;
        int height = _startHeight;
        int minW = MinWidth;
        int minH = MinHeight;

        switch (_dragZone)
        {
            case DragZone.Move:
                left += dx;
                top += dy;
                break;
            case DragZone.East:
                width += dx;
                break;
            case DragZone.South:
                height += dy;
                break;
            case DragZone.SouthEast:
                width += dx;
                height += dy;
                break;
            case DragZone.West:
                left += dx;
                width -= dx;
                break;
            case DragZone.SouthWest:
                left += dx;
                width -= dx;
                height += dy;
                break;
            case DragZone.NorthEast:
                top += dy;
                height -= dy;
                width += dx;
                break;
            case DragZone.NorthWest:
                left += dx;
                width -= dx;
                top += dy;
                height -= dy;
                break;
        }

        // Enforce minimum size; when a left/top edge is being dragged the position
        // must stop moving once the opposite edge would be pushed.
        if (width < minW)
        {
            if (_dragZone is DragZone.West or DragZone.SouthWest or DragZone.NorthWest)
                left = _startLeft + _startWidth - minW;
            width = minW;
        }
        if (height < minH)
        {
            if (_dragZone is DragZone.NorthEast or DragZone.NorthWest)
                top = _startTop + _startHeight - minH;
            height = minH;
        }

        Left = Math.Max(0, left);
        Top = Math.Max(0, top);
        if (_dragZone != DragZone.Move)
        {
            Width = width;
            Height = height;
        }

        ArrangeInHost(new Size(root.RenderSize.Width, root.RenderSize.Height));
        root.Invalidate();
        e.Handled = true;
    }

    public override void OnMouseUp(MouseEventArgs e)
    {
        base.OnMouseUp(e);
        if (_dragZone == DragZone.None) return;

        _dragZone = DragZone.None;
        var root = GetRoot() as TuiWindow;
        if (root?.CapturedElement == this)
        {
            root.ReleaseMouseCapture();
        }
        e.Handled = true;
    }
}
