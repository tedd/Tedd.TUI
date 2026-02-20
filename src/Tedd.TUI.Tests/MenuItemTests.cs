using System;
using Xunit;
using Tedd.TUI;
using System.Linq;

namespace Tedd.TUI.Tests;

public class MenuItemTests
{
    [Fact]
    public void TestNestedMenuBackNavigation()
    {
        // Setup Window and MenuBar
        var window = new TuiWindow();
        window.Measure(new Size(80, 25));
        window.Arrange(new Rect(0, 0, 80, 25));

        var menuBar = new MenuBar();
        window.Content = menuBar;

        // Level 0 Item (File)
        var fileMenu = new MenuItem { Header = new TextBlock { Text = "File" } };
        menuBar.AddChild(fileMenu);

        // Level 1 Items (Open, Exit)
        var openItem = new MenuItem { Header = new TextBlock { Text = "Open" } };
        var exitItem = new MenuItem { Header = new TextBlock { Text = "Exit" } };
        fileMenu.Items.Add(openItem);
        fileMenu.Items.Add(exitItem);

        // Level 2 Item (Recent) inside Open
        var recentItem = new MenuItem { Header = new TextBlock { Text = "Recent" } };
        openItem.Items.Add(recentItem);

        // Initial State
        Assert.False(fileMenu.IsExpanded);
        Assert.False(openItem.IsExpanded);
        Assert.Null(window.Overlay);

        // 1. Open "File" menu (Level 1)
        // Simulate focusing fileMenu first?
        window.SetFocus(fileMenu);
        fileMenu.OpenSubMenu();

        Assert.True(fileMenu.IsExpanded);
        Assert.NotNull(window.Overlay);
        Assert.Equal(fileMenu, openItem.ParentMenuItem); // Verify ParentMenuItem set

        // OpenSubMenu logic attempts to focus first item.
        // openItem is the first item.
        Assert.True(openItem.IsFocused);

        // 2. Open "Open" submenu (Level 2)
        // openItem is focused. Simulate Enter or call OpenSubMenu.
        openItem.OpenSubMenu();

        Assert.True(openItem.IsExpanded);
        Assert.NotNull(window.Overlay);
        // Verify ParentMenuItem set for Level 2 item
        Assert.Equal(openItem, recentItem.ParentMenuItem);

        // Verify "Recent" item is focused (it's the only item in Level 2)
        Assert.True(recentItem.IsFocused);

        // 3. Simulate Left Arrow on "Recent" item (Level 2 -> Level 1)
        var keyEvent = new KeyEventArgs { Key = ConsoleKey.LeftArrow };
        recentItem.OnKeyDown(keyEvent);

        // Assertions after Left Arrow
        // Level 2 should be closed
        Assert.False(openItem.IsExpanded);

        // Level 1 should be visible (Overlay is not null)
        Assert.NotNull(window.Overlay);

        // Level 1 owner (File Menu) should be Expanded (it was re-opened)
        Assert.True(fileMenu.IsExpanded);

        // Focus should be back on "Open" item (Level 1 item)
        Assert.True(openItem.IsFocused);
    }
}
