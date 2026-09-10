using Xunit;

namespace Tedd.TUI.Tests;

public class ThemeChangedEventArgsTests
{
    [Theory]
    [InlineData(nameof(TuiThemes.Dark), nameof(TuiThemes.Light))]
    [InlineData(nameof(TuiThemes.Light), nameof(TuiThemes.TurboPascal))]
    [InlineData(nameof(TuiThemes.TurboPascal), nameof(TuiThemes.Dark))]
    public void Constructor_SetsProperties(string oldThemeName, string newThemeName)
    {
        var oldTheme = oldThemeName == nameof(TuiThemes.Dark) ? TuiThemes.Dark : (oldThemeName == nameof(TuiThemes.Light) ? TuiThemes.Light : TuiThemes.TurboPascal);
        var newTheme = newThemeName == nameof(TuiThemes.Dark) ? TuiThemes.Dark : (newThemeName == nameof(TuiThemes.Light) ? TuiThemes.Light : TuiThemes.TurboPascal);

        var args = new ThemeChangedEventArgs(oldTheme, newTheme);

        Assert.Same(oldTheme, args.OldTheme);
        Assert.Same(newTheme, args.NewTheme);
    }
}
