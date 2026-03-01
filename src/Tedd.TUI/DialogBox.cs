using System;

namespace Tedd.TUI;

/// <summary>
/// A modal dialog box control with a border, title bar, and content container.
/// Can be shown/hidden using the Visibility property.
/// </summary>
public class DialogBox : UIElement
{
    private UIElement _content;

    /// <summary>
    /// Gets or sets the content element displayed inside the dialog.
    /// </summary>
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
    public override int VisualChildrenCount => _content != null ? 1 : 0;

    public override UIElement GetVisualChild(int index)
    {
        if (_content != null && index == 0) return _content;
        throw new ArgumentOutOfRangeException(nameof(index));
    }

    /// <summary>
    /// Title displayed in the dialog's title bar.
    /// </summary>
    public static readonly DependencyProperty TitleProperty =
        DependencyProperty.Register("Title", typeof(string), typeof(DialogBox), string.Empty);

    public string Title
    {
        get { return (string)GetValue(TitleProperty); }
        set { SetValue(TitleProperty, value); }
    }

    /// <summary>
    /// Border color for the dialog box frame.
    /// </summary>
    public static readonly DependencyProperty BorderColorProperty =
        DependencyProperty.Register("BorderColor", typeof(ConsoleColor), typeof(DialogBox), ConsoleColor.White);

    public ConsoleColor BorderColor
    {
        get { return (ConsoleColor)GetValue(BorderColorProperty); }
        set { SetValue(BorderColorProperty, value); }
    }

    /// <summary>
    /// Title bar foreground color.
    /// </summary>
    public static readonly DependencyProperty TitleColorProperty =
        DependencyProperty.Register("TitleColor", typeof(ConsoleColor), typeof(DialogBox), ConsoleColor.Yellow);

    public ConsoleColor TitleColor
    {
        get { return (ConsoleColor)GetValue(TitleColorProperty); }
        set { SetValue(TitleColorProperty, value); }
    }

    /// <summary>
    /// Background color of the dialog.
    /// </summary>
    public static readonly DependencyProperty BackgroundColorProperty =
        DependencyProperty.Register("BackgroundColor", typeof(ConsoleColor), typeof(DialogBox), ConsoleColor.Black);

    public ConsoleColor BackgroundColor
    {
        get { return (ConsoleColor)GetValue(BackgroundColorProperty); }
        set { SetValue(BackgroundColorProperty, value); }
    }

    /// <summary>
    /// Box drawing style (Single or Double lines).
    /// </summary>
    public static readonly DependencyProperty BoxStyleProperty =
        DependencyProperty.Register("BoxStyle", typeof(BoxStyle), typeof(DialogBox), BoxStyle.Double);

    public BoxStyle BoxStyle
    {
        get { return (BoxStyle)GetValue(BoxStyleProperty); }
        set { SetValue(BoxStyleProperty, value); }
    }

    protected override void OnDataContextChanged(object newValue)
    {
        base.OnDataContextChanged(newValue);
        if (Content != null)
        {
            Content.DataContext = newValue;
        }
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        // If Width/Height explicitly set, use those; otherwise measure content
        int desiredWidth = Width > 0 ? Width : 40;
        int desiredHeight = Height > 0 ? Height : 10;

        if (Content != null)
        {
            // Border takes 2 width (left + right), 3 height (top border + title + bottom border)
            Size contentAvailable = new Size(
                Math.Max(0, desiredWidth - 2),
                Math.Max(0, desiredHeight - 2) // 1 for top border, 1 for bottom border
            );

            Content.Measure(contentAvailable);
            Size contentSize = Content.DesiredSize;

            // If not explicit size, calculate from content
            if (Width <= 0)
            {
                // Content width + border (2) + some padding for title
                int titleWidth = (Title?.Length ?? 0) + 4; // [ Title ] padding
                desiredWidth = Math.Max(contentSize.Width + 2, titleWidth);
            }

            if (Height <= 0)
            {
                // Content height + 2 (top and bottom border)
                desiredHeight = contentSize.Height + 2;
            }
        }

        return new Size(desiredWidth, desiredHeight);
    }

    protected override void ArrangeOverride(Size finalSize)
    {
        if (Content != null)
        {
            // Content area: inside the border (1 from each edge)
            // Top: 1 row for border/title
            // Bottom: 1 row for border
            // Left/Right: 1 column each for border
            Content.Arrange(new Rect(
                1,
                1,
                Math.Max(0, finalSize.Width - 2),
                Math.Max(0, finalSize.Height - 2)
            ));
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
                buffer.SetPixel(x + col, y + row, ' ', ConsoleColor.White, bgColor);
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

        // Render content
        if (Content != null)
        {
            Content.Render(buffer, x, y);
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
