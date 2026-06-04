using System.Runtime.InteropServices;
using Tedd.TUI;
using Tedd.TUI.HumanTests.Infrastructure;
using Tedd.TUI.HumanTests.Screens;
using Tedd.TUI.Platform.Console;
using Tedd.TUI.Platform.LinuxTerminal;
using Tedd.TUI.Platform.WindowsTerminal;

namespace Tedd.TUI.HumanTests;

class Program
{
    static void Main(string[] args)
    {
        Logger.Clear();

        var profile = TerminalProbe.Detect();
        var platform = SelectRenderingPlatformInteractively(profile);
        LogRenderingChoice(platform, profile);

        var window = new TuiWindow();
        var app = new TuiApp(window, platform);

        var runner = new TestRunner(window);

        void ShowSelection()
        {
            // Clear any overlays
            window.ClearOverlay();

            var selection = new SelectionScreen(runner);
            window.Content = selection;
            window.SetFocus(selection); // Ensure focus is somewhere valid
            window.EnsureInitialFocus();
        }

        runner.OnComplete = ShowSelection;

        // Initial Screen
        ShowSelection();

        System.Console.CancelKeyPress += (s, e) =>
        {
            app.Stop();
            e.Cancel = true;
        };

        try
        {
            app.Run();
        }
        finally
        {
            app.Stop();
        }
    }

    private static void LogRenderingChoice(ITuiPlatform platform, TerminalProfile profile)
    {
        var backend = platform switch
        {
            LegacyConsolePlatform => "Legacy",
            WindowsTerminalPlatform => "WindowsTerminal",
            LinuxTerminalPlatform => "LinuxTerminal",
            _ => platform.GetType().FullName ?? platform.GetType().Name,
        };

        var termDisp = profile.RawTerm ?? "(null)";
        var colorDisp = profile.RawColorTerm ?? "(null)";
        var message =
            $"Selected={backend}; " +
            $"probe: IsWindowsTerminal={profile.IsWindowsTerminal}, IsLegacyWindowsConsole={profile.IsLegacyWindowsConsole}, " +
            $"IsUnixTerminal={profile.IsUnixTerminal}, SupportsTrueColor={profile.SupportsTrueColor}, " +
            $"TERM={termDisp}, COLORTERM={colorDisp}";

        Logger.Log(new TestResult
        {
            ComponentName = "RenderingBackend",
            Status = TestStatus.Info,
            Message = message,
            Timestamp = DateTime.Now,
        });
    }

    /// <summary>
    /// Plain <see cref="System.Console"/> prompt before the TUI takes over the screen.
    /// Lists every backend this executable can host so manual runs can exercise each path.
    /// </summary>
    private static ITuiPlatform SelectRenderingPlatformInteractively(TerminalProfile profile)
    {
        var choices = new List<(string Label, Func<ITuiPlatform> Factory)>
        {
            ("Auto (PlatformLoader - best match for this host)", () => PlatformLoader.Load()),
            ("Legacy (16-color System.Console / ConsoleRenderer)", () => new LegacyConsolePlatform(profile)),
        };

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            choices.Add(("WindowsTerminal (VT output / AnsiTrueColorRenderer)", () => new WindowsTerminalPlatform(profile)));
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ||
            RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            choices.Add(("LinuxTerminal (unix truecolor + raw-mode input)", () => new LinuxTerminalPlatform(profile)));
        }

        System.Console.WriteLine();
        System.Console.WriteLine("Tedd.TUI HumanTests - rendering backend");
        System.Console.WriteLine("Host probe: " + DescribeProbeOneLine(profile));
        System.Console.WriteLine();
        for (var i = 0; i < choices.Count; i++)
            System.Console.WriteLine($"  {i + 1}) {choices[i].Label}");
        System.Console.WriteLine();
        System.Console.Write($"Enter 1-{choices.Count} [default: 1 = Auto]: ");

        var line = System.Console.ReadLine();
        var index = 0;
        if (!string.IsNullOrWhiteSpace(line) &&
            int.TryParse(line.Trim(), out var n) &&
            n >= 1 && n <= choices.Count)
        {
            index = n - 1;
        }

        System.Console.WriteLine();
        return choices[index].Factory();
    }

    private static string DescribeProbeOneLine(TerminalProfile p)
    {
        var os = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "Windows"
            : RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? "macOS"
            : RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ? "Linux"
            : RuntimeInformation.OSDescription;
        var term = p.RawTerm ?? "?";
        return $"{os}; WT={p.IsWindowsTerminal}; legacyConhost={p.IsLegacyWindowsConsole}; unix={p.IsUnixTerminal}; " +
               $"trueColor={p.SupportsTrueColor}; TERM={term}";
    }
}
