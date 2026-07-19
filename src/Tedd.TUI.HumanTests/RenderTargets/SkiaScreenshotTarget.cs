using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using Tedd.TUI.HumanTests.Infrastructure;
using Tedd.TUI.Platform.Skia;

namespace Tedd.TUI.HumanTests.RenderTargets;

/// <summary>
/// Exercises the standalone Skia host (<c>Tedd.TUI.Platform.Skia</c>): renders the
/// selection screen and one frame of every test page headlessly to PNG files for visual
/// review, then opens the output folder. The host has no windowing or input of its own;
/// interactive testing of the same Skia cell surface happens via the Avalonia target.
/// </summary>
public static class SkiaScreenshotTarget
{
    private const int Columns = 100;
    private const int Rows = 34;

    public static void Run()
    {
        var outputDir = Path.Combine(Environment.CurrentDirectory, "skia_screenshots");
        Directory.CreateDirectory(outputDir);

        using var host = new TuiSkiaHost();

        // Selection screen exactly as the interactive targets present it.
        host.SetContent(TestSession.CreateWindow());
        host.RenderToPng(Path.Combine(outputDir, "00_SelectionScreen.png"), Columns, Rows);

        // One frame of every test page.
        var tests = TestDiscovery.GetAllTests();
        for (var i = 0; i < tests.Count; i++)
        {
            var window = new TuiWindow { Content = tests[i].BuildPage() };
            host.SetContent(window);
            host.RenderToPng(Path.Combine(outputDir, $"{i + 1:00}_{tests[i].Name}.png"), Columns, Rows);
        }

        System.Console.WriteLine($"Wrote {tests.Count + 1} PNG frames ({Columns}x{Rows} cells) to:");
        System.Console.WriteLine($"  {outputDir}");

        // Set TEDD_TUI_NO_OPEN=1 to suppress opening the folder (automation/CI).
        if (Environment.GetEnvironmentVariable("TEDD_TUI_NO_OPEN") is null or "" or "0")
            TryOpenFolder(outputDir);
    }

    private static void TryOpenFolder(string dir)
    {
        try
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ||
                RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                Process.Start(new ProcessStartInfo { FileName = dir, UseShellExecute = true });
            }
            else
            {
                Process.Start("xdg-open", dir);
            }
        }
        catch
        {
            // Viewing is a convenience; the paths were already printed.
        }
    }
}
