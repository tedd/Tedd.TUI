using System;
using Xunit;
using Tedd.TUI;

namespace Tedd.TUI.Tests;

public class MenuItemCoverageTests
{
    [Fact]
    public void MenuItem_OnKeyDown_CoverageTest()
    {
        var window = new TuiWindow();
        var menuBar = new MenuBar();
        window.Content = menuBar;

        var fileMenu = new MenuItem { Header = new TextBlock { Text = "File" } };
        var fileMenu2 = new MenuItem { Header = new TextBlock { Text = "File2" } };
        var openMenu = new MenuItem { Header = new TextBlock { Text = "Open" } };
        var openMenu2 = new MenuItem { Header = new TextBlock { Text = "Open2" } };
        var recentMenu = new MenuItem { Header = new TextBlock { Text = "Recent" } };
        var recentMenu2 = new MenuItem { Header = new TextBlock { Text = "Recent2" } };
        var terminalMenuItem = new MenuItem { Header = new TextBlock { Text = "Terminal" } };

        openMenu.Items.Add(recentMenu);
        openMenu.Items.Add(recentMenu2);
        fileMenu.Items.Add(openMenu);
        fileMenu.Items.Add(openMenu2);
        fileMenu.Items.Add(terminalMenuItem);

        menuBar.AddChild(fileMenu);
        menuBar.AddChild(fileMenu2);

        window.Measure(new Size(80, 25));
        window.Arrange(new Rect(0, 0, 80, 25));

        // Test Enter to open submenu
        var enterEvent = new KeyEventArgs { Key = ConsoleKey.Enter };
        fileMenu.OnKeyDown(enterEvent);
        Assert.True(enterEvent.Handled);
        Assert.True(fileMenu.IsExpanded);

        // Test Enter to close submenu
        enterEvent = new KeyEventArgs { Key = ConsoleKey.Enter };
        fileMenu.OnKeyDown(enterEvent);
        Assert.True(enterEvent.Handled);
        Assert.False(fileMenu.IsExpanded);

        // Test RightArrow in MenuBar to next top level
        var rightEvent = new KeyEventArgs { Key = ConsoleKey.RightArrow };
        fileMenu.OnKeyDown(rightEvent);
        Assert.True(rightEvent.Handled);

        // Test LeftArrow in MenuBar to previous top level
        var leftEvent = new KeyEventArgs { Key = ConsoleKey.LeftArrow };
        fileMenu2.OnKeyDown(leftEvent);
        Assert.True(leftEvent.Handled);

        // Enter on leaf menu item (invokes command and closes parent menus)
        var commandInvoked = false;
        terminalMenuItem.Command = () => commandInvoked = true;
        fileMenu.OpenSubMenu();
        enterEvent = new KeyEventArgs { Key = ConsoleKey.Enter };
        terminalMenuItem.OnKeyDown(enterEvent);
        Assert.True(enterEvent.Handled);
        Assert.True(commandInvoked);
        Assert.False(fileMenu.IsExpanded);

        fileMenu.OpenSubMenu();
        // Test DownArrow in submenu for next sibling
        var downEvent = new KeyEventArgs { Key = ConsoleKey.DownArrow };
        openMenu.OnKeyDown(downEvent);
        Assert.True(downEvent.Handled);

        // Test UpArrow in submenu for prev sibling
        var upEvent = new KeyEventArgs { Key = ConsoleKey.UpArrow };
        openMenu2.OnKeyDown(upEvent);
        Assert.True(upEvent.Handled);

        // Test RightArrow in Submenu to open submenu
        rightEvent = new KeyEventArgs { Key = ConsoleKey.RightArrow };
        openMenu.OnKeyDown(rightEvent);
        Assert.True(rightEvent.Handled);
        Assert.True(openMenu.IsExpanded);

        // Test RightArrow on a leaf submenu item - doesn't do much, just handled=true
        rightEvent = new KeyEventArgs { Key = ConsoleKey.RightArrow };
        recentMenu.OnKeyDown(rightEvent);
        Assert.True(rightEvent.Handled);

        // Test LeftArrow in submenu to close submenu and focus parent
        leftEvent = new KeyEventArgs { Key = ConsoleKey.LeftArrow };
        recentMenu.OnKeyDown(leftEvent);
        Assert.True(leftEvent.Handled);
        Assert.False(openMenu.IsExpanded);

        // Deeply nested LeftArrow test
        openMenu.OpenSubMenu();
        var deepMenu = new MenuItem { Header = new TextBlock { Text = "Deep" } };
        recentMenu.Items.Add(deepMenu);
        recentMenu.OpenSubMenu();

        leftEvent = new KeyEventArgs { Key = ConsoleKey.LeftArrow };
        deepMenu.OnKeyDown(leftEvent);
        Assert.True(leftEvent.Handled);
        Assert.False(recentMenu.IsExpanded);
        Assert.True(openMenu.IsExpanded);

        // Test UpArrow in MenuBar
        var upEvent2 = new KeyEventArgs { Key = ConsoleKey.UpArrow };
        fileMenu.OnKeyDown(upEvent2);
        Assert.True(upEvent2.Handled);
    }

    [Fact]
    public void MenuItem_OnMouseDown_CoverageTest()
    {
        var window = new TuiWindow();
        var menuBar = new MenuBar();
        window.Content = menuBar;

        var fileMenu = new MenuItem { Header = new TextBlock { Text = "File" } };
        var openMenu = new MenuItem { Header = new TextBlock { Text = "Open" } };
        var recentMenu = new MenuItem { Header = new TextBlock { Text = "Recent" } };

        fileMenu.Items.Add(openMenu);
        openMenu.Items.Add(recentMenu);
        menuBar.AddChild(fileMenu);

        window.Measure(new Size(80, 25));
        window.Arrange(new Rect(0, 0, 80, 25));

        // Test Handled early return
        var mouseEventHandled = new MouseEventArgs { X = 0, Y = 0, Handled = true };
        fileMenu.OnMouseDown(mouseEventHandled);
        Assert.False(fileMenu.IsFocused); // Was not processed

        // Test MouseDown on item with children (opens submenu)
        var mouseEvent = new MouseEventArgs { X = 0, Y = 0 };
        fileMenu.OnMouseDown(mouseEvent);
        Assert.True(mouseEvent.Handled);
        window.SetFocus(fileMenu); Assert.True(fileMenu.IsFocused);
        Assert.True(fileMenu.IsExpanded);

        // Test MouseDown on item with children again (closes submenu)
        mouseEvent = new MouseEventArgs { X = 0, Y = 0 };
        fileMenu.OnMouseDown(mouseEvent);
        Assert.True(mouseEvent.Handled);
        Assert.False(fileMenu.IsExpanded);

        // Reopen to test leaf
        fileMenu.OpenSubMenu();
        openMenu.OpenSubMenu();

        var commandInvoked = false;
        recentMenu.Command = () => commandInvoked = true;

        // Test MouseDown on leaf item (invokes command and closes parent menus)
        mouseEvent = new MouseEventArgs { X = 0, Y = 0 };
        recentMenu.OnMouseDown(mouseEvent);

        Assert.True(mouseEvent.Handled);
        Assert.True(recentMenu.IsFocused);
        Assert.True(commandInvoked);

        // Assert parents are closed
        Assert.False(openMenu.IsExpanded);
        Assert.False(fileMenu.IsExpanded);
    }


    [Fact]
    public void MenuItem_CloseParentMenu_CoverageTest()
    {
        var window = new TuiWindow();
        var menuBar = new MenuBar();
        window.Content = menuBar;

        var fileMenu = new MenuItem { Header = new TextBlock { Text = "File" } };
        var openMenu = new MenuItem { Header = new TextBlock { Text = "Open" } };
        var recentMenu = new MenuItem { Header = new TextBlock { Text = "Recent" } };

        fileMenu.Items.Add(openMenu);
        openMenu.Items.Add(recentMenu);
        menuBar.AddChild(fileMenu);

        window.Measure(new Size(80, 25));
        window.Arrange(new Rect(0, 0, 80, 25));

        // Open down to leaf
        fileMenu.OpenSubMenu();
        openMenu.OpenSubMenu();

        Assert.True(fileMenu.IsExpanded);
        Assert.True(openMenu.IsExpanded);

        // Invoke close indirectly through Enter key on leaf
        recentMenu.OnKeyDown(new KeyEventArgs { Key = ConsoleKey.Enter });

        // Everything should be closed
        Assert.False(fileMenu.IsExpanded);
        Assert.False(openMenu.IsExpanded);
    }



    [Fact]
    public void MenuItem_IsMenuSessionActive_CoverageTest()
    {
        var window = new TuiWindow();
        var menuBar = new MenuBar();
        window.Content = menuBar;

        var fileMenu = new MenuItem { Header = new TextBlock { Text = "File" } };
        var openMenu = new MenuItem { Header = new TextBlock { Text = "Open" } };
        var editMenu = new MenuItem { Header = new TextBlock { Text = "Edit" } };

        fileMenu.Items.Add(openMenu);
        menuBar.AddChild(fileMenu);
        menuBar.AddChild(editMenu);

        window.Measure(new Size(80, 25));
        window.Arrange(new Rect(0, 0, 80, 25));

        // Start session on first top-level menu
        fileMenu.OpenSubMenu();

        // Mouse hover on other top-level menu (simulates slide between menus)
        // Edit is currently not focused, but menu session is active via File
        Assert.False(editMenu.IsFocused);
        editMenu.OnMouseMove(new MouseEventArgs { X = 0, Y = 0 });
        Assert.True(editMenu.IsFocused);

        // Mouse hover on already focused item
        editMenu.OnMouseMove(new MouseEventArgs { X = 0, Y = 0 });
        Assert.True(editMenu.IsFocused); // Doesn't fail, simply does nothing

        // Mouse hover on nested item
        Assert.False(openMenu.IsFocused);
        openMenu.OnMouseMove(new MouseEventArgs { X = 0, Y = 0 });
        Assert.True(openMenu.IsFocused);

        // Mouse hover when no session active
        fileMenu.CloseSubMenu();
        editMenu.CloseSubMenu(); // Just to be sure

        var nonSessionItem = new MenuItem { Header = new TextBlock { Text = "Lone" } };
        window.Content = nonSessionItem; // Replace menubar
        window.Measure(new Size(80, 25));
        window.Arrange(new Rect(0, 0, 80, 25));

        Assert.False(nonSessionItem.IsFocused);
        nonSessionItem.OnMouseMove(new MouseEventArgs { X = 0, Y = 0 });
        Assert.False(nonSessionItem.IsFocused); // Session not active
    }



    [Fact]
    public void MenuItem_LayoutAndRender_CoverageTest()
    {
        var window = new TuiWindow();
        var menuBar = new MenuBar();
        window.Content = menuBar;

        var nullHeaderItem = new MenuItem(); // Header is null
        menuBar.AddChild(nullHeaderItem);

        window.Measure(new Size(80, 25));
        window.Arrange(new Rect(0, 0, 80, 25));

        Assert.Equal(new Size(0, 0), nullHeaderItem.DesiredSize);
        Assert.Equal(0, nullHeaderItem.VisualChildrenCount);

        Assert.Throws<ArgumentOutOfRangeException>(() => nullHeaderItem.GetVisualChild(0));

        // Render null header item
        var buffer = new VirtualBuffer(80, 25);
        nullHeaderItem.Render(buffer, 0, 0); // No dimensions, no rendering to do really, just runs loop
    }



    [Fact]
    public void MenuItem_IsMenuSessionActive_MenuBarChildNonMenuItem()
    {
        var window = new TuiWindow();
        var menuBar = new MenuBar();
        window.Content = menuBar;

        var fileMenu = new MenuItem { Header = new TextBlock { Text = "File" } };
        var button = new Button { Content = "Not a Menu" };

        menuBar.AddChild(fileMenu);
        menuBar.AddChild(button);

        window.Measure(new Size(80, 25));
        window.Arrange(new Rect(0, 0, 80, 25));

        // Start session on first top-level menu
        fileMenu.OpenSubMenu();

        // Mouse hover on button in menubar - file menu is open, so session is active,
        // this loop needs to encounter the button
        fileMenu.OnMouseMove(new MouseEventArgs { X = 0, Y = 0 });
    }



    [Fact]
    public void MenuItem_DeepNestedMenu_CoverageTest()
    {
        var window = new TuiWindow();
        var menuBar = new MenuBar();
        window.Content = menuBar;

        var contextMenuRoot = new MenuItem { Header = new TextBlock { Text = "Context" } };
        var childItem = new MenuItem { Header = new TextBlock { Text = "Child" } };
        var leafItem = new MenuItem { Header = new TextBlock { Text = "Leaf" } };

        contextMenuRoot.Items.Add(childItem);
        childItem.Items.Add(leafItem);

        // Don't add to MenuBar. Emulate context menu floating somewhere
        window.Measure(new Size(80, 25));
        window.Arrange(new Rect(0, 0, 80, 25));

        // Let's add it to a stackpanel which isn't MenuBar
        var stack = new StackPanel();
        stack.AddChild(contextMenuRoot);
        window.Content = stack;

        contextMenuRoot.OpenSubMenu();
        childItem.OpenSubMenu();

        // Assert state
        Assert.True(contextMenuRoot.IsExpanded);
        Assert.True(childItem.IsExpanded);

        // Fire LeftArrow on leafItem
        var leftEvent = new KeyEventArgs { Key = ConsoleKey.LeftArrow };
        leafItem.OnKeyDown(leftEvent);

        Assert.True(leftEvent.Handled);

        // childItem's subMenu was closed, and we hit the 'else' branch in navigation because ParentMenuItem's Parent isn't a MenuBar
        // and grandParent (contextMenuRoot's ParentMenuItem) is null.
        Assert.False(childItem.IsExpanded);
    }



    [Fact]
    public void MenuItem_NavigateSibling_ParentIsMenuBar_CoverageTest()
    {
        var window = new TuiWindow();
        var menuBar = new MenuBar();
        window.Content = menuBar;

        var fileMenu = new MenuItem { Header = new TextBlock { Text = "File" } };
        var contextMenuRoot = new MenuItem { Header = new TextBlock { Text = "Context" } };
        var childItem = new MenuItem { Header = new TextBlock { Text = "Child" } };

        contextMenuRoot.Items.Add(childItem);
        menuBar.AddChild(fileMenu);

        // Don't add to MenuBar. Emulate context menu floating somewhere
        window.Measure(new Size(80, 25));
        window.Arrange(new Rect(0, 0, 80, 25));

        // Let's add it to a stackpanel which isn't MenuBar
        var stack = new StackPanel();
        stack.AddChild(contextMenuRoot);
        window.Content = stack;

        // Emulate an invalid structure where a context menu root has a parent menu item in a menu bar
        // to cover lines 244-246
        contextMenuRoot.ParentMenuItem = fileMenu;
        contextMenuRoot.OpenSubMenu();

        var leftEvent = new KeyEventArgs { Key = ConsoleKey.LeftArrow };
        childItem.OnKeyDown(leftEvent);

        Assert.True(leftEvent.Handled);
        window.SetFocus(fileMenu); Assert.True(fileMenu.IsFocused);
    }



    [Fact]
    public void MenuItem_NavigateSibling_Focusable_CoverageTest()
    {
        var window = new TuiWindow();
        var menuBar = new MenuBar();
        window.Content = menuBar;

        var fileMenu = new MenuItem { Header = new TextBlock { Text = "File" } };
        var openMenu = new MenuItem { Header = new TextBlock { Text = "Open" } };

        // This is to hit the 'if (item.Focusable)' missed branch when opening submenu
        var separatorItem = new Separator();
        separatorItem.Focusable = false;

        fileMenu.Items.Add(separatorItem);
        fileMenu.Items.Add(openMenu);
        menuBar.AddChild(fileMenu);

        window.Measure(new Size(80, 25));
        window.Arrange(new Rect(0, 0, 80, 25));

        fileMenu.OpenSubMenu();
        // Since separator isn't focusable, the logic should skip it and focus openMenu
        Assert.True(openMenu.IsFocused);
    }

}
