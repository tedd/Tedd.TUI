using System;

namespace Tedd.TUI;

public class Button : ButtonBase
{
    public Button()
    {
        Focusable = true;

        // Define default template
        Template = new ControlTemplate(parent =>
        {
            var btn = (Button)parent;
            var border = new Border();
            border.TemplatedParent = btn;

            // Bind visual properties to Button's "Effective" properties
            // Note: Border.Foreground/Background usually come from UIElement
            // But Border might shadow them or need explicit binding.
            // Border uses BorderColor and BoxStyle.

            // Bind BorderColor
            var borderColorBinding = new Binding("EffectiveBorderColor");
            borderColorBinding.RelativeSource = RelativeSource.TemplatedParent;
            border.SetBinding(Border.BorderColorProperty, borderColorBinding);

            // Bind BoxStyle
            var boxStyleBinding = new Binding("BoxStyle");
            boxStyleBinding.RelativeSource = RelativeSource.TemplatedParent;
            border.SetBinding(Border.BoxStyleProperty, boxStyleBinding);

            // Bind Background (inherited but explicit binding ensures it's passed if border doesn't inherit)
            var bgBinding = new Binding("Background");
            bgBinding.RelativeSource = RelativeSource.TemplatedParent;
            border.SetBinding(UIElement.BackgroundProperty, bgBinding);

            // Bind Foreground
            // TextBlock inside ContentPresenter needs foreground.
            // ContentPresenter inherits from Border. Border inherits from Button.
            // If ForegroundProperty is inherited (which we made it), then we don't need to bind it explicitly
            // UNLESS Border sets it to something else.
            // But binding explicit ensures template works as expected.
            var fgBinding = new Binding("EffectiveForeground");
            fgBinding.RelativeSource = RelativeSource.TemplatedParent;
            border.SetBinding(UIElement.ForegroundProperty, fgBinding);

            var cp = new ContentPresenter();
            cp.TemplatedParent = btn;

            // Center content by default
            cp.HorizontalAlignment = HorizontalAlignment.Center;
            cp.VerticalAlignment = VerticalAlignment.Center;

            // ContentPresenter properties
            var contentBinding = new Binding("Content");
            contentBinding.RelativeSource = RelativeSource.TemplatedParent;
            cp.SetBinding(ContentPresenter.ContentProperty, contentBinding);

            var contentTemplateBinding = new Binding("ContentTemplate");
            contentTemplateBinding.RelativeSource = RelativeSource.TemplatedParent;
            cp.SetBinding(ContentPresenter.ContentTemplateProperty, contentTemplateBinding);

            border.Content = cp;
            return border;
        });

        UpdateEffectiveColors();
    }

    // Public Properties

    public static readonly DependencyProperty BoxStyleProperty =
        DependencyProperty.Register(nameof(BoxStyle), typeof(BoxStyle), typeof(Button), BoxStyle.Single);

    public BoxStyle BoxStyle
    {
        get => (BoxStyle)GetValue(BoxStyleProperty);
        set => SetValue(BoxStyleProperty, value);
    }

    public static readonly DependencyProperty BorderColorProperty =
        DependencyProperty.Register(nameof(BorderColor), typeof(ConsoleColor), typeof(Button), ConsoleColor.Gray);

    public ConsoleColor BorderColor
    {
        get => (ConsoleColor)GetValue(BorderColorProperty);
        set => SetValue(BorderColorProperty, value);
    }

    public static readonly DependencyProperty FocusedForegroundProperty =
        DependencyProperty.Register(nameof(FocusedForeground), typeof(ConsoleColor), typeof(Button), ConsoleColor.Yellow);

    public ConsoleColor FocusedForeground
    {
        get => (ConsoleColor)GetValue(FocusedForegroundProperty);
        set => SetValue(FocusedForegroundProperty, value);
    }

    public static readonly DependencyProperty FocusedBorderColorProperty =
        DependencyProperty.Register(nameof(FocusedBorderColor), typeof(ConsoleColor), typeof(Button), ConsoleColor.Yellow);

    public ConsoleColor FocusedBorderColor
    {
        get => (ConsoleColor)GetValue(FocusedBorderColorProperty);
        set => SetValue(FocusedBorderColorProperty, value);
    }

    // Internal "Effective" properties for Template Binding

    public static readonly DependencyProperty EffectiveBorderColorProperty =
        DependencyProperty.Register(nameof(EffectiveBorderColor), typeof(ConsoleColor), typeof(Button), ConsoleColor.Gray);

    public ConsoleColor EffectiveBorderColor
    {
        get => (ConsoleColor)GetValue(EffectiveBorderColorProperty);
        private set => SetValue(EffectiveBorderColorProperty, value);
    }

    public static readonly DependencyProperty EffectiveForegroundProperty =
        DependencyProperty.Register(nameof(EffectiveForeground), typeof(ConsoleColor), typeof(Button), ConsoleColor.White);

    public ConsoleColor EffectiveForeground
    {
        get => (ConsoleColor)GetValue(EffectiveForegroundProperty);
        private set => SetValue(EffectiveForegroundProperty, value);
    }

    // Logic

    protected override void OnPropertyChanged(DependencyProperty dp)
    {
        base.OnPropertyChanged(dp);
        if (dp == BorderColorProperty || dp == FocusedBorderColorProperty ||
            dp == UIElement.ForegroundProperty || dp == FocusedForegroundProperty ||
            dp == IsFocusedProperty)
        {
            UpdateEffectiveColors();
        }
    }

    private void UpdateEffectiveColors()
    {
        if (IsFocused)
        {
            EffectiveBorderColor = FocusedBorderColor;
            EffectiveForeground = FocusedForeground;
        }
        else
        {
            EffectiveBorderColor = BorderColor;
            EffectiveForeground = Foreground;
        }
    }
}
