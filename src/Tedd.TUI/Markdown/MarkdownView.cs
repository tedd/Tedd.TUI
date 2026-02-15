using System;
using Tedd.TUI;

namespace Tedd.TUI.Markdown;

public class MarkdownView : UIElement
{
    private FlowDocument _document;
    private MarkdownParser _parser;

    public static readonly DependencyProperty TextProperty =
        DependencyProperty.Register("Text", typeof(string), typeof(MarkdownView), string.Empty);

    public string Text
    {
        get { return (string)GetValue(TextProperty); }
        set { SetValue(TextProperty, value); }
    }

    private MarkdownTheme _theme;
    public MarkdownTheme Theme
    {
        get => _theme ?? (_theme = new MarkdownTheme());
        set
        {
            _theme = value;
            Refresh();
        }
    }

    public MarkdownView()
    {
        _document = new FlowDocument();
        _document.Parent = this;
    }

    protected override void OnPropertyChanged(DependencyProperty dp)
    {
        base.OnPropertyChanged(dp);
        if (dp == TextProperty)
        {
            Refresh();
        }
    }

    public void Refresh()
    {
        if (string.IsNullOrEmpty(Text))
        {
            // Reset to empty document
            _document = new FlowDocument();
            _document.Parent = this;
            Invalidate();
            return;
        }

        // Create parser with current theme
        _parser = new MarkdownParser(Theme);

        // Parse and populate
        var doc = _parser.Parse(Text);

        // Replace document
        _document = doc;
        _document.Parent = this;

        Invalidate();
    }

    public override int VisualChildrenCount => _document != null ? 1 : 0;

    public override UIElement GetVisualChild(int index)
    {
        if (_document != null && index == 0) return _document;
        throw new ArgumentOutOfRangeException(nameof(index));
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        if (_document == null) return new Size(0, 0);
        _document.Measure(availableSize);
        return _document.DesiredSize;
    }

    protected override void ArrangeOverride(Size finalSize)
    {
        if (_document != null)
        {
            _document.Arrange(new Rect(0, 0, finalSize.Width, finalSize.Height));
        }
    }

    public override void Render(VirtualBuffer buffer, int offsetX, int offsetY)
    {
        if (_document != null)
        {
            int x = RenderSize.X + offsetX;
            int y = RenderSize.Y + offsetY;
            _document.Render(buffer, x, y);
        }
    }
}
