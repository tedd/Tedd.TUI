using System.Runtime.InteropServices;
using Tedd.TUI;
using Tedd.TUI.HumanTests.Infrastructure;
using Tedd.TUI.HumanTests.RenderTargets;
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
        var targets = BuildTargets(profile);
        var target = SelectTarget(targets, args, profile);
        LogRenderingChoice(target, profile);
        target.Run();
    }

    /// <summary>
    /// Every rendering target this executable can host on the current OS. Terminal
    /// targets run in this console; GUI targets open their own window.
    /// </summary>
    private static List<RenderTarget> BuildTargets(TerminalProfile profile)
    {
        var targets = new List<RenderTarget>
        {
            new()
            {
                Id = "auto",
                Label = "Auto terminal (PlatformLoader - best match for this host)",
                Run = () => TerminalTarget.Run(PlatformLoader.Load())
            },
            new()
            {
                Id = "legacy",
                Label = "Legacy terminal (16-color System.Console / ConsoleRenderer)",
                Run = () => TerminalTarget.Run(new LegacyConsolePlatform(profile))
            },
        };

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            targets.Add(new()
            {
                Id = "windowsterminal",
                Label = "WindowsTerminal (VT output / AnsiTrueColorRenderer)",
                Run = () => TerminalTarget.Run(new WindowsTerminalPlatform(profile))
            });
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ||
            RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            targets.Add(new()
            {
                Id = "linuxterminal",
                Label = "LinuxTerminal (unix truecolor + raw-mode input)",
                Run = () => TerminalTarget.Run(new LinuxTerminalPlatform(profile))
            });
        }

        targets.Add(new()
        {
            Id = "avalonia",
            Label = "Avalonia (desktop window, SkiaSharp cell surface)",
            Run = AvaloniaTarget.Run
        });

#if WINDOWS
        targets.Add(new()
        {
            Id = "wpf",
            Label = "WPF (desktop window, DrawingContext cell surface)",
            Run = WpfTarget.Run
        });
#endif

        targets.Add(new()
        {
            Id = "sdl2",
            Label = "SDL2 (native window, SkiaSharp cell surface)",
            Run = Sdl2Target.Run
        });

        targets.Add(new()
        {
            Id = "skia",
            Label = "Skia standalone (headless: PNG frame of every test page)",
            Run = SkiaScreenshotTarget.Run
        });

        targets.Add(new()
        {
            Id = "blazor",
            Label = "Blazor (browser: launches the Tedd.TUI.Demo.Blazor WASM dev server)",
            Run = BlazorTarget.Run
        });

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            targets.Add(new()
            {
                Id = "winui",
                Label = "WinUI (info: needs a Windows App SDK app; prints how to test)",
                Run = InfoTargets.WinUi
            });
        }

        targets.Add(new()
        {
            Id = "maui",
            Label = "MAUI (info: needs a MAUI app; prints how to test)",
            Run = InfoTargets.Maui
        });

        return targets;
    }

    /// <summary>
    /// Picks the target from <c>--target &lt;id&gt;</c> (or <c>--target=&lt;id&gt;</c>)
    /// when given, otherwise via a plain <see cref="System.Console"/> prompt before the
    /// selected backend takes over the screen.
    /// </summary>
    private static RenderTarget SelectTarget(List<RenderTarget> targets, string[] args, TerminalProfile profile)
    {
        var requested = ParseTargetArg(args);
        if (requested != null)
        {
            var match = targets.FirstOrDefault(t => t.Id.Equals(requested, StringComparison.OrdinalIgnoreCase));
            if (match != null)
                return match;

            System.Console.WriteLine($"Unknown --target '{requested}'. Available: {string.Join(", ", targets.Select(t => t.Id))}");
        }

        System.Console.WriteLine();
        System.Console.WriteLine("Tedd.TUI HumanTests - rendering target");
        System.Console.WriteLine("Host probe: " + DescribeProbeOneLine(profile));
        System.Console.WriteLine();
        for (var i = 0; i < targets.Count; i++)
            System.Console.WriteLine($"  {i + 1}) {targets[i].Label}");
        System.Console.WriteLine();
        System.Console.Write($"Enter 1-{targets.Count} [default: 1 = {targets[0].Id}]: ");

        var line = System.Console.ReadLine();
        var index = 0;
        if (!string.IsNullOrWhiteSpace(line) &&
            int.TryParse(line.Trim(), out var n) &&
            n >= 1 && n <= targets.Count)
        {
            index = n - 1;
        }

        System.Console.WriteLine();
        return targets[index];
    }

    private static string? ParseTargetArg(string[] args)
    {
        for (var i = 0; i < args.Length; i++)
        {
            if (args[i].Equals("--target", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
                return args[i + 1];
            if (args[i].StartsWith("--target=", StringComparison.OrdinalIgnoreCase))
                return args[i]["--target=".Length..];
        }
        return null;
    }

    private static void LogRenderingChoice(RenderTarget target, TerminalProfile profile)
    {
        var termDisp = profile.RawTerm ?? "(null)";
        var colorDisp = profile.RawColorTerm ?? "(null)";
        var message =
            $"Selected={target.Id}; " +
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
