using System.IO;
using Tedd.TUI;

namespace Tedd.TUI.HumanTests.Infrastructure;

public class TestRunner
{
    private readonly TuiWindow _window;
    private readonly List<TestPage> _selectedTests = new();
    private int _currentTestIndex = -1;

    public TestRunner(TuiWindow window)
    {
        _window = window;
    }

    public void StartTests(IEnumerable<TestPage> tests)
    {
        _selectedTests.Clear();
        _selectedTests.AddRange(tests);
        _currentTestIndex = 0;

        // Clear any residual overlays
        _window.ClearOverlay();

        RunCurrentTest();
    }

    private void RunCurrentTest()
    {
        // Clear any overlays from previous test
        _window.ClearOverlay();

        if (_currentTestIndex >= 0 && _currentTestIndex < _selectedTests.Count)
        {
            var test = _selectedTests[_currentTestIndex];
            var page = test.BuildPage();

            // Wrap the page with Pass/Fail controls
            var wrapper = new StackPanel { Orientation = Orientation.Vertical };
            wrapper.AddChild(page);

            // Spacer
            wrapper.AddChild(new TextBlock { Text = " " });

            // Test Controls
            var controls = new StackPanel { Orientation = Orientation.Horizontal };

            var btnPass = new Button { Content = " PASS ", Background = ConsoleColor.DarkGreen };
            btnPass.Click += (s, e) => RecordResult(TestStatus.Passed);

            var btnFail = new Button { Content = " FAIL ", Background = ConsoleColor.DarkRed };
            btnFail.Click += (s, e) => PromptFailureReason();

            controls.AddChild(btnPass);
            controls.AddChild(new TextBlock { Text = "  " });
            controls.AddChild(btnFail);

            wrapper.AddChild(controls);

            _window.Content = wrapper;
        }
        else
        {
            // Finish
            ShowSummary();
        }
    }

    private void RecordResult(TestStatus status, string message = "")
    {
        var test = _selectedTests[_currentTestIndex];
        Logger.Log(new TestResult
        {
            ComponentName = test.Name,
            Status = status,
            Message = message,
            Timestamp = DateTime.Now
        });

        _currentTestIndex++;
        RunCurrentTest();
    }

    private void PromptFailureReason()
    {
        // Simple Dialog for input
        var dialog = new DialogBox
        {
            Title = "Failure Reason",
            Width = 50,
            Height = 10,
            BoxStyle = BoxStyle.Double
        };

        var content = new StackPanel { Orientation = Orientation.Vertical };
        var input = new TextBox { Width = 40, Text = "" };

        var btnSubmit = new Button { Content = "Submit" };
        btnSubmit.Click += (s, e) =>
        {
            dialog.Hide(); // Sets Visibility=Hidden
            _window.RemoveOverlay(dialog); // Remove from stack
            RecordResult(TestStatus.Failed, input.Text);
        };

        var btnCancel = new Button { Content = "Cancel" };
        btnCancel.Click += (s, e) =>
        {
            dialog.Hide();
            _window.RemoveOverlay(dialog);
        };

        content.AddChild(new TextBlock { Text = "Please describe the failure:" });
        content.AddChild(input);
        content.AddChild(new TextBlock { Text = " " });

        var btnPanel = new StackPanel { Orientation = Orientation.Horizontal };
        btnPanel.AddChild(btnSubmit);
        btnPanel.AddChild(new TextBlock { Text = " " });
        btnPanel.AddChild(btnCancel);
        content.AddChild(btnPanel);

        dialog.Content = content;

        _window.PushOverlay(dialog);
        dialog.Show();
    }

    public Action? OnComplete { get; set; }

    private void ShowSummary()
    {
        var summary = new StackPanel { Orientation = Orientation.Vertical };
        summary.AddChild(new TextBlock { Text = "All Tests Completed!", Foreground = ConsoleColor.Green });
        summary.AddChild(new TextBlock { Text = $"Check {Path.GetFullPath("test_results.log")} for details." });

        var btnMenu = new Button { Content = "Back to Menu" };
        btnMenu.Click += (s, e) => OnComplete?.Invoke();

        var btnExit = new Button { Content = "Exit" };
        btnExit.Click += (s, e) => Environment.Exit(0);

        var btnPanel = new StackPanel { Orientation = Orientation.Horizontal };
        btnPanel.AddChild(btnMenu);
        btnPanel.AddChild(new TextBlock { Text = "  " });
        btnPanel.AddChild(btnExit);

        summary.AddChild(new TextBlock { Text = " " });
        summary.AddChild(btnPanel);

        _window.Content = summary;
    }
}
