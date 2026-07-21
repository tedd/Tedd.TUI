using System;

namespace Tedd.TUI.Controls;

public class Button : ButtonBase
{
    public Button()
    {
        Focusable = true;

        // Buttons size to their content by default; without this a Button dropped
        // into a Stretch-oriented container (e.g. a vertical StackPanel) would
        // inherit UIElement's Stretch default and fill the container's width.
        HorizontalAlignment = HorizontalAlignment.Left;

        // Define default template
        Template = new ControlTemplate(parent =>
        {
            var btn = (Button)parent;
            // Buttons are tight chrome: label sits directly inside the border line.
            var border = new Border { Padding = new Thickness(0) };
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

            // Bind Background to the state-aware EffectiveBackground so focus/hover
            // fills (and theme-provided fills) reach the Border.
            var bgBinding = new Binding("EffectiveBackground");
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

            // Bind HorizontalAlignment to parent.HorizontalContentAlignment
            var hAlignBinding = new Binding("HorizontalContentAlignment");
            hAlignBinding.RelativeSource = RelativeSource.TemplatedParent;
            cp.SetBinding(UIElement.HorizontalAlignmentProperty, hAlignBinding);

            // Bind VerticalAlignment to parent.VerticalContentAlignment
            var vAlignBinding = new Binding("VerticalContentAlignment");
            vAlignBinding.RelativeSource = RelativeSource.TemplatedParent;
            cp.SetBinding(UIElement.VerticalAlignmentProperty, vAlignBinding);

            border.Content = cp;
            return border;
        });

        UpdateEffectiveColors();
    }

    // Public Properties

    public static readonly DependencyProperty BoxStyleProperty =
        DependencyProperty.Register("BoxStyle", typeof(BoxStyle), typeof(Button), BoxStyle.Single);

    public BoxStyle BoxStyle
    {
        get => (BoxStyle)GetValue(BoxStyleProperty);
        set => SetValue(BoxStyleProperty, value);
    }

    public static readonly DependencyProperty BorderColorProperty =
        DependencyProperty.Register("BorderColor", typeof(TuiColor), typeof(Button), TuiColor.Gray);

    public TuiColor BorderColor
    {
        get => (TuiColor)GetValue(BorderColorProperty);
        set => SetValue(BorderColorProperty, value);
    }

    public static readonly DependencyProperty FocusedForegroundProperty =
        DependencyProperty.Register("FocusedForeground", typeof(TuiColor), typeof(Button), TuiColor.Yellow);

    public TuiColor FocusedForeground
    {
        get => (TuiColor)GetValue(FocusedForegroundProperty);
        set => SetValue(FocusedForegroundProperty, value);
    }

    public static readonly DependencyProperty FocusedBorderColorProperty =
        DependencyProperty.Register("FocusedBorderColor", typeof(TuiColor), typeof(Button), TuiColor.Yellow);

    public TuiColor FocusedBorderColor
    {
        get => (TuiColor)GetValue(FocusedBorderColorProperty);
        set => SetValue(FocusedBorderColorProperty, value);
    }

    public static readonly DependencyProperty HoverForegroundProperty =
        DependencyProperty.Register("HoverForeground", typeof(TuiColor), typeof(Button), TuiColor.Cyan);

    /// <summary>Foreground used while the mouse hovers the button and it is not focused.</summary>
    public TuiColor HoverForeground
    {
        get => (TuiColor)GetValue(HoverForegroundProperty);
        set => SetValue(HoverForegroundProperty, value);
    }

    public static readonly DependencyProperty HoverBorderColorProperty =
        DependencyProperty.Register("HoverBorderColor", typeof(TuiColor), typeof(Button), TuiColor.Cyan);

    /// <summary>Border color used while the mouse hovers the button and it is not focused.</summary>
    public TuiColor HoverBorderColor
    {
        get => (TuiColor)GetValue(HoverBorderColorProperty);
        set => SetValue(HoverBorderColorProperty, value);
    }

    public static readonly DependencyProperty FocusedBackgroundProperty =
        DependencyProperty.Register("FocusedBackground", typeof(TuiColor?), typeof(Button), null);

    /// <summary>Background used while focused; null falls back to <see cref="UIElement.Background"/>.</summary>
    public TuiColor? FocusedBackground
    {
        get => (TuiColor?)GetValue(FocusedBackgroundProperty);
        set => SetValue(FocusedBackgroundProperty, value);
    }

    public static readonly DependencyProperty HoverBackgroundProperty =
        DependencyProperty.Register("HoverBackground", typeof(TuiColor?), typeof(Button), null);

    /// <summary>Background used while hovered and not focused; null falls back to <see cref="UIElement.Background"/>.</summary>
    public TuiColor? HoverBackground
    {
        get => (TuiColor?)GetValue(HoverBackgroundProperty);
        set => SetValue(HoverBackgroundProperty, value);
    }

    // Internal "Effective" properties for Template Binding

    public static readonly DependencyProperty EffectiveBackgroundProperty =
        DependencyProperty.Register("EffectiveBackground", typeof(TuiColor?), typeof(Button), null);

    public TuiColor? EffectiveBackground
    {
        get => (TuiColor?)GetValue(EffectiveBackgroundProperty);
        private set => SetValue(EffectiveBackgroundProperty, value);
    }

    public static readonly DependencyProperty EffectiveBorderColorProperty =
        DependencyProperty.Register("EffectiveBorderColor", typeof(TuiColor), typeof(Button), TuiColor.Gray);

    public TuiColor EffectiveBorderColor
    {
        get => (TuiColor)GetValue(EffectiveBorderColorProperty);
        private set => SetValue(EffectiveBorderColorProperty, value);
    }

    public static readonly DependencyProperty EffectiveForegroundProperty =
        DependencyProperty.Register("EffectiveForeground", typeof(TuiColor), typeof(Button), TuiColor.White);

    public TuiColor EffectiveForeground
    {
        get => (TuiColor)GetValue(EffectiveForegroundProperty);
        private set => SetValue(EffectiveForegroundProperty, value);
    }

    // Shadow Properties (DOS Turbo Pascal / Quick Basic style drop shadow)

    public static readonly DependencyProperty ShadowStyleProperty =
        DependencyProperty.Register("ShadowStyle", typeof(ButtonShadowStyle), typeof(Button), ButtonShadowStyle.None);

    /// <summary>
    /// The visual style for the drop shadow. Defaults to <see cref="ButtonShadowStyle.None"/>
    /// for backward compatibility. Set to <see cref="ButtonShadowStyle.Solid"/> for an
    /// authentic DOS dialog look.
    /// </summary>
    public ButtonShadowStyle ShadowStyle
    {
        get => (ButtonShadowStyle)GetValue(ShadowStyleProperty);
        set => SetValue(ShadowStyleProperty, value);
    }

    public static readonly DependencyProperty ShadowForegroundProperty =
        DependencyProperty.Register("ShadowForeground", typeof(TuiColor), typeof(Button), TuiColor.DarkGray);

    /// <summary>
    /// Foreground color used when rendering shaded shadow characters
    /// (<see cref="ButtonShadowStyle.Light"/>, <see cref="ButtonShadowStyle.Medium"/>,
    /// <see cref="ButtonShadowStyle.Dark"/>, <see cref="ButtonShadowStyle.Cast"/>).
    /// </summary>
    public TuiColor ShadowForeground
    {
        get => (TuiColor)GetValue(ShadowForegroundProperty);
        set => SetValue(ShadowForegroundProperty, value);
    }

    public static readonly DependencyProperty ShadowBackgroundProperty =
        DependencyProperty.Register("ShadowBackground", typeof(TuiColor), typeof(Button), TuiColor.Black);

    /// <summary>
    /// Background color of the shadow cells. The classic DOS look uses
    /// <see cref="TuiColor.Black"/> to produce a solid void shadow.
    /// </summary>
    public TuiColor ShadowBackground
    {
        get => (TuiColor)GetValue(ShadowBackgroundProperty);
        set => SetValue(ShadowBackgroundProperty, value);
    }

    public static readonly DependencyProperty ShadowOffsetXProperty =
        DependencyProperty.Register("ShadowOffsetX", typeof(int), typeof(Button), 2);

    /// <summary>
    /// Horizontal extent of the shadow in character cells. Defaults to 2 because
    /// terminal cells are taller than wide; a 2-cell-wide right shadow visually
    /// matches a 1-cell-tall bottom shadow, mirroring Turbo Vision dialogs.
    /// </summary>
    public int ShadowOffsetX
    {
        get => (int)GetValue(ShadowOffsetXProperty);
        set => SetValue(ShadowOffsetXProperty, value);
    }

    public static readonly DependencyProperty ShadowOffsetYProperty =
        DependencyProperty.Register("ShadowOffsetY", typeof(int), typeof(Button), 1);

    /// <summary>
    /// Vertical extent of the shadow in character cells. Defaults to 1
    /// (DOS-authentic).
    /// </summary>
    public int ShadowOffsetY
    {
        get => (int)GetValue(ShadowOffsetYProperty);
        set => SetValue(ShadowOffsetYProperty, value);
    }

    public static readonly DependencyProperty AnimatePressProperty =
        DependencyProperty.Register("AnimatePress", typeof(bool), typeof(Button), true);

    /// <summary>
    /// When <c>true</c> (the default) a shadowed button plays a click animation: while it
    /// is held down (<see cref="ButtonBase.IsPressed"/>) the whole button shifts right/down
    /// by the shadow extent and its drop shadow is suppressed, so it reads as the button
    /// being pushed "in" onto the surface where its shadow used to fall. Buttons without a
    /// shadow (<see cref="ButtonShadowStyle.None"/>) have nothing to sink into and are
    /// unaffected.
    /// </summary>
    public bool AnimatePress
    {
        get => (bool)GetValue(AnimatePressProperty);
        set => SetValue(AnimatePressProperty, value);
    }

    private int ShadowExtentX => ShadowStyle == ButtonShadowStyle.None ? 0 : Math.Max(0, ShadowOffsetX);
    private int ShadowExtentY => ShadowStyle == ButtonShadowStyle.None ? 0 : Math.Max(0, ShadowOffsetY);

    // While the button is held down it "sinks" into its drop shadow: the button shifts
    // right/down by the shadow extent and the shadow is not drawn. Only meaningful when a
    // shadow is actually reserved, so this stays false for ButtonShadowStyle.None.
    private bool IsPressedIntoShadow =>
        AnimatePress && IsPressed && ShadowStyle != ButtonShadowStyle.None &&
        (ShadowExtentX > 0 || ShadowExtentY > 0);

    // When BoxStyle is None the button is rendered as a flat label with one space
    // before and after the content (DOS dialog "[ OK ]" look without brackets) and
    // no extra rows above/below. Border itself reserves zero space in this mode,
    // so the button reserves the side-padding here.
    private int BorderlessInsetX => BoxStyle == BoxStyle.None ? 2 : 0;
    private int BorderlessInsetY => 0;

    // Layout & Render overrides to reserve shadow + borderless side-padding

    protected override Size MeasureOverride(Size availableSize)
    {
        if (TemplateRoot == null)
            return base.MeasureOverride(availableSize);

        int sx = ShadowExtentX;
        int sy = ShadowExtentY;
        int bx = BorderlessInsetX;
        int by = BorderlessInsetY;

        if (sx == 0 && sy == 0 && bx == 0 && by == 0)
            return base.MeasureOverride(availableSize);

        var padding = Padding;
        int paddingW = padding.Left + padding.Right;
        int paddingH = padding.Top + padding.Bottom;

        int reservedW = sx + bx + paddingW;
        int reservedH = sy + by + paddingH;

        var innerAvailable = new Size(
            Math.Max(0, availableSize.Width - reservedW),
            Math.Max(0, availableSize.Height - reservedH));

        TemplateRoot.Measure(innerAvailable);

        return new Size(
            TemplateRoot.DesiredSize.Width + reservedW,
            TemplateRoot.DesiredSize.Height + reservedH);
    }

    protected override void ArrangeOverride(Size finalSize)
    {
        if (TemplateRoot == null) return;

        int sx = ShadowExtentX;
        int sy = ShadowExtentY;
        int bx = BorderlessInsetX;
        int by = BorderlessInsetY;

        if (sx == 0 && sy == 0 && bx == 0 && by == 0)
        {
            base.ArrangeOverride(finalSize);
            return;
        }

        var padding = Padding;
        int paddingW = padding.Left + padding.Right;
        int paddingH = padding.Top + padding.Bottom;

        int innerWidth = Math.Max(0, finalSize.Width - sx - bx - paddingW);
        int innerHeight = Math.Max(0, finalSize.Height - sy - by - paddingH);

        // Borderless inset is centered (1 char each side); shadow is right/bottom only.
        int leftOffset = padding.Left + bx / 2;
        int topOffset = padding.Top + by / 2;

        TemplateRoot.Arrange(new Rect(leftOffset, topOffset, innerWidth, innerHeight));
    }

    public override void Render(VirtualBuffer buffer, int offsetX, int offsetY)
    {
        int sx = ShadowExtentX;
        int sy = ShadowExtentY;

        // Pressed click animation: the button drops onto its shadow. We skip painting the
        // shadow and shift the button body by the shadow extent so it lands exactly where
        // the shadow used to be -- the classic DOS "button pushed in" effect.
        if (IsPressedIntoShadow)
        {
            FillBorderlessInsetBackground(buffer, offsetX + sx, offsetY + sy);
            base.Render(buffer, offsetX + sx, offsetY + sy);
            return;
        }

        if ((sx > 0 || sy > 0) && ShadowStyle != ButtonShadowStyle.None)
        {
            int x = RenderSize.X + offsetX;
            int y = RenderSize.Y + offsetY;
            int btnW = Math.Max(0, RenderSize.Width - sx);
            int btnH = Math.Max(0, RenderSize.Height - sy);
            RenderShadow(buffer, x, y, btnW, btnH, sx, sy);
        }

        FillBorderlessInsetBackground(buffer, offsetX, offsetY);

        base.Render(buffer, offsetX, offsetY);
    }

    // BoxStyle.None reserves BorderlessInsetX columns of side padding that TemplateRoot
    // (Border) is deliberately arranged narrower than -- see ArrangeOverride -- so the
    // label stays centered with exactly one padding column on each side regardless of
    // content length. Border only paints its own arranged rect (never the full control),
    // so without this fill those padding columns showed whatever was behind the button
    // instead of the button's own face color. Since the shadow is sized to the control's
    // full reserved width (including this padding), the unfilled columns previously made
    // the shadow look like it overhung the visibly-painted face by more than ShadowOffsetX.
    private void FillBorderlessInsetBackground(VirtualBuffer buffer, int offsetX, int offsetY)
    {
        if (BorderlessInsetX <= 0) return;

        int sx = ShadowExtentX;
        int sy = ShadowExtentY;
        int w = Math.Max(0, RenderSize.Width - sx);
        int h = Math.Max(0, RenderSize.Height - sy);
        if (w <= 0 || h <= 0) return;

        int x = RenderSize.X + offsetX;
        int y = RenderSize.Y + offsetY;
        buffer.FillRect(x, y, w, h, ' ', EffectiveForeground, EffectiveBackground ?? Background ?? TuiColor.Black);
    }

    private void RenderShadow(VirtualBuffer buffer, int x, int y, int btnW, int btnH, int sx, int sy)
    {
        // L-shaped drop shadow: a right strip and a bottom strip, offset by (sx, sy)
        // so neither piece overlaps the button rectangle itself. The corner where the
        // two strips meet (bottom-right of the button's bounding box) is part of the
        // bottom strip; this matches the look used by Turbo Pascal / Quick Basic.

        char ch;
        bool castMode = false;
        bool translucentMode = false;
        switch (ShadowStyle)
        {
            case ButtonShadowStyle.Solid: ch = ' '; break;
            case ButtonShadowStyle.Light: ch = '\u2591'; break;
            case ButtonShadowStyle.Medium: ch = '\u2592'; break;
            case ButtonShadowStyle.Dark: ch = '\u2593'; break;
            case ButtonShadowStyle.Cast:
                ch = ' ';
                castMode = true;
                break;
            case ButtonShadowStyle.Translucent:
                ch = ' ';
                translucentMode = true;
                break;
            default:
                return;
        }

        var fg = ShadowForeground;
        var bg = ShadowBackground;

        // Right strip: starts sy rows below the button top so it doesn't sit above the button
        int rightX = x + btnW;
        int rightY = y + sy;
        int rightH = btnH;

        // Bottom strip: starts sx columns right of the button left so it doesn't sit left of the button.
        // Its column range [sx, sx+btnW-1] already reaches the corner under the right strip
        // (btnW..sx+btnW-1) whenever sx <= btnW, so width is just btnW -- NOT btnW + sx, which
        // would paint sx columns past the control's own reserved right edge (RenderSize.Width - 1)
        // and bleed into whatever sits to the right of the button.
        int bottomX = x + sx;
        int bottomY = y + btnH;
        int bottomW = btnW;

        if (castMode)
        {
            CastShadow(buffer, rightX, rightY, sx, rightH, fg, bg);
            CastShadow(buffer, bottomX, bottomY, bottomW, sy, fg, bg);
        }
        else if (translucentMode)
        {
            // Blend a semi-transparent black over the existing content. The compositor /
            // BlendPixel pipeline handles alpha → fall-through palette on legacy surfaces.
            var shadowOver = new TuiColor(bg.R, bg.G, bg.B, 128);
            TranslucentShadow(buffer, rightX, rightY, sx, rightH, shadowOver);
            TranslucentShadow(buffer, bottomX, bottomY, bottomW, sy, shadowOver);
        }
        else
        {
            if (sx > 0 && rightH > 0)
                buffer.FillRect(rightX, rightY, sx, rightH, ch, fg, bg);
            if (sy > 0 && bottomW > 0)
                buffer.FillRect(bottomX, bottomY, bottomW, sy, ch, fg, bg);
        }
    }

    private static void TranslucentShadow(VirtualBuffer buffer, int x, int y, int w, int h, TuiColor shadow)
    {
        if (w <= 0 || h <= 0) return;
        for (int row = 0; row < h; row++)
        {
            for (int col = 0; col < w; col++)
            {
                var cell = buffer.GetPixel(x + col, y + row);
                buffer.BlendPixel(x + col, y + row, cell.Character, cell.Foreground, shadow);
            }
        }
    }

    private static void CastShadow(VirtualBuffer buffer, int x, int y, int w, int h,
        TuiColor fg, TuiColor bg)
    {
        // Re-render existing buffer cells with the shadow palette so whatever lies
        // beneath the button (typically the parent's background fill) "shows through"
        // dimmed -- the classic translucent Turbo Vision effect.
        for (int row = 0; row < h; row++)
        {
            for (int col = 0; col < w; col++)
            {
                var cell = buffer.GetPixel(x + col, y + row);
                buffer.SetPixel(x + col, y + row, cell.Character, fg, bg);
            }
        }
    }

    // Logic

    protected override void OnPropertyChanged(DependencyProperty dp)
    {
        base.OnPropertyChanged(dp);
        if (dp == BorderColorProperty || dp == FocusedBorderColorProperty ||
            dp == UIElement.ForegroundProperty || dp == FocusedForegroundProperty ||
            dp == HoverBorderColorProperty || dp == HoverForegroundProperty ||
            dp == UIElement.BackgroundProperty || dp == FocusedBackgroundProperty ||
            dp == HoverBackgroundProperty ||
            dp == IsFocusedProperty || dp == IsMouseOverProperty)
        {
            UpdateEffectiveColors();
        }
    }

    // Effective colors are computed snapshots, so theme swaps and (re)attachment must
    // recompute them: theme style values change without per-property notifications.
    protected override void OnThemeChanged()
    {
        base.OnThemeChanged();
        UpdateEffectiveColors();
    }

    protected override void OnParentChanged()
    {
        base.OnParentChanged();
        UpdateEffectiveColors();
    }

    private void UpdateEffectiveColors()
    {
        // Focus wins over hover so keyboard users don't lose the focus highlight
        // when the pointer happens to rest on the focused button.
        if (IsFocused)
        {
            EffectiveBorderColor = FocusedBorderColor;
            EffectiveForeground = FocusedForeground;
            EffectiveBackground = FocusedBackground ?? Background;
        }
        else if (IsMouseOver)
        {
            EffectiveBorderColor = HoverBorderColor;
            EffectiveForeground = HoverForeground;
            EffectiveBackground = HoverBackground ?? Background;
        }
        else
        {
            EffectiveBorderColor = BorderColor;
            EffectiveForeground = Foreground;
            EffectiveBackground = Background;
        }
    }
}
