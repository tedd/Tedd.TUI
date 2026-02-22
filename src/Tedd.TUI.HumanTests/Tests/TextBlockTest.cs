using Tedd.TUI;
using Tedd.TUI.HumanTests.Infrastructure;

namespace Tedd.TUI.HumanTests.Tests;

public class TextBlockTest : TestPage
{
    public override string Name => "TextBlock";
    public override string Description => "Standard TextBlock Control";

    protected override void AddScenarios()
    {
        // 1. Functionality Test
        var funcPanel = new StackPanel { Orientation = Orientation.Vertical };
        var tb = new TextBlock { Text = "Initial Text" };
        var btn = new Button { Content = "Change Text" };
        btn.Click += (s, e) => tb.Text = $"Changed at {DateTime.Now:mm:ss}";

        funcPanel.AddChild(tb);
        funcPanel.AddChild(new TextBlock { Text = " " });
        funcPanel.AddChild(btn);

        AddScenario("Functionality", funcPanel);

        // 2. Standard Layout Tests
        AddStandardScenarios(() => new TextBlock { Text = "Test TextBlock" });

        // 3. Wrapping
        var wrapPanel = new StackPanel { Orientation = Orientation.Vertical };
        wrapPanel.AddChild(new Border
        {
            Width = 20,
            BoxStyle = BoxStyle.Single,
            Child = new TextBlock
            {
                Text = "This is a very long text that should wrap inside this small box hopefully if TextBlock supports wrapping.",
                // TextWrapping is not standard property in TUI usually unless supported.
                // Checking TextBlock.cs might be useful.
                // Assuming it might just clip or wrap. Let's see.
            }
        });

        AddScenario("Wrapping Test", wrapPanel);

        // 4. Foreground/Background
        var colors = new StackPanel { Orientation = Orientation.Vertical };
        colors.AddChild(new TextBlock { Text = "Red on Blue", Foreground = ConsoleColor.Red, Background = ConsoleColor.Blue });
        colors.AddChild(new TextBlock { Text = "Green on Black", Foreground = ConsoleColor.Green, Background = ConsoleColor.Black });

        AddScenario("Colors", colors);
    }
}
