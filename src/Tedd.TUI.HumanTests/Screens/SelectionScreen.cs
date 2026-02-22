using Tedd.TUI;
using Tedd.TUI.HumanTests.Infrastructure;

namespace Tedd.TUI.HumanTests.Screens;

public class SelectionScreen : StackPanel
{
    private readonly TestRunner _runner;
    private readonly List<TestPage> _allTests;
    private readonly List<CheckBox> _checkBoxes = new();
    private readonly Dictionary<CheckBox, TestPage> _testMap = new();

    public SelectionScreen(TestRunner runner)
    {
        _runner = runner;
        _allTests = TestDiscovery.GetAllTests();

        Orientation = Orientation.Vertical;

        // Title
        AddChild(new Border
        {
            BoxStyle = BoxStyle.Double,
            Child = new TextBlock { Text = " Tedd.TUI Component Tests ", Foreground = ConsoleColor.Yellow }
        });

        AddChild(new TextBlock { Text = "Select components to test:", Foreground = ConsoleColor.Gray });
        AddChild(new TextBlock { Text = " " });

        // ScrollViewer for list
        var scrollViewer = new ScrollViewer
        {
            Height = 15,
            VerticalScrollBarVisibility = true
        };

        var itemsPanel = new StackPanel { Orientation = Orientation.Vertical };

        foreach (var test in _allTests)
        {
            var cb = new CheckBox
            {
                Content = $"{test.Name} - {test.Description}",
                IsChecked = false
            };
            _testMap[cb] = test;
            _checkBoxes.Add(cb);
            itemsPanel.AddChild(cb);
        }

        scrollViewer.Content = itemsPanel;
        AddChild(scrollViewer);

        AddChild(new TextBlock { Text = " " });

        // Buttons
        var btnPanel = new StackPanel { Orientation = Orientation.Horizontal };

        var btnAll = new Button { Content = "Select All" };
        btnAll.Click += (s, e) => _checkBoxes.ForEach(c => c.IsChecked = true);

        var btnNone = new Button { Content = "Select None" };
        btnNone.Click += (s, e) => _checkBoxes.ForEach(c => c.IsChecked = false);

        var btnStart = new Button { Content = " START TESTS ", Background = ConsoleColor.DarkGreen };
        btnStart.Click += (s, e) => StartSelected();

        btnPanel.AddChild(btnAll);
        btnPanel.AddChild(new TextBlock { Text = "  " });
        btnPanel.AddChild(btnNone);
        btnPanel.AddChild(new TextBlock { Text = "    " });
        btnPanel.AddChild(btnStart);

        AddChild(btnPanel);
    }

    private void StartSelected()
    {
        var selected = _checkBoxes
            .Where(c => c.IsChecked)
            .Select(c => _testMap[c])
            .ToList();

        if (selected.Count == 0)
        {
            return;
        }

        _runner.StartTests(selected);
    }
}
