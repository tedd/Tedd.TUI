using Tedd.TUI;

namespace Tedd.TUI.HumanTests.Infrastructure;

public abstract class TestPage
{
    public abstract string Name { get; }
    public abstract string Description { get; }

    protected TabControl _tabs;

    public virtual UIElement BuildPage()
    {
        var root = new StackPanel { Orientation = Orientation.Vertical };

        // Header
        root.AddChild(new Border
        {
            BoxStyle = BoxStyle.Single,
            Child = new TextBlock { Text = $"{Name} - {Description}", Foreground = ConsoleColor.Cyan }
        });

        _tabs = new TabControl { Height = 20 }; // Default height, maybe make it auto or configurable?
        // TabControl height determines the content area height.
        // If we want it to fit the window, we might need to handle layout better.
        // For now, fixed height is safer for TUI.

        AddScenarios();

        root.AddChild(_tabs);
        return root;
    }

    protected abstract void AddScenarios();

    protected void AddScenario(string header, UIElement content)
    {
        _tabs.Items.Add(new TabItem { Header = header, Content = content });
    }

    // Helper to create standard scenarios
    protected void AddStandardScenarios(Func<UIElement> controlFactory)
    {
        // 1. Default
        AddScenario("Default", controlFactory());

        // 2. Constrained (Small Box)
        var constrained = new Border
        {
            Width = 10,
            Height = 3,
            BoxStyle = BoxStyle.Single,
            Child = controlFactory()
        };
        AddScenario("Constrained", constrained);

        // 3. ScrollViewer
        var scroll = new ScrollViewer
        {
            Width = 20,
            Height = 5,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Visible,
            VerticalScrollBarVisibility = ScrollBarVisibility.Visible
        };
        // Put a large stack inside scroll
        var largeContent = new StackPanel { Orientation = Orientation.Vertical };
        largeContent.AddChild(new TextBlock { Text = "Scroll Down..." });
        largeContent.AddChild(controlFactory());
        largeContent.AddChild(new TextBlock { Text = "End of Content" });
        scroll.Content = largeContent;
        AddScenario("Scroll", scroll);

        // 4. Surrounded
        var surrounded = new StackPanel { Orientation = Orientation.Vertical };
        surrounded.AddChild(new TextBlock { Text = "Above" });
        var row = new StackPanel { Orientation = Orientation.Horizontal };
        row.AddChild(new TextBlock { Text = "Left " });
        row.AddChild(controlFactory());
        row.AddChild(new TextBlock { Text = " Right" });
        surrounded.AddChild(row);
        surrounded.AddChild(new TextBlock { Text = "Below" });
        AddScenario("Surrounded", surrounded);
    }

    protected void AddBindingScenario(string title, UIElement control, DependencyProperty dp, string propertyName, Action<TestViewModel> updateAction)
    {
        var vm = new TestViewModel();
        control.DataContext = vm;
        control.SetBinding(dp, new Binding(propertyName));

        var panel = new StackPanel { Orientation = Orientation.Vertical };
        panel.AddChild(control);

        var btnUpdate = new Button { Content = "Update Data (ViewModel)" };
        btnUpdate.Click += (s, e) => updateAction(vm);

        panel.AddChild(new TextBlock { Text = " " });
        panel.AddChild(btnUpdate);

        AddScenario(title, panel);
    }
}
