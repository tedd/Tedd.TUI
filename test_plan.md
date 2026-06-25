1. **Analyze Implementation Metrics**:
   - The user requests updating the "Show Dialog" button to showcase the new drop shadow capability of the `Button` control.
   - We need to modify the "Show Dialog" button across all four UI setups (`Tedd.TUI.Demo.Blazor/Pages/Home.razor`, `Tedd.TUI.Demo.Blazor/Pages/Programmatic.razor`, `Tedd.TUI.Demo/demo.xaml`, and `Tedd.TUI.Demo/Program.cs`) to have a `ShadowStyle` of `Solid` and `ShadowBackground` set to `ConsoleColor.DarkGray`.
   - The XAML and Blazor versions will need support for these properties in their XAML definitions and Blazor wrappers. Wait, does Blazor wrapper support `ShadowStyle` and `ShadowBackground`? We need to check `Tedd.TUI.Platform.Blazor/Components/TuiButton.razor.cs`.

2. **Verify Blazor Component for Button**:
   - Let's read `TuiButton.razor.cs` to see if `ShadowStyle` and `ShadowBackground` are mapped. If not, we'll need to map them.

3. **Check XAML Loader**:
   - XAML loader usually uses Reflection to set properties, so as long as they are simple types or enums/colors, it should work.

4. **Update the Demos**:
   - `demo.xaml`: `<Button Content="Show Dialog" BoxStyle="Single" Click="OnShowDialog" ShadowStyle="Solid" ShadowBackground="DarkGray" />`
   - `Program.cs` (`RunCodeDemo`): `new Button { Content = "Show Dialog", BoxStyle = BoxStyle.Single, ShadowStyle = ButtonShadowStyle.Solid, ShadowBackground = ConsoleColor.DarkGray }`
   - `Home.razor`: `<TuiButton Text="Show Dialog" BoxStyle="BoxStyle.Single" OnClick="ShowDialog" ShadowStyle="ButtonShadowStyle.Solid" ShadowBackground="ConsoleColor.DarkGray" />` (Assuming TuiButton maps it).
   - `Programmatic.razor`: `new Button { Content = "Show Dialog", BoxStyle = BoxStyle.Single, ShadowStyle = ButtonShadowStyle.Solid, ShadowBackground = ConsoleColor.DarkGray }`

5. **Journaling**:
   - Update `.jules/vanguard.md`.

Let's check `TuiButton.cs` or `.razor`.
