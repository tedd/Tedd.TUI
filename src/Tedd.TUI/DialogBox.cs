using System;

namespace Tedd.TUI;

/// <summary>
/// A modal dialog box control with a border, title bar, and content container.
/// Can be shown/hidden using the Visibility property.
///
/// Extends <see cref="ScrollViewer"/> so that content taller (or wider) than the
/// dialog's frame gets a scrollbar instead of silently overflowing past the border.
/// By default the vertical scrollbar is <see cref="ScrollBarVisibility.Auto"/> (shown
/// only when content overflows); the horizontal one is <see cref="ScrollBarVisibility.Disabled"/>,
/// matching the common case of a dialog that is too short but sized to the right width.
/// </summary>
public class DialogBox : ScrollViewer, IModalOverlay
{
    /// <summary>
    /// Title displayed in the dialog's title bar.
    /// </summary>
    public static readonly DependencyProperty TitleProperty =
        DependencyProperty.Register("Title", typeof(string), typeof(DialogBox), string.Empty);

    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    /// <summary>
    /// Border color for the dialog box frame.
    /// </summary>
    public static readonly DependencyProperty BorderColorProperty =
        DependencyProperty.Register("BorderColor", typeof(TuiColor), typeof(DialogBox), TuiColor.White);

    public TuiColor BorderColor
    {
        get => (TuiColor)GetValue(BorderColorProperty);
        set => SetValue(BorderColorProperty, value);
    }

    /// <summary>
    /// Title bar foreground color.
    /// </summary>
    public static readonly DependencyProperty TitleColorProperty =
        DependencyProperty.Register("TitleColor", typeof(TuiColor), typeof(DialogBox), TuiColor.Yellow);

    public TuiColor TitleColor
    {
        get => (TuiColor)GetValue(TitleColorProperty);
        set => SetValue(TitleColorProperty, value);
    }

    /// <summary>
    /// Background color of the dialog.
    /// </summary>
    public static readonly DependencyProperty BackgroundColorProperty =
        DependencyProperty.Register("BackgroundColor", typeof(TuiColor), typeof(DialogBox), TuiColor.Black);

    public TuiColor BackgroundColor
    {
        get => (TuiColor)GetValue(BackgroundColorProperty);
        set => SetValue(BackgroundColorProperty, value);
    }

    /// <summary>
    /// Box drawing style (Single or Double lines).
    /// </summary>
    public static readonly DependencyProperty BoxStyleProperty =
        DependencyProperty.Register("BoxStyle", typeof(BoxStyle), typeof(DialogBox), BoxStyle.Double);

    public BoxStyle BoxStyle
    {
        get => (BoxStyle)GetValue(BoxStyleProperty);
        set => SetValue(BoxStyleProperty, value);
    }

    /// <summary>
    /// Space between the dialog frame and its Content, in addition to the border
    /// itself. Defaults to one character on every side so content never sits
    /// flush against the frame.
    /// </summary>
    public static readonly DependencyProperty PaddingProperty =
        DependencyProperty.Register("Padding", typeof(Thickness), typeof(DialogBox), new Thickness(1));

    public Thickness Padding
    {
        get => (Thickness)GetValue(PaddingProperty);
        set => SetValue(PaddingProperty, value);
    }

    public DialogBox()
    {
        VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
        HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled;
        UpdateScrollBarStyle();
    }

    private void UpdateScrollBarStyle()
    {
        var chars = BoxDrawingChars.Get(BoxStyle);
        var borderColor = BorderColor;

        // Scrollbars overlay the border line itself (like Border), so they don't
        // steal a row/column of content space and read as part of the frame.
        _verticalScrollBar.TrackChar = chars.Vertical;
        _verticalScrollBar.Foreground = borderColor;
        _verticalScrollBar.Background = null;

        _horizontalScrollBar.TrackChar = chars.Horizontal;
        _horizontalScrollBar.Foreground = borderColor;
        _horizontalScrollBar.Background = null;
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        UpdateScrollBarStyle();

        Thickness padding = Padding;
        int insetW = 2 + padding.Left + padding.Right;
        int insetH = 2 + padding.Top + padding.Bottom;

        // The frame this dialog is allowed to occupy in each axis: an explicit
        // Width/Height when set, otherwise bounded by the available space so an
        // auto-sized dialog grows with its content but never exceeds the screen.
        int frameW = Width > 0 ? Width : availableSize.Width;
        int frameH = Height > 0 ? Height : availableSize.Height;

        int viewportContentW = Math.Max(0, frameW - insetW);
        int viewportContentH = Math.Max(0, frameH - insetH);

        Size contentAvailable = new Size(viewportContentW, viewportContentH);
        if (VerticalScrollBarVisibility != ScrollBarVisibility.Disabled) contentAvailable.Height = int.MaxValue;
        if (HorizontalScrollBarVisibility != ScrollBarVisibility.Disabled) contentAvailable.Width = int.MaxValue;

        Size contentSize = new Size(0, 0);
        if (Content != null)
        {
            Content.Measure(contentAvailable);
            contentSize = Content.DesiredSize;
        }

        bool showVertical = VerticalScrollBarVisibility switch
        {
            ScrollBarVisibility.Visible => true,
            ScrollBarVisibility.Auto => contentSize.Height > viewportContentH,
            _ => false
        };
        bool showHorizontal = HorizontalScrollBarVisibility switch
        {
            ScrollBarVisibility.Visible => true,
            ScrollBarVisibility.Auto => contentSize.Width > viewportContentW,
            _ => false
        };

        SetResolvedScrollBarVisibility(showVertical, showHorizontal);

        if (showVertical)
        {
            int viewport = Math.Max(1, viewportContentH);
            _verticalScrollBar.ViewportSize = viewport;
            _verticalScrollBar.Maximum = Math.Max(0, contentSize.Height - viewport);
            _verticalScrollBar.Minimum = 0;
            _verticalScrollBar.Measure(new Size(1, Math.Max(0, frameH - 2)));
        }

        if (showHorizontal)
        {
            int viewport = Math.Max(1, viewportContentW);
            _horizontalScrollBar.ViewportSize = viewport;
            _horizontalScrollBar.Maximum = Math.Max(0, contentSize.Width - viewport);
            _horizontalScrollBar.Minimum = 0;
            _horizontalScrollBar.Measure(new Size(Math.Max(0, frameW - 2), 1));
        }

        int desiredWidth;
        int desiredHeight;

        if (Width > 0)
        {
            desiredWidth = Width;
        }
        else if (Content != null)
        {
            int titleWidth = (Title?.Length ?? 0) + 4; // [ Title ] padding
            desiredWidth = Math.Max(Math.Min(contentSize.Width + insetW, availableSize.Width), titleWidth);
        }
        else
        {
            desiredWidth = 40;
        }

        if (Height > 0)
        {
            desiredHeight = Height;
        }
        else if (Content != null)
        {
            desiredHeight = Math.Min(contentSize.Height + insetH, availableSize.Height);
        }
        else
        {
            desiredHeight = 10;
        }

        return new Size(desiredWidth, desiredHeight);
    }

    protected override void ArrangeOverride(Size finalSize)
    {
        int w = finalSize.Width;
        int h = finalSize.Height;

        Thickness padding = Padding;

        if (Content != null)
        {
            int viewportW = Math.Max(0, w - 2 - padding.Left - padding.Right);
            int viewportH = Math.Max(0, h - 2 - padding.Top - padding.Bottom);

            // When an axis is Disabled we clamp the content's arrange rect to the
            // viewport; when scrollable, arrange it at its full natural size so it
            // can be scrolled, and Render clips + offsets it into the viewport.
            int arrangeW = (HorizontalScrollBarVisibility == ScrollBarVisibility.Disabled)
                ? viewportW
                : Math.Max(viewportW, Content.DesiredSize.Width);
            int arrangeH = (VerticalScrollBarVisibility == ScrollBarVisibility.Disabled)
                ? viewportH
                : Math.Max(viewportH, Content.DesiredSize.Height);

            Content.Arrange(new Rect(1 + padding.Left, 1 + padding.Top, arrangeW, arrangeH));
        }

        if (IsVerticalScrollBarShown)
        {
            int vHeight = Math.Max(0, h - 2);
            _verticalScrollBar.Arrange(new Rect(w - 1, 1, 1, vHeight));
        }

        if (IsHorizontalScrollBarShown)
        {
            int hWidth = Math.Max(0, w - 2);
            _horizontalScrollBar.Arrange(new Rect(1, h - 1, hWidth, 1));
        }
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

        // Fill background
        for (int row = 1; row < h - 1; row++)
        {
            for (int col = 1; col < w - 1; col++)
            {
                buffer.SetPixel(x + col, y + row, ' ', TuiColor.White, bgColor);
            }
        }

        // Draw border - corners
        buffer.SetPixel(x, y, chars.TopLeft, borderColor, bgColor);
        buffer.SetPixel(x + w - 1, y, chars.TopRight, borderColor, bgColor);
        buffer.SetPixel(x, y + h - 1, chars.BottomLeft, borderColor, bgColor);
        buffer.SetPixel(x + w - 1, y + h - 1, chars.BottomRight, borderColor, bgColor);

        // Top border with title
        int titleMaxLen = w - 4; // Leave space for [ ] and borders
        if (title.Length > titleMaxLen && titleMaxLen > 0)
        {
            title = title.Substring(0, titleMaxLen);
        }

        int titleStart = (w - title.Length - 2) / 2; // Center the title with brackets

        for (int i = 1; i < w - 1; i++)
        {
            if (!string.IsNullOrEmpty(title) && i == titleStart)
            {
                // Draw " Title " in the border
                buffer.SetPixel(x + i, y, ' ', titleColor, bgColor);
                i++;
                for (int ti = 0; ti < title.Length && i < w - 1; ti++, i++)
                {
                    buffer.SetPixel(x + i, y, title[ti], titleColor, bgColor);
                }
                if (i < w - 1)
                {
                    buffer.SetPixel(x + i, y, ' ', titleColor, bgColor);
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

        // Scrollbars overlay the border lines -- only when resolved as shown in MeasureOverride.
        if (IsVerticalScrollBarShown)
            _verticalScrollBar.Render(buffer, x, y);

        if (IsHorizontalScrollBarShown)
            _horizontalScrollBar.Render(buffer, x, y);

        // Render content, clipped to the padded content area and scrolled by the
        // scrollbar offsets so overflowing content never bleeds past the frame.
        if (Content != null)
        {
            Thickness padding = Padding;
            buffer.PushClip(new Rect(
                x + 1 + padding.Left,
                y + 1 + padding.Top,
                Math.Max(0, w - 2 - padding.Left - padding.Right),
                Math.Max(0, h - 2 - padding.Top - padding.Bottom)));

            Content.Render(buffer, x - HorizontalOffset, y - VerticalOffset);

            buffer.PopClip();
        }
    }

    /// <summary>
    /// Shows the dialog box.
    /// </summary>
    public void Show()
    {
        Visibility = true;
        var root = GetRoot() as TuiWindow;
        if (root != null)
        {
            // Measure the dialog against the window size
            Measure(new Size(root.RenderSize.Width, root.RenderSize.Height));

            // Center the dialog
            int x = (root.RenderSize.Width - DesiredSize.Width) / 2;
            int y = (root.RenderSize.Height - DesiredSize.Height) / 2;
            if (x < 0) x = 0;
            if (y < 0) y = 0;

            Arrange(new Rect(x, y, DesiredSize.Width, DesiredSize.Height));

            // Set focus to the first element in the dialog
            root.FocusFirstIn(this);
        }
    }

    /// <summary>
    /// Gets or sets whether the dialog is modal.
    /// If true, input events outside the dialog are blocked.
    /// Default is true.
    /// </summary>
    public bool IsModal { get; set; } = true;

    /// <summary>
    /// Hides the dialog box and clears it from the window overlay.
    /// </summary>
    public void Hide()
    {
        Visibility = false;
        var root = GetRoot() as TuiWindow;
        if (root != null)
        {
            root.RemoveOverlay(this);
        }
    }
}
