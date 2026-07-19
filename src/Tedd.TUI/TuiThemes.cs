using System;

namespace Tedd.TUI;

/// <summary>
/// The predefined themes shipped with Tedd.TUI. Assign one to
/// <see cref="ThemeManager.Current"/> to restyle the application:
/// <code>ThemeManager.Current = TuiThemes.TurboPascal;</code>
/// </summary>
/// <remarks>
/// <list type="bullet">
/// <item><see cref="Dark"/> — the default: today's palette plus visible fills for
/// buttons and input boxes.</item>
/// <item><see cref="Light"/> — dark text on a light gray desktop, Windows-3.x flavored.</item>
/// <item><see cref="TurboPascal"/> — Borland Turbo Pascal 7 IDE: blue desktop, gray
/// dialogs, green buttons with solid drop shadows.</item>
/// <item><see cref="QuickBasic"/> — Microsoft QBasic/QuickBASIC IDE: blue desktop,
/// gray dialogs, flat gray buttons.</item>
/// </list>
/// The retro themes use the authentic 16-color DOS/VGA RGB values; on 16-color hosts
/// they quantize back onto the classic console palette.
/// </remarks>
public static class TuiThemes
{
    private static readonly Lazy<TuiTheme> _dark = new(CreateDark);
    private static readonly Lazy<TuiTheme> _light = new(CreateLight);
    private static readonly Lazy<TuiTheme> _turboPascal = new(CreateTurboPascal);
    private static readonly Lazy<TuiTheme> _quickBasic = new(CreateQuickBasic);

    public static TuiTheme Dark => _dark.Value;
    public static TuiTheme Light => _light.Value;
    public static TuiTheme TurboPascal => _turboPascal.Value;
    public static TuiTheme QuickBasic => _quickBasic.Value;

    /// <summary>
    /// Case-insensitive lookup of a predefined theme by name ("Dark", "Light",
    /// "TurboPascal", "QuickBasic"); returns null for unknown names.
    /// </summary>
    public static TuiTheme? FromName(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return null;
        return name.Trim().ToLowerInvariant() switch
        {
            "dark" => Dark,
            "light" => Light,
            "turbopascal" => TurboPascal,
            "quickbasic" => QuickBasic,
            _ => null,
        };
    }

    // Authentic DOS/VGA palette entries used by the retro themes.
    private static readonly TuiColor DosBlue = TuiColor.FromRgb(0x00, 0x00, 0xAA);
    private static readonly TuiColor DosGreen = TuiColor.FromRgb(0x00, 0xAA, 0x00);
    private static readonly TuiColor DosCyan = TuiColor.FromRgb(0x00, 0xAA, 0xAA);
    private static readonly TuiColor DosLightGray = TuiColor.FromRgb(0xAA, 0xAA, 0xAA);
    private static readonly TuiColor DosDarkGray = TuiColor.FromRgb(0x55, 0x55, 0x55);
    private static readonly TuiColor DosYellow = TuiColor.FromRgb(0xFF, 0xFF, 0x55);
    private static readonly TuiColor DosWhite = TuiColor.FromRgb(0xFF, 0xFF, 0xFF);

    private static TuiTheme CreateDark()
    {
        var t = new TuiTheme("Dark");

        // The Dark theme keeps the historical control palette (tests and existing apps
        // rely on it) but gives buttons and input boxes a visible fill so they no longer
        // render as black-on-black boxes on the default black desktop.
        t.Styles.Add(new Style(typeof(Button))
            .Set(UIElement.BackgroundProperty, TuiColor.DarkGray));

        t.Styles.Add(new Style(typeof(TextBox))
            .Set(UIElement.BackgroundProperty, TuiColor.DarkBlue));

        t.Styles.Add(new Style(typeof(PasswordBox))
            .Set(UIElement.BackgroundProperty, TuiColor.DarkBlue));

        return t;
    }

    private static TuiTheme CreateLight()
    {
        var t = new TuiTheme("Light");

        t.Styles.Add(new Style(typeof(TuiWindow))
            .Set(UIElement.BackgroundProperty, TuiColor.Gray)
            .Set(UIElement.ForegroundProperty, TuiColor.Black));

        t.Styles.Add(new Style(typeof(Button))
            .Set(UIElement.BackgroundProperty, TuiColor.White)
            .Set(Button.BorderColorProperty, TuiColor.DarkGray)
            .Set(Button.FocusedForegroundProperty, TuiColor.DarkBlue)
            .Set(Button.FocusedBorderColorProperty, TuiColor.DarkBlue)
            .Set(Button.HoverForegroundProperty, TuiColor.DarkCyan)
            .Set(Button.HoverBorderColorProperty, TuiColor.DarkCyan)
            .Set(Button.ShadowForegroundProperty, TuiColor.DarkGray));

        t.Styles.Add(new Style(typeof(TextBox))
            .Set(UIElement.BackgroundProperty, TuiColor.White)
            .Set(TextBox.FocusedForegroundProperty, TuiColor.Black)
            .Set(TextBox.FocusedBackgroundProperty, TuiColor.White)
            .Set(TextBox.SelectionForegroundProperty, TuiColor.White)
            .Set(TextBox.SelectionBackgroundProperty, TuiColor.DarkBlue)
            .Set(TextBox.CaretForegroundProperty, TuiColor.White)
            .Set(TextBox.CaretBackgroundProperty, TuiColor.Black));

        t.Styles.Add(new Style(typeof(PasswordBox))
            .Set(UIElement.BackgroundProperty, TuiColor.White));

        t.Styles.Add(new Style(typeof(CheckBox))
            .Set(CheckBox.FocusedForegroundProperty, TuiColor.DarkBlue)
            .Set(CheckBox.HoverForegroundProperty, TuiColor.DarkCyan)
            .Set(CheckBox.CheckColorProperty, TuiColor.DarkGreen)
            .Set(CheckBox.BracketColorProperty, TuiColor.DarkGray));

        t.Styles.Add(new Style(typeof(RadioButton))
            .Set(RadioButton.FocusedForegroundProperty, TuiColor.DarkBlue)
            .Set(RadioButton.HoverForegroundProperty, TuiColor.DarkCyan)
            .Set(RadioButton.CheckColorProperty, TuiColor.DarkGreen)
            .Set(RadioButton.BracketColorProperty, TuiColor.DarkGray));

        t.Styles.Add(new Style(typeof(ToggleSwitch))
            .Set(ToggleSwitch.FocusedForegroundProperty, TuiColor.DarkBlue)
            .Set(ToggleSwitch.HoverForegroundProperty, TuiColor.DarkCyan)
            .Set(ToggleSwitch.BracketColorProperty, TuiColor.DarkGray)
            .Set(ToggleSwitch.KnobColorProperty, TuiColor.White)
            .Set(ToggleSwitch.OnKnobColorProperty, TuiColor.DarkGreen));

        t.Styles.Add(new Style(typeof(DialogBox))
            .Set(DialogBox.BackgroundColorProperty, TuiColor.White)
            .Set(DialogBox.BorderColorProperty, TuiColor.Black)
            .Set(DialogBox.TitleColorProperty, TuiColor.Black)
            .Set(UIElement.ForegroundProperty, TuiColor.Black));

        t.Styles.Add(new Style(typeof(ListBox))
            .Set(ListBox.SelectionForegroundProperty, TuiColor.White)
            .Set(ListBox.SelectionBackgroundProperty, TuiColor.DarkBlue)
            .Set(ListBox.FocusedSelectionForegroundProperty, TuiColor.White)
            .Set(ListBox.FocusedSelectionBackgroundProperty, TuiColor.Blue));

        t.Styles.Add(new Style(typeof(GroupBox))
            .Set(GroupBox.BorderColorProperty, TuiColor.DarkGray));

        t.Styles.Add(new Style(typeof(ComboBox))
            .Set(ComboBox.FocusedForegroundProperty, TuiColor.DarkBlue)
            .Set(ComboBox.HoverForegroundProperty, TuiColor.DarkCyan)
            .Set(ComboBox.PopupBackgroundProperty, TuiColor.White)
            .Set(ComboBox.PopupBorderColorProperty, TuiColor.Black));

        t.Styles.Add(new Style(typeof(ProgressBar))
            .Set(ProgressBar.ProgressColorProperty, TuiColor.DarkGreen)
            .Set(ProgressBar.EmptyColorProperty, TuiColor.Gray));

        t.Styles.Add(new Style(typeof(Slider))
            .Set(Slider.TrackColorProperty, TuiColor.DarkGray)
            .Set(Slider.ThumbColorProperty, TuiColor.Black)
            .Set(Slider.FocusedThumbColorProperty, TuiColor.DarkBlue)
            .Set(Slider.HoverThumbColorProperty, TuiColor.DarkCyan));

        t.Styles.Add(new Style(typeof(NumericUpDown))
            .Set(NumericUpDown.FocusedForegroundProperty, TuiColor.DarkBlue)
            .Set(NumericUpDown.HoverForegroundProperty, TuiColor.DarkCyan)
            .Set(NumericUpDown.ButtonColorProperty, TuiColor.DarkGray));

        return t;
    }

    private static TuiTheme CreateTurboPascal()
    {
        var t = new TuiTheme("TurboPascal");

        t.Styles.Add(new Style(typeof(TuiWindow))
            .Set(UIElement.BackgroundProperty, DosBlue)
            .Set(UIElement.ForegroundProperty, DosYellow));

        t.Styles.Add(new Style(typeof(DialogBox))
            .Set(DialogBox.BackgroundColorProperty, DosLightGray)
            .Set(DialogBox.BorderColorProperty, DosWhite)
            .Set(DialogBox.TitleColorProperty, TuiColor.Black)
            .Set(DialogBox.BoxStyleProperty, BoxStyle.Single)
            .Set(UIElement.ForegroundProperty, TuiColor.Black));

        // TP dialog buttons: black text on a green face, no frame, solid black shadow.
        t.Styles.Add(new Style(typeof(Button))
            .Set(UIElement.BackgroundProperty, DosGreen)
            .Set(UIElement.ForegroundProperty, TuiColor.Black)
            .Set(Button.FocusedForegroundProperty, DosWhite)
            .Set(Button.HoverForegroundProperty, DosWhite)
            .Set(Button.BoxStyleProperty, BoxStyle.None)
            .Set(Button.ShadowStyleProperty, ButtonShadowStyle.Solid)
            .Set(Button.ShadowBackgroundProperty, TuiColor.Black));

        t.Styles.Add(new Style(typeof(TextBox))
            .Set(UIElement.BackgroundProperty, DosBlue)
            .Set(UIElement.ForegroundProperty, DosYellow)
            .Set(TextBox.FocusedForegroundProperty, DosYellow)
            .Set(TextBox.FocusedBackgroundProperty, DosBlue)
            .Set(TextBox.SelectionForegroundProperty, TuiColor.Black)
            .Set(TextBox.SelectionBackgroundProperty, DosCyan)
            .Set(TextBox.CaretForegroundProperty, TuiColor.Black)
            .Set(TextBox.CaretBackgroundProperty, DosLightGray));

        t.Styles.Add(new Style(typeof(PasswordBox))
            .Set(UIElement.BackgroundProperty, DosBlue)
            .Set(UIElement.ForegroundProperty, DosYellow));

        t.Styles.Add(new Style(typeof(CheckBox))
            .Set(CheckBox.FocusedForegroundProperty, DosWhite)
            .Set(CheckBox.HoverForegroundProperty, DosWhite)
            .Set(CheckBox.CheckColorProperty, TuiColor.Black)
            .Set(CheckBox.BracketColorProperty, TuiColor.Black));

        t.Styles.Add(new Style(typeof(RadioButton))
            .Set(RadioButton.FocusedForegroundProperty, DosWhite)
            .Set(RadioButton.HoverForegroundProperty, DosWhite)
            .Set(RadioButton.CheckColorProperty, TuiColor.Black)
            .Set(RadioButton.BracketColorProperty, TuiColor.Black));

        t.Styles.Add(new Style(typeof(ToggleSwitch))
            .Set(ToggleSwitch.FocusedForegroundProperty, DosWhite)
            .Set(ToggleSwitch.HoverForegroundProperty, DosWhite)
            .Set(ToggleSwitch.BracketColorProperty, TuiColor.Black)
            .Set(ToggleSwitch.TrackColorProperty, DosCyan)
            .Set(ToggleSwitch.KnobColorProperty, DosWhite)
            .Set(ToggleSwitch.OnKnobColorProperty, DosGreen));

        t.Styles.Add(new Style(typeof(ListBox))
            .Set(ListBox.SelectionForegroundProperty, TuiColor.Black)
            .Set(ListBox.SelectionBackgroundProperty, DosCyan)
            .Set(ListBox.FocusedSelectionForegroundProperty, DosWhite)
            .Set(ListBox.FocusedSelectionBackgroundProperty, DosGreen));

        t.Styles.Add(new Style(typeof(GroupBox))
            .Set(GroupBox.BorderColorProperty, TuiColor.Black));

        t.Styles.Add(new Style(typeof(ComboBox))
            .Set(ComboBox.FocusedForegroundProperty, DosWhite)
            .Set(ComboBox.HoverForegroundProperty, DosWhite)
            .Set(ComboBox.PopupBackgroundProperty, DosLightGray)
            .Set(ComboBox.PopupBorderColorProperty, TuiColor.Black));

        t.Styles.Add(new Style(typeof(ProgressBar))
            .Set(ProgressBar.ProgressColorProperty, DosCyan)
            .Set(ProgressBar.EmptyColorProperty, DosDarkGray));

        t.Styles.Add(new Style(typeof(Slider))
            .Set(Slider.TrackColorProperty, DosCyan)
            .Set(Slider.ThumbColorProperty, DosBlue)
            .Set(Slider.FocusedThumbColorProperty, DosYellow)
            .Set(Slider.HoverThumbColorProperty, DosWhite));

        return t;
    }

    private static TuiTheme CreateQuickBasic()
    {
        var t = new TuiTheme("QuickBasic");

        t.Styles.Add(new Style(typeof(TuiWindow))
            .Set(UIElement.BackgroundProperty, DosBlue)
            .Set(UIElement.ForegroundProperty, DosLightGray));

        t.Styles.Add(new Style(typeof(DialogBox))
            .Set(DialogBox.BackgroundColorProperty, DosLightGray)
            .Set(DialogBox.BorderColorProperty, TuiColor.Black)
            .Set(DialogBox.TitleColorProperty, TuiColor.Black)
            .Set(DialogBox.BoxStyleProperty, BoxStyle.Single)
            .Set(UIElement.ForegroundProperty, TuiColor.Black));

        // QB dialog buttons: flat "< OK >"-style black-on-gray text; focus inverts.
        t.Styles.Add(new Style(typeof(Button))
            .Set(UIElement.BackgroundProperty, DosLightGray)
            .Set(UIElement.ForegroundProperty, TuiColor.Black)
            .Set(Button.FocusedForegroundProperty, DosWhite)
            .Set(Button.FocusedBackgroundProperty, TuiColor.Black)
            .Set(Button.HoverForegroundProperty, DosWhite)
            .Set(Button.HoverBackgroundProperty, DosDarkGray)
            .Set(Button.BoxStyleProperty, BoxStyle.None)
            .Set(Button.ShadowStyleProperty, ButtonShadowStyle.None));

        t.Styles.Add(new Style(typeof(TextBox))
            .Set(UIElement.BackgroundProperty, DosCyan)
            .Set(UIElement.ForegroundProperty, TuiColor.Black)
            .Set(TextBox.FocusedForegroundProperty, DosWhite)
            .Set(TextBox.FocusedBackgroundProperty, DosCyan)
            .Set(TextBox.SelectionForegroundProperty, DosWhite)
            .Set(TextBox.SelectionBackgroundProperty, TuiColor.Black)
            .Set(TextBox.CaretForegroundProperty, TuiColor.Black)
            .Set(TextBox.CaretBackgroundProperty, DosWhite));

        t.Styles.Add(new Style(typeof(PasswordBox))
            .Set(UIElement.BackgroundProperty, DosCyan)
            .Set(UIElement.ForegroundProperty, TuiColor.Black));

        t.Styles.Add(new Style(typeof(CheckBox))
            .Set(CheckBox.FocusedForegroundProperty, DosWhite)
            .Set(CheckBox.HoverForegroundProperty, DosWhite)
            .Set(CheckBox.CheckColorProperty, TuiColor.Black)
            .Set(CheckBox.BracketColorProperty, TuiColor.Black));

        t.Styles.Add(new Style(typeof(RadioButton))
            .Set(RadioButton.FocusedForegroundProperty, DosWhite)
            .Set(RadioButton.HoverForegroundProperty, DosWhite)
            .Set(RadioButton.CheckColorProperty, TuiColor.Black)
            .Set(RadioButton.BracketColorProperty, TuiColor.Black));

        t.Styles.Add(new Style(typeof(ToggleSwitch))
            .Set(ToggleSwitch.FocusedForegroundProperty, DosWhite)
            .Set(ToggleSwitch.HoverForegroundProperty, DosWhite)
            .Set(ToggleSwitch.BracketColorProperty, TuiColor.Black)
            .Set(ToggleSwitch.TrackColorProperty, DosCyan)
            .Set(ToggleSwitch.KnobColorProperty, DosWhite)
            .Set(ToggleSwitch.OnKnobColorProperty, DosGreen));

        t.Styles.Add(new Style(typeof(ListBox))
            .Set(ListBox.SelectionForegroundProperty, DosWhite)
            .Set(ListBox.SelectionBackgroundProperty, TuiColor.Black)
            .Set(ListBox.FocusedSelectionForegroundProperty, DosWhite)
            .Set(ListBox.FocusedSelectionBackgroundProperty, TuiColor.Black));

        t.Styles.Add(new Style(typeof(GroupBox))
            .Set(GroupBox.BorderColorProperty, TuiColor.Black));

        t.Styles.Add(new Style(typeof(ComboBox))
            .Set(ComboBox.PopupBackgroundProperty, DosLightGray)
            .Set(ComboBox.PopupBorderColorProperty, TuiColor.Black));

        t.Styles.Add(new Style(typeof(ProgressBar))
            .Set(ProgressBar.ProgressColorProperty, DosCyan)
            .Set(ProgressBar.EmptyColorProperty, DosDarkGray));

        t.Styles.Add(new Style(typeof(Slider))
            .Set(Slider.TrackColorProperty, DosCyan));

        return t;
    }
}
