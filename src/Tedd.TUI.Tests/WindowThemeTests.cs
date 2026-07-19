using System;
using Xunit;
using Tedd.TUI;

namespace Tedd.TUI.Tests;

/// <summary>
/// Theme integration for the Window/Dialog family. Uses
/// <see cref="ThemeManager.BeginScope"/> only — the global theme must never be
/// reassigned from tests (parallel test classes share it).
/// </summary>
public class WindowThemeTests
{
    [Fact]
    public void LightTheme_StylesWindowColors()
    {
        using var _ = ThemeManager.BeginScope(TuiThemes.Light);
        var window = new Window();

        Assert.Equal(TuiColor.White, window.BackgroundColor);
        Assert.Equal(TuiColor.Black, window.BorderColor);
        Assert.Equal(TuiColor.Black, window.TitleColor);
    }

    [Fact]
    public void WindowStyle_AppliesToDialogSubclasses()
    {
        using var _ = ThemeManager.BeginScope(TuiThemes.TurboPascal);
        var dialog = new MessageDialog();

        // The Window-targeted theme style must reach every dialog subclass.
        Assert.Equal(BoxStyle.Single, dialog.BoxStyle);
        Assert.Equal(TuiColor.Black, dialog.TitleColor);
    }

    [Fact]
    public void LocalValue_BeatsThemeStyle()
    {
        using var _ = ThemeManager.BeginScope(TuiThemes.Light);
        var window = new Window { BorderColor = TuiColor.Red };

        Assert.Equal(TuiColor.Red, window.BorderColor);
    }

    [Fact]
    public void DarkTheme_KeepsRegistrationDefaults()
    {
        using var _ = ThemeManager.BeginScope(TuiThemes.Dark);
        var window = new Window();

        Assert.Equal(TuiColor.Black, window.BackgroundColor);
        Assert.Equal(TuiColor.White, window.BorderColor);
        Assert.Equal(TuiColor.Yellow, window.TitleColor);
    }
}
