using Xunit;
using Tedd.TUI;
using System.Collections.Generic;
using System;

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

        // FIX: Ensure layout is calculated BEFORE opening submenus.
        // OpenSubMenu relies on RenderSize to determine popup coordinates.
        window.Measure(new Size(20, 1));
        window.Arrange(new Rect(0, 0, 20, 1));

        // Act 1. Open File Menu
        fileMenu.OpenSubMenu();

        // Assert 1
        Assert.True(fileMenu.IsExpanded, "File menu should be expanded");
        Assert.False(editMenu.IsExpanded, "Edit menu should not be expanded");

        // Act 2. Open Edit Menu (Simulating click on second menu item)
        // In the real UI, clicking Edit would trigger OnMouseDown -> OpenSubMenu
        editMenu.OpenSubMenu();

        // Assert 2
        Assert.True(editMenu.IsExpanded, "Edit menu should be expanded");
        Assert.False(fileMenu.IsExpanded, "File menu should be closed when Edit menu is opened");

        // Visual Verification
        var buffer = new VirtualBuffer(20, 5);
        window.Render(buffer, 0, 0);
        
        // File at (0,0)
        Assert.Equal('F', buffer.GetPixel(0, 0).Character);
        // Edit at (4,0)
        Assert.Equal('E', buffer.GetPixel(4, 0).Character);

        // Popup for Edit should be at (4, 1)
        Assert.Equal('┌', buffer.GetPixel(4, 1).Character);
    }

    [Fact]
    public void TestNestedSubMenuPositioning()
    {
        // Arrange
        var window = new TuiWindow();
        var menuBar = new MenuBar();
        window.Content = menuBar;

        var fileMenu = new MenuItem { Header = new TextBlock { Text = "File" } };
        var openMenu = new MenuItem { Header = new TextBlock { Text = "Open" } };
        openMenu.Items.Add(new MenuItem { Header = new TextBlock { Text = "Project" } });
        fileMenu.Items.Add(openMenu);
        menuBar.AddChild(fileMenu);

        // Layout
        window.Measure(new Size(80, 25));
        window.Arrange(new Rect(0, 0, 80, 25));

        // Act
        fileMenu.OpenSubMenu();
        openMenu.OpenSubMenu();

        // Assert
        Assert.True(openMenu.IsExpanded, "Nested menu should be expanded");

        var buffer = new VirtualBuffer(80, 25);
        window.Render(buffer, 0, 0);

        // File is at (0,0). File popup starts at (0,1).
        // File popup has a border. Open is inside.
        // StackPanel inside Border is at (1, 1) relative to Border.
        // So Open item is at (1, 2) relative to window.
        // Width of Open is 4.
        // Nested popup should be at X = absX + Width = 1 + 4 = 5.
        // Y = absY = 2.
        
        Assert.Equal('┌', buffer.GetPixel(5, 2).Character);
    }

    [Fact]
    public void TestMenuBarKeyboardNavigation()
    {
        // Arrange
        var window = new TuiWindow();
        var menuBar = new MenuBar();
        window.Content = menuBar;

        var fileMenu = new MenuItem { Header = new TextBlock { Text = "File" } };
        fileMenu.Items.Add(new MenuItem { Header = new TextBlock { Text = "Open" } });
        menuBar.AddChild(fileMenu);

        window.Measure(new Size(80, 25));
        window.Arrange(new Rect(0, 0, 80, 25));
        
        fileMenu.Focus();

        // Act
        window.ProcessKey(new KeyEventArgs { Key = System.ConsoleKey.DownArrow, Modifiers = System.ConsoleModifiers.None });

        // Assert
        Assert.True(fileMenu.IsExpanded, "Down arrow on MenuBar item should open submenu");
    }
}
