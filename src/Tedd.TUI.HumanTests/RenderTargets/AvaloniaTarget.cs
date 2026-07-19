using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Themes.Fluent;
using Tedd.TUI.HumanTests.Infrastructure;
using Tedd.TUI.Platform.Avalonia;

// Inside the Tedd.TUI.* namespace the TUI types win name lookups; alias the
// colliding Avalonia types explicitly.
using AvApplication = Avalonia.Application;
using AvWindow = Avalonia.Controls.Window;

namespace Tedd.TUI.HumanTests.RenderTargets;

/// <summary>
/// Runs the test session in an Avalonia desktop window hosting
/// <see cref="TuiHostControl"/> (SkiaSharp cell surface, works on Windows/macOS/Linux).
/// </summary>
public static class AvaloniaTarget
{
    public static void Run()
    {
        AppBuilder.Configure<HostApp>()
            .UsePlatformDetect()
            .StartWithClassicDesktopLifetime(Array.Empty<string>());
    }

    private sealed class HostApp : AvApplication
    {
        public override void Initialize()
        {
            // The host window is a templated control; without a theme it has no template
            // and presents nothing.
            Styles.Add(new FluentTheme());
        }

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.MainWindow = new AvWindow
                {
                    Title = "Tedd.TUI HumanTests - Avalonia host",
                    Width = 1100,
                    Height = 750,
                    Content = new TuiHostControl { Window = TestSession.CreateWindow() }
                };
            }

            base.OnFrameworkInitializationCompleted();
        }
    }
}
