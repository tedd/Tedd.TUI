using Tedd.TUI.HumanTests.Screens;

namespace Tedd.TUI.HumanTests.Infrastructure;

public static class TestSession
{
    /// <summary>
    /// Builds the shared test UI: a <see cref="TuiWindow"/> wired to a
    /// <see cref="TestRunner"/> showing the component selection screen. Every rendering
    /// target hosts the window returned here, so the test flow is identical on all of them.
    /// </summary>
    public static TuiWindow CreateWindow()
    {
        var window = new TuiWindow();
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
        ShowSelection();
        return window;
    }
}
