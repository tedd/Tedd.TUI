#if WINDOWS
using System.Threading;
using Tedd.TUI.HumanTests.Infrastructure;
using Tedd.TUI.Platform.Wpf;

// Inside the Tedd.TUI.* namespace the TUI types win name lookups; alias the
// colliding WPF types explicitly.
using WpfApplication = System.Windows.Application;
using WpfWindow = System.Windows.Window;

namespace Tedd.TUI.HumanTests.RenderTargets;

/// <summary>
/// Runs the test session in a WPF window hosting <see cref="TuiHostElement"/>
/// (DrawingContext cell surface, Windows only).
/// </summary>
public static class WpfTarget
{
    public static void Run()
    {
        // WPF needs an STA dispatcher thread; the console Main thread is MTA, so the
        // application runs on a dedicated thread and Run blocks until the window closes.
        var thread = new Thread(() =>
        {
            var host = new TuiHostElement { Window = TestSession.CreateWindow() };
            var window = new WpfWindow
            {
                Title = "Tedd.TUI HumanTests - WPF host",
                Width = 1100,
                Height = 750,
                Content = host
            };
            window.Loaded += (_, _) => host.Focus();

            var app = new WpfApplication { ShutdownMode = System.Windows.ShutdownMode.OnMainWindowClose };
            app.Run(window);
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();
    }
}
#endif
