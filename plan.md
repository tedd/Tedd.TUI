1.  **Update TuiButton**:
    - Modify `src/Tedd.TUI.Platform.Blazor/Components/TuiButton.cs` to add `ShadowStyle` and `ShadowBackground` parameters.
    - Inside `ApplyProperties`, set `_button.ShadowStyle` and `_button.ShadowBackground` based on these parameters. Default `ShadowStyle` to `ButtonShadowStyle.None` and `ShadowBackground` to `ConsoleColor.Black`. wait, `ShadowBackground` is `TuiColor`. Blazor UI seems to use strings or enums. Let's look at `TuiBorder` for how colors are handled, or just use `TuiColor` or `ConsoleColor`. `ShadowBackground` in `Button` is `TuiColor`. Since `ConsoleColor` is used widely in Blazor Demo (e.g. `Foreground="ConsoleColor.Cyan"`), and `TuiColor` implicitly converts from `ConsoleColor`, we can use `ConsoleColor` as property type if we want, or `TuiColor` struct. `TuiLabel` uses `ConsoleColor?`. Let's see `TuiLabel.cs`.

2.  **Update "Show Dialog" button in all 4 places**:
    - `src/Tedd.TUI.Demo.Blazor/Pages/Home.razor`: Update `<TuiButton Text="Show Dialog" ...>` to include `ShadowStyle="ButtonShadowStyle.Solid"` and `ShadowBackground="ConsoleColor.DarkGray"`.
    - `src/Tedd.TUI.Demo.Blazor/Pages/Programmatic.razor`: Update `var btnDialog = new Button { ... BoxStyle = BoxStyle.Single, ShadowStyle = ButtonShadowStyle.Solid, ShadowBackground = ConsoleColor.DarkGray };`.
    - `src/Tedd.TUI.Demo/demo.xaml`: Update `<Button Content="Show Dialog" BoxStyle="Single" ...>` to include `ShadowStyle="Solid"` and `ShadowBackground="DarkGray"`.
    - `src/Tedd.TUI.Demo/Program.cs`: Update `var btnDialog = new Button { ... BoxStyle = BoxStyle.Single, ShadowStyle = ButtonShadowStyle.Solid, ShadowBackground = ConsoleColor.DarkGray };`.

3.  **Journaling**:
    - Append an entry to `.jules/vanguard.md` about synchronizing the button's drop shadow functionality and parity implementation.

4.  **Complete pre-commit steps to ensure proper testing, verification, review, and reflection are done.**
5.  **Submit**.
