using Tedd.TUI;
using Tedd.TUI.HumanTests.Infrastructure;

namespace Tedd.TUI.HumanTests.Tests;

public class TextBoxTest : TestPage
{
    public override string Name => "TextBox";
    public override string Description => "Standard TextBox Control";

    protected override void AddScenarios()
    {
        // 1. Functionality Test
        var funcPanel = new StackPanel { Orientation = Orientation.Vertical };
        var tb = new TextBox { Text = "Initial Text", Width = 20 };
        var output = new TextBlock { Text = "Value: Initial Text" };

        // TextBox usually updates Text on input.
        // We can hook PropertyChanged if available or wait for LostFocus.
        // TUI doesn't have standard INotifyPropertyChanged for DependencyProperty changes except bindings?
        // But UIElement has OnPropertyChanged.
        // We don't have public event for TextChanged usually.
        // We'll add a "Show Value" button to verify content.

        var btnShow = new Button { Content = "Show Value" };
        btnShow.Click += (s, e) => output.Text = $"Value: {tb.Text}";

        funcPanel.AddChild(tb);
        funcPanel.AddChild(new TextBlock { Text = " " });
        funcPanel.AddChild(btnShow);
        funcPanel.AddChild(new TextBlock { Text = " " });
        funcPanel.AddChild(output);

        AddScenario("Functionality", funcPanel);

        // 2. Standard Layout Tests
        AddStandardScenarios(() => new TextBox { Text = "Test TextBox", Width = 15 });

        // 3. Password
        AddScenario("Password", new TextBox { Text = "Secret", IsPassword = true, Width = 15 });

        // 4. Multiline (if supported?)
        // Assuming not supported unless specialized or using Height > 1?
        // Let's test with height > 1
        AddScenario("Multiline Attempt", new TextBox { Text = "Line 1\nLine 2", Height = 5, Width = 20 });

        AddBindingScenario("Data Binding",
            new TextBox { Width = 20 },
            TextBox.TextProperty,
            nameof(TestViewModel.Text),
            vm => vm.Text = "Bound Text Updated!");
    }
}
