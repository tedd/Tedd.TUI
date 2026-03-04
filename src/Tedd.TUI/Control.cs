using System;

namespace Tedd.TUI;

public class Control : UIElement
{
    private UIElement _templateRoot;
    protected UIElement TemplateRoot => _templateRoot;

    public static readonly DependencyProperty TemplateProperty =
        DependencyProperty.Register("Template", typeof(ControlTemplate), typeof(Control), null);

    public ControlTemplate Template
    {
        get => (ControlTemplate)GetValue(TemplateProperty);
        set => SetValue(TemplateProperty, value);
    }

    public static readonly DependencyProperty PaddingProperty =
        DependencyProperty.Register("Padding", typeof(Thickness), typeof(Control), new Thickness(0));

    public Thickness Padding
    {
        get => (Thickness)GetValue(PaddingProperty);
        set => SetValue(PaddingProperty, value);
    }

    public static readonly DependencyProperty BorderBrushProperty =
        DependencyProperty.Register("BorderBrush", typeof(ConsoleColor), typeof(Control), ConsoleColor.Gray);

    public ConsoleColor BorderBrush
    {
        get => (ConsoleColor)GetValue(BorderBrushProperty);
        set => SetValue(BorderBrushProperty, value);
    }

    public static readonly DependencyProperty BorderThicknessProperty =
        DependencyProperty.Register("BorderThickness", typeof(Thickness), typeof(Control), new Thickness(0));

    public Thickness BorderThickness
    {
        get => (Thickness)GetValue(BorderThicknessProperty);
        set => SetValue(BorderThicknessProperty, value);
    }

    protected override void OnPropertyChanged(DependencyProperty dp)
    {
        base.OnPropertyChanged(dp);
        if (dp == TemplateProperty)
        {
            ApplyTemplate();
        }
    }

    public virtual void ApplyTemplate()
    {
        var template = Template;
        if (template != null)
        {
            // Remove old template root parent
            if (_templateRoot != null)
            {
                _templateRoot.Parent = null;
                _templateRoot.TemplatedParent = null;
            }

            _templateRoot = template.LoadContent(this);

            if (_templateRoot != null)
            {
                _templateRoot.TemplatedParent = this;
                _templateRoot.Parent = this; // Set logical/visual parent
                Invalidate();
            }
        }
        else
        {
            if (_templateRoot != null)
            {
                _templateRoot.Parent = null;
                _templateRoot.TemplatedParent = null;
            }
            _templateRoot = null;
            Invalidate();
        }
    }

    public override int VisualChildrenCount => _templateRoot != null ? 1 : 0;

    public override UIElement GetVisualChild(int index)
    {
        if (_templateRoot != null && index == 0) return _templateRoot;
        throw new ArgumentOutOfRangeException(nameof(index));
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        if (_templateRoot != null)
        {
            Thickness padding = Padding;
            int paddingWidth = padding.Left + padding.Right;
            int paddingHeight = padding.Top + padding.Bottom;

            Size innerAvailableSize = new Size(
                System.Math.Max(0, availableSize.Width - paddingWidth),
                System.Math.Max(0, availableSize.Height - paddingHeight)
            );

            _templateRoot.Measure(innerAvailableSize);

            return new Size(
                _templateRoot.DesiredSize.Width + paddingWidth,
                _templateRoot.DesiredSize.Height + paddingHeight
            );
        }
        return new Size(0, 0);
    }

    protected override void ArrangeOverride(Size finalSize)
    {
        if (_templateRoot != null)
        {
            Thickness padding = Padding;
            int paddingWidth = padding.Left + padding.Right;
            int paddingHeight = padding.Top + padding.Bottom;

            int innerWidth = System.Math.Max(0, finalSize.Width - paddingWidth);
            int innerHeight = System.Math.Max(0, finalSize.Height - paddingHeight);

            _templateRoot.Arrange(new Rect(padding.Left, padding.Top, innerWidth, innerHeight));
        }
    }

    public override void Render(VirtualBuffer buffer, int offsetX, int offsetY)
    {
        if (_templateRoot != null)
        {
            int x = RenderSize.X + offsetX;
            int y = RenderSize.Y + offsetY;
            _templateRoot.Render(buffer, x, y);
        }
    }
}
