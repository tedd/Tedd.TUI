namespace Tedd.TUI.HumanTests.RenderTargets;

/// <summary>
/// Rendering targets this executable cannot host in-process (they require their own
/// application model). Selecting them prints how to test the host instead.
/// </summary>
public static class InfoTargets
{
    public static void WinUi()
    {
        System.Console.WriteLine("WinUI 3 cannot be hosted from this console executable (it needs a Windows");
        System.Console.WriteLine("App SDK application, and its XAML stack conflicts with the WPF target).");
        System.Console.WriteLine();
        System.Console.WriteLine("To test it, add Tedd.TUI.Platform.WinUI's TuiHostControl to a Windows App SDK");
        System.Console.WriteLine("app (font property is MonoFontFamily) - see docs/platforms/winui.md.");
        System.Console.WriteLine();
        SharedPipelineNote();
    }

    public static void Maui()
    {
        System.Console.WriteLine(".NET MAUI cannot be hosted from this console executable (it needs the MAUI");
        System.Console.WriteLine("application model and platform packaging).");
        System.Console.WriteLine();
        System.Console.WriteLine("To test it, call UseTeddTui() in a MAUI app and add TuiHostView; keyboard");
        System.Console.WriteLine("input is injected via SendKey/SendText - see docs/platforms/maui.md.");
        System.Console.WriteLine();
        SharedPipelineNote();
    }

    private static void SharedPipelineNote()
    {
        System.Console.WriteLine("Note: this host paints with the same TuiSurfaceController + SkiaCellSurface");
        System.Console.WriteLine("pipeline as the 'avalonia' and 'skia' targets, so the drawing path itself is");
        System.Console.WriteLine("covered by those targets.");
    }
}
