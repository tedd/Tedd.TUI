using Tedd.TUI;
using Tedd.TUI.Platform.Console;
using Tedd.TUI.HumanTests.Infrastructure;
using Tedd.TUI.HumanTests.Screens;

namespace Tedd.TUI.HumanTests;

class Program
{
    static void Main(string[] args)
    {
        // Clean log
        Logger.Clear();

        var window = new TuiWindow();
        var app = new TuiApp(window);

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
}
