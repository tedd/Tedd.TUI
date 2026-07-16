using Xunit;
using Tedd.TUI;
using Tedd.TUI.Tests.TestInfrastructure;

namespace Tedd.TUI.Tests;

public class MenuBarInteractionTests
{
    [Fact]
    public void TestMenuBarHitTest()
    {
        // Arrange
        var window = new TuiWindow();
        var menuBar = new MenuBar();
        window.Content = menuBar;

        var fileMenu = new MenuItem { Header = new TextBlock { Text = "File" } };
        menuBar.AddChild(fileMenu);

        // Measure and Arrange
        // Assume window size 80x25
        window.Measure(new Size(80, 25));
        window.Arrange(new Rect(0, 0, 80, 25));

        // Act
        // Click on "F" of File.
        // MenuBar is at 0,0.
        // File menu should be at 0,0 relative to MenuBar?
        // Let's check coordinates.
        // MenuBar (StackPanel Horizontal)
        // FileMenu (MenuItem) -> Measure -> Header (TextBlock "File") -> 4 chars.
        // Arrange -> 0,0, 4, 1.

        // So (0,0) (1,0) (2,0) (3,0) should hit FileMenu.
        var hit = window.InputHitTest(0, 0);

        // Assert
        Assert.NotNull(hit);
        // InputHitTest returns the leaf node (TextBlock), so we verify it's the child of our menu item
        Assert.Equal(fileMenu, hit.Element.Parent);
    }

    [Fact]
    public void MouseClick_NestedMenuBar_InvokesOnlyChosenPopupItem()
    {
        var openCount = 0;
        var cutCount = 0;
        var openItem = new MenuItem
        {
            Header = new TextBlock { Text = "Open" },
            Command = () => openCount++
        };
        var cutItem = new MenuItem
        {
            Header = new TextBlock { Text = "Cut" },
            Command = () => cutCount++
        };
        var fileMenu = new MenuItem { Header = new TextBlock { Text = "File" } };
        fileMenu.Items.Add(openItem);
        var editMenu = new MenuItem { Header = new TextBlock { Text = "Edit" } };
        editMenu.Items.Add(cutItem);
        var spacer = new TextBlock { Text = "   " };
        var menuBar = new MenuBar();
        menuBar.AddChild(fileMenu);
        menuBar.AddChild(spacer);
        menuBar.AddChild(editMenu);

        var panel = new StackPanel();
        panel.AddChild(new TextBlock { Text = "application" });
        panel.AddChild(menuBar);
        panel.AddChild(new TextBlock { Text = "workspace surface" });
        var host = new ControlTestHost(new Border { Child = panel }, 28, 8);

        host.Click(spacer, 1, 0);
        Assert.Equal(0, openCount);
        Assert.Equal(0, cutCount);

        var fileClick = host.Click(fileMenu, 1, 0);

        Assert.True(fileClick.Down.Handled);
        Assert.True(fileMenu.IsExpanded);
        Assert.False(editMenu.IsExpanded);

        var openClick = host.Click(openItem, 1, 0);

        Assert.True(openClick.Down.Handled);
        Assert.Equal(1, openCount);
        Assert.Equal(0, cutCount);
        Assert.False(fileMenu.IsExpanded);

        host.Click(editMenu, 1, 0);
        Assert.False(fileMenu.IsExpanded);
        Assert.True(editMenu.IsExpanded);

        host.Click(cutItem, 1, 0);
        Assert.Equal(1, openCount);
        Assert.Equal(1, cutCount);
        Assert.False(editMenu.IsExpanded);
    }
}
