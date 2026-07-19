using Tedd.TUI.HumanTests.Infrastructure;
using Tedd.TUI.Platform.Console;

namespace Tedd.TUI.HumanTests.RenderTargets;

/// <summary>
/// Runs the test session in the current terminal on a concrete
/// <see cref="ITuiPlatform"/> (legacy console, Windows Terminal or Linux terminal).
/// </summary>
public static class TerminalTarget
{
    public static void Run(ITuiPlatform platform)
    {
        var window = TestSession.CreateWindow();
        var app = new TuiApp(window, platform);

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
}
