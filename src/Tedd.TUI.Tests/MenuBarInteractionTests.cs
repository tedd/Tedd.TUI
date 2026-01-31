using Xunit;
using Tedd.TUI;

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
}
