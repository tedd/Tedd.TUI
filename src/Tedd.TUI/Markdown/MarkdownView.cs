using System;
using Tedd.TUI;

namespace Tedd.TUI.Markdown;

public class MarkdownView : ScrollViewer
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
        Content = _document;
        HorizontalScrollBarVisibility = false; // Usually wrapping text doesn't need horizontal scroll
        VerticalScrollBarVisibility = true;
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
        if (_document == null) return;

        // Clear existing children
        // FlowDocument inherits StackPanel -> Children.Clear()
        _document.Children.Clear();

        if (string.IsNullOrEmpty(Text)) return;

        // Create parser with current theme
        _parser = new MarkdownParser(Theme);

        // Parse and populate
        var doc = _parser.Parse(Text);

        // Move children from parsed doc to our _document
        // or just replace Content?
        // Replacing Content is cleaner.
        _document = doc;
        Content = _document;
    }
}
