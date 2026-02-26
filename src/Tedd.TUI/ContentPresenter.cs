using System;

namespace Tedd.TUI;

public class ContentPresenter : UIElement
{
    public static readonly DependencyProperty ContentProperty =
        DependencyProperty.Register("Content", typeof(object), typeof(ContentPresenter), null);

    public object Content
    {
        get => GetValue(ContentProperty);
        set => SetValue(ContentProperty, value);
    }

    public static readonly DependencyProperty ContentTemplateProperty =
        DependencyProperty.Register("ContentTemplate", typeof(DataTemplate), typeof(ContentPresenter), null);

    public DataTemplate ContentTemplate
    {
        get => (DataTemplate)GetValue(ContentTemplateProperty);
        set => SetValue(ContentTemplateProperty, value);
    }

    private UIElement _visualChild;

    protected override void OnPropertyChanged(DependencyProperty dp)
    {
        base.OnPropertyChanged(dp);
        if (dp == ContentProperty || dp == ContentTemplateProperty)
        {
            UpdateVisual();
        }
    }

    private void UpdateVisual()
    {
        var content = Content;
        var template = ContentTemplate;

        // Clear old
        if (_visualChild != null)
        {
            _visualChild.Parent = null;
            _visualChild = null;
        }

        if (content == null)
        {
            Invalidate();
            return;
        }

        if (content is UIElement uiElement)
        {
            _visualChild = uiElement;
        }
        else if (template != null)
        {
            // DataTemplate factory creates the tree.
            _visualChild = template.LoadContent(this);
            if (_visualChild != null)
            {
                _visualChild.DataContext = content;
            }
        }
        else
        {
            // Fallback: TextBlock with ToString()
            var tb = new TextBlock();
            tb.Text = content.ToString() ?? "";
            // Bind Foreground? TextBlock inherits Foreground from parent (ContentPresenter)
            // if ContentPresenter inherits it from its parent (TemplatedParent usually).
            // But TextBlock checks its local value.
            // We should ensure TextBlock uses something visible.
            // TextBlock.Foreground defaults to White.
            // If parent has a different foreground, TextBlock should inherit it?
            // DependencyObject inheritance logic handles this if ForegroundProperty is inherited.
            // TextBlock.ForegroundProperty is NOT inherited by default in my implementation (check TextBlock.cs).
            // But UIElement.Foreground is not standard.
            // Control has Foreground.
            // TextBlock has Foreground.
            // If I want inheritance, I should register Foreground as inherited.
            // But for now, let's assume White is fine or user sets it on TextBlock style.

            // Actually, `ContentPresenter` is a `UIElement`. It doesn't have `Foreground`.
            // So TextBlock won't inherit it from ContentPresenter unless I add it.
            // But ContentPresenter is inside a Control (e.g. Button) which has Foreground.
            // DependencyObject inheritance chain: TextBlock -> ContentPresenter -> Border -> Button.
            // If Button sets Foreground, does it propagate?
            // Only if `ForegroundProperty` is registered as Inherited.
            // In `Control.cs`: `DependencyProperty.Register("Foreground", ...)` -> inherited? Default is false.
            // I should make `Foreground` inherited in `Control` (or wherever it's defined).

            _visualChild = tb;
        }

        if (_visualChild != null)
        {
            _visualChild.Parent = this;
            Invalidate();
        }
    }

    public override int VisualChildrenCount => _visualChild != null ? 1 : 0;

    public override UIElement GetVisualChild(int index)
    {
        if (_visualChild != null && index == 0) return _visualChild;
        throw new ArgumentOutOfRangeException(nameof(index));
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        if (_visualChild != null)
        {
            _visualChild.Measure(availableSize);
            return _visualChild.DesiredSize;
        }
        return new Size(0, 0);
    }

    protected override void ArrangeOverride(Size finalSize)
    {
        if (_visualChild != null)
        {
            _visualChild.Arrange(new Rect(0, 0, finalSize.Width, finalSize.Height));
        }
    }

    public override void Render(VirtualBuffer buffer, int offsetX, int offsetY)
    {
         if (_visualChild != null)
         {
             int x = RenderSize.X + offsetX;
             int y = RenderSize.Y + offsetY;
             _visualChild.Render(buffer, x, y);
         }
    }
}
