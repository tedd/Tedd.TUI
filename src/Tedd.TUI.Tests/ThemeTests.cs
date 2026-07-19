using System;
using Xunit;
using Tedd.TUI;

namespace Tedd.TUI.Tests;

/// <summary>
/// Tests for the XAML-style theme system: <see cref="TuiTheme"/> implicit styles,
/// <see cref="ThemeManager"/> resolution precedence, the predefined themes, and the
/// theme-change refresh path.
/// </summary>
/// <remarks>
/// Tests never reassign the global <see cref="ThemeManager.Current"/> (other test
/// classes run in parallel against it); they use <see cref="ThemeManager.BeginScope"/>,
/// which overrides the theme for the current async flow only.
/// </remarks>
public class ThemeTests
{
    private static readonly TuiColor DosBlue = TuiColor.FromRgb(0x00, 0x00, 0xAA);
    private static readonly TuiColor DosGreen = TuiColor.FromRgb(0x00, 0xAA, 0x00);
    private static readonly TuiColor DosLightGray = TuiColor.FromRgb(0xAA, 0xAA, 0xAA);

    [Fact]
    public void DefaultThemeIsDark()
    {
        Assert.Equal("Dark", ThemeManager.Current.Name);
        Assert.Same(TuiThemes.Dark, ThemeManager.Current);
    }

    [Fact]
    public void StyleValue_RanksBelowLocal_AboveDefault()
    {
        var theme = new TuiTheme("Test");
        theme.Styles.Add(new Style(typeof(Button)).Set(Button.BorderColorProperty, TuiColor.Red));

        using var _ = ThemeManager.BeginScope(theme);
        var btn = new Button();

        // Theme style beats the registration default (Gray).
        Assert.Equal(TuiColor.Red, btn.BorderColor);

        // A local value beats the theme style.
        btn.BorderColor = TuiColor.Blue;
        Assert.Equal(TuiColor.Blue, btn.BorderColor);

        // Clearing the local value restores the themed value, not the default.
        btn.ClearValue(Button.BorderColorProperty);
        Assert.Equal(TuiColor.Red, btn.BorderColor);
    }

    [Fact]
    public void StyleAppliesToDerivedTypes_MostDerivedWins()
    {
        var theme = new TuiTheme("Test");
        theme.Styles.Add(new Style(typeof(UIElement)).Set(UIElement.ForegroundProperty, TuiColor.Red));
        theme.Styles.Add(new Style(typeof(Button)).Set(UIElement.ForegroundProperty, TuiColor.Green));

        using var _ = ThemeManager.BeginScope(theme);

        Assert.Equal(TuiColor.Green, new Button().Foreground);
        Assert.Equal(TuiColor.Red, new TextBlock().Foreground);
    }

    [Fact]
    public void InheritedProperty_FlowsFromThemedAncestor()
    {
        var theme = new TuiTheme("Test");
        theme.Styles.Add(new Style(typeof(TuiWindow)).Set(UIElement.ForegroundProperty, TuiColor.DarkCyan));

        using var _ = ThemeManager.BeginScope(theme);
        var window = new TuiWindow();
        var text = new TextBlock { Text = "x" };
        window.Content = text;

        Assert.Equal(TuiColor.DarkCyan, text.Foreground);
    }

    [Fact]
    public void ScopeRestoresPreviousThemeOnDispose()
    {
        var outer = ThemeManager.Current;
        using (ThemeManager.BeginScope(TuiThemes.TurboPascal))
        {
            Assert.Same(TuiThemes.TurboPascal, ThemeManager.Current);
            using (ThemeManager.BeginScope(TuiThemes.Light))
            {
                Assert.Same(TuiThemes.Light, ThemeManager.Current);
            }
            Assert.Same(TuiThemes.TurboPascal, ThemeManager.Current);
        }
        Assert.Same(outer, ThemeManager.Current);
    }

    [Fact]
    public void DarkTheme_ButtonHasVisibleFill()
    {
        // The user-visible regression this system fixes: default buttons used to render
        // as black boxes (Border fills Background ?? Black) on a black desktop.
        var btn = new Button { Content = "OK" };
        btn.Measure(new Size(100, 100));
        btn.Arrange(new Rect(0, 0, btn.DesiredSize.Width, btn.DesiredSize.Height));

        var buffer = new VirtualBuffer(btn.DesiredSize.Width, btn.DesiredSize.Height);
        btn.Render(buffer, 0, 0);

        var textCell = buffer.GetPixel(1, 1); // 'O'
        Assert.Equal('O', textCell.Character);
        Assert.Equal(TuiColor.DarkGray, textCell.Background);
        Assert.NotEqual(textCell.Background, textCell.Foreground);
    }

    [Fact]
    public void DarkTheme_TextBoxHasVisibleFill()
    {
        var tb = new TextBox { Text = "hi", Width = 5 };
        tb.Measure(new Size(10, 1));
        tb.Arrange(new Rect(0, 0, 5, 1));

        var buffer = new VirtualBuffer(5, 1);
        tb.Render(buffer, 0, 0);

        var cell = buffer.GetPixel(0, 0);
        Assert.Equal('h', cell.Character);
        Assert.Equal(TuiColor.DarkBlue, cell.Background);
        Assert.Equal(TuiColor.White, cell.Foreground);
    }

    [Fact]
    public void TextBox_UsesThemedFocusAndCaretColors()
    {
        var theme = new TuiTheme("Test");
        theme.Styles.Add(new Style(typeof(TextBox))
            .Set(TextBox.FocusedBackgroundProperty, TuiColor.DarkRed)
            .Set(TextBox.CaretBackgroundProperty, TuiColor.Magenta));

        using var _ = ThemeManager.BeginScope(theme);
        var tb = new TextBox { Text = "ab", Width = 5, IsFocused = true };
        tb.Measure(new Size(10, 1));
        tb.Arrange(new Rect(0, 0, 5, 1));

        var buffer = new VirtualBuffer(5, 1);
        tb.Render(buffer, 0, 0);

        Assert.Equal(TuiColor.DarkRed, buffer.GetPixel(0, 0).Background);
        // Programmatic Text set leaves the caret at the end (index 2).
        Assert.Equal(TuiColor.Magenta, buffer.GetPixel(2, 0).Background);
    }

    [Fact]
    public void TurboPascal_ButtonIsFlatGreenWithSolidShadow()
    {
        using var _ = ThemeManager.BeginScope(TuiThemes.TurboPascal);
        var btn = new Button { Content = "OK" };

        Assert.Equal(BoxStyle.None, btn.BoxStyle);
        Assert.Equal(ButtonShadowStyle.Solid, btn.ShadowStyle);

        btn.Measure(new Size(100, 100));
        btn.Arrange(new Rect(0, 0, btn.DesiredSize.Width, btn.DesiredSize.Height));

        var buffer = new VirtualBuffer(btn.DesiredSize.Width, btn.DesiredSize.Height);
        btn.Render(buffer, 0, 0);

        // Flat green face with black text on the content row...
        var textCell = buffer.GetPixel(1, 0);
        Assert.Equal('O', textCell.Character);
        Assert.Equal(DosGreen, textCell.Background);
        Assert.Equal(TuiColor.Black, textCell.Foreground);

        // ...and a solid black shadow row below.
        Assert.Equal(TuiColor.Black, buffer.GetPixel(2, 1).Background);
    }

    [Fact]
    public void QuickBasic_ButtonInvertsOnFocus()
    {
        using var _ = ThemeManager.BeginScope(TuiThemes.QuickBasic);
        var btn = new Button();

        Assert.Equal(BoxStyle.None, btn.BoxStyle);
        Assert.Equal(ButtonShadowStyle.None, btn.ShadowStyle);
        Assert.Equal(DosLightGray, btn.EffectiveBackground);
        Assert.Equal(TuiColor.Black, btn.EffectiveForeground);

        btn.IsFocused = true;
        Assert.Equal(new TuiColor?(TuiColor.Black), btn.EffectiveBackground);
        Assert.Equal(TuiColor.FromRgb(0xFF, 0xFF, 0xFF), btn.EffectiveForeground);
    }

    [Fact]
    public void LightTheme_StylesResolveOnControls()
    {
        using var _ = ThemeManager.BeginScope(TuiThemes.Light);

        var btn = new Button();
        Assert.Equal(new TuiColor?(TuiColor.White), btn.Background);
        Assert.Equal(TuiColor.DarkBlue, btn.FocusedForeground);

        var cb = new CheckBox();
        Assert.Equal(TuiColor.DarkGreen, cb.CheckColor);

        var dialog = new DialogBox();
        Assert.Equal(TuiColor.White, dialog.BackgroundColor);
        Assert.Equal(TuiColor.Black, dialog.BorderColor);
    }

    [Fact]
    public void TurboPascal_WindowPaintsBlueDesktop()
    {
        using var _ = ThemeManager.BeginScope(TuiThemes.TurboPascal);
        var window = new TuiWindow();
        window.Measure(new Size(10, 4));
        window.Arrange(new Rect(0, 0, 10, 4));

        var buffer = new VirtualBuffer(10, 4);
        window.Render(buffer, 0, 0);

        Assert.Equal(DosBlue, buffer.GetPixel(5, 2).Background);
    }

    [Fact]
    public void ThemeChange_RefreshesCachedEffectiveColorsAndInvalidates()
    {
        var theme = new TuiTheme("Test");
        theme.Styles.Add(new Style(typeof(Button)).Set(UIElement.BackgroundProperty, TuiColor.Red));

        var window = new TuiWindow();
        var btn = new Button { Content = "X" };
        window.Content = btn;

        // Under the default Dark theme the button caches a DarkGray fill.
        Assert.Equal(new TuiColor?(TuiColor.DarkGray), btn.EffectiveBackground);

        bool invalidated = false;
        window.VisualChanged += (_, _) => invalidated = true;

        using (ThemeManager.BeginScope(theme))
        {
            // Drive the same refresh path ThemeManager uses on a global theme swap.
            window.OnGlobalThemeChanged();
            Assert.Equal(new TuiColor?(TuiColor.Red), btn.EffectiveBackground);
        }

        Assert.True(invalidated);
    }

    [Fact]
    public void AssigningSameThemeInstance_DoesNotRaiseThemeChanged()
    {
        bool raised = false;
        EventHandler<ThemeChangedEventArgs> handler = (_, _) => raised = true;
        ThemeManager.ThemeChanged += handler;
        try
        {
            ThemeManager.Current = TuiThemes.Dark; // already current: must be a no-op
        }
        finally
        {
            ThemeManager.ThemeChanged -= handler;
        }

        Assert.False(raised);
    }

    [Fact]
    public void GuardClauses()
    {
        Assert.Throws<ArgumentNullException>(() => ThemeManager.Current = null!);
        Assert.Throws<ArgumentException>(() => new TuiTheme(" "));
        Assert.Throws<ArgumentNullException>(() => ThemeManager.BeginScope(null!));
    }

    [Fact]
    public void FromName_ResolvesPredefinedThemesCaseInsensitively()
    {
        Assert.Same(TuiThemes.Dark, TuiThemes.FromName("dark"));
        Assert.Same(TuiThemes.Light, TuiThemes.FromName("LIGHT"));
        Assert.Same(TuiThemes.TurboPascal, TuiThemes.FromName("TurboPascal"));
        Assert.Same(TuiThemes.QuickBasic, TuiThemes.FromName(" quickbasic "));
        Assert.Null(TuiThemes.FromName("no-such-theme"));
        Assert.Null(TuiThemes.FromName(null!));
    }

    [Fact]
    public void MutatedTheme_AppliesAfterInvalidateCache()
    {
        var theme = new TuiTheme("Test");
        theme.Styles.Add(new Style(typeof(Button)).Set(Button.BorderColorProperty, TuiColor.Red));

        using var _ = ThemeManager.BeginScope(theme);
        var btn = new Button();
        Assert.Equal(TuiColor.Red, btn.BorderColor);

        theme.Styles[0].Setters[0].Value = TuiColor.Green;
        theme.InvalidateCache();
        Assert.Equal(TuiColor.Green, btn.BorderColor);
    }
}
