using Xunit;
using Tedd.TUI;
using System.Collections.Generic;

namespace Tedd.TUI.Tests;

public class MenuBarTests
{
    [Fact]
    public void TestMenuBarSwitching()
    {
        // Arrange
        var window = new TuiWindow();
        var menuBar = new MenuBar();
        window.Content = menuBar;

        var fileMenu = new MenuItem { Header = new TextBlock { Text = "File" } };
        fileMenu.Items.Add(new MenuItem { Header = new TextBlock { Text = "Open" } });
        
        var editMenu = new MenuItem { Header = new TextBlock { Text = "Edit" } };
        editMenu.Items.Add(new MenuItem { Header = new TextBlock { Text = "Cut" } });

        menuBar.AddChild(fileMenu);
        menuBar.AddChild(editMenu);

        // Ensure everything is parented correctly
        // Windows/MenuBar usually Setup parenting on AddChild
        
        // Act
        // 1. Open File Menu
        fileMenu.OpenSubMenu();

        // Assert 1
        Assert.True(fileMenu.IsExpanded, "File menu should be expanded");
        Assert.False(editMenu.IsExpanded, "Edit menu should not be expanded");

        // Act 2. Open Edit Menu (Simulating click on second menu item)
        // In the real UI, clicking Edit would trigger OnMouseDown -> OpenSubMenu
        editMenu.OpenSubMenu();

        Assert.True(editMenu.IsExpanded, "Edit menu should be expanded");
        Assert.False(fileMenu.IsExpanded, "File menu should be closed when Edit menu is opened");

        // Visual Verification
        // Setup buffer and render
        menuBar.Measure(new Size(20, 1));
        menuBar.Arrange(new Rect(0, 0, 20, 1));
        var buffer = new VirtualBuffer(20, 1);
        
        // Render
        window.Render(buffer, 0, 0);

        // Check colors
        // File menu (0,0) should be Gray (Inactive) because it is closed.
        // Edit menu (depends on length of "File") should be Green (Active).
        
        // "File" length is 4. "Edit" starts at 4?
        // Let's check text positions.
        // fileMenu header text is "File".
        
        var pixel0 = buffer.GetPixel(0, 0); // F of File
        Assert.Equal('F', pixel0.Character);
        
        // If the BUG exists visually, this might be Green.
        // It SHOULD be Gray (default MenuBar background) or null if transparent (but it inherits Gray effectively)
        // MenuBar draws Gray background. TextBlock draws transparency on top.
        // So actual buffer color should be Gray.
        
        // Assert.Equal(ConsoleColor.Gray, pixel0.Background);
        if (pixel0.Background != ConsoleColor.Gray)
        {
             throw new System.Exception($"Expected Gray but got {pixel0.Background}");
        }

        // Edit menu should be Green
        // "File" is 4 chars. So Edit starts at 4.
        var pixel4 = buffer.GetPixel(4, 0); // E of Edit
        Assert.Equal('E', pixel4.Character);
        Assert.Equal(ConsoleColor.Green, pixel4.Background);
    }
}
