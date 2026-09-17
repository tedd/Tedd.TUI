using System;
using Xunit;
using Tedd.TUI;
using Tedd.TUI.Controls;

namespace Tedd.TUI.Tests;

public class MenuItemCoverageTests
{
    [Fact]
    public void MenuItem_OnMouseMove_FocusesWhenMenuSessionActive()
    {
        var window = new TuiWindow();
        var menuBar = new MenuBar();
        window.Content = menuBar;

        var fileMenu = new MenuItem { Header = new TextBlock { Text = "File" } };
        var editMenu = new MenuItem { Header = new TextBlock { Text = "Edit" } };
        menuBar.AddChild(fileMenu);
        menuBar.AddChild(editMenu);

        var openItem = new MenuItem { Header = new TextBlock { Text = "Open" } };
        fileMenu.Items.Add(openItem);

        window.Measure(new Size(80, 25));
        window.Arrange(new Rect(0, 0, 80, 25));

        fileMenu.OpenSubMenu();

        var mouseEvent = new MouseEventArgs(UIElement.MouseMoveEvent, editMenu);
        editMenu.OnMouseMove(mouseEvent);

        Assert.True(editMenu.IsFocused);
    }

    [Theory]
    [InlineData(ConsoleKey.Enter)]
    [InlineData(ConsoleKey.Spacebar)]
    public void MenuItem_OnKeyDown_EnterOrSpace_WithItems_TogglesExpansion(ConsoleKey key)
    {
        var window = new TuiWindow();
        var menuBar = new MenuBar();
        window.Content = menuBar;

        var fileMenu = new MenuItem { Header = new TextBlock { Text = "File" } };
        menuBar.AddChild(fileMenu);
        var openItem = new MenuItem { Header = new TextBlock { Text = "Open" } };
        fileMenu.Items.Add(openItem);

        window.Measure(new Size(80, 25));
        window.Arrange(new Rect(0, 0, 80, 25));

        window.SetFocus(fileMenu);

        Assert.False(fileMenu.IsExpanded);

        var keyEvent = new KeyEventArgs(UIElement.KeyDownEvent, fileMenu) { Key = key };
        fileMenu.OnKeyDown(keyEvent);

        Assert.True(fileMenu.IsExpanded);
        Assert.True(keyEvent.Handled);

        var keyEvent2 = new KeyEventArgs(UIElement.KeyDownEvent, fileMenu) { Key = key };
        fileMenu.OnKeyDown(keyEvent2);

        Assert.False(fileMenu.IsExpanded);
        Assert.True(keyEvent2.Handled);
    }

    [Theory]
    [InlineData(ConsoleKey.Enter)]
    [InlineData(ConsoleKey.Spacebar)]
    public void MenuItem_OnKeyDown_EnterOrSpace_WithoutItems_InvokesCommand(ConsoleKey key)
    {
        var window = new TuiWindow();
        var menuBar = new MenuBar();
        window.Content = menuBar;

        var fileMenu = new MenuItem { Header = new TextBlock { Text = "File" } };
        menuBar.AddChild(fileMenu);

        bool invoked = false;
        fileMenu.Command = () => invoked = true;

        window.Measure(new Size(80, 25));
        window.Arrange(new Rect(0, 0, 80, 25));

        window.SetFocus(fileMenu);

        var keyEvent = new KeyEventArgs(UIElement.KeyDownEvent, fileMenu) { Key = key };
        fileMenu.OnKeyDown(keyEvent);

        Assert.True(invoked);
        Assert.True(keyEvent.Handled);
    }

    [Fact]
    public void MenuItem_OnKeyDown_RightArrow_MenuBar_NavigatesNext()
    {
        var window = new TuiWindow();
        var menuBar = new MenuBar();
        window.Content = menuBar;

        var fileMenu = new MenuItem { Header = new TextBlock { Text = "File" } };
        var editMenu = new MenuItem { Header = new TextBlock { Text = "Edit" } };
        menuBar.AddChild(fileMenu);
        menuBar.AddChild(editMenu);

        window.Measure(new Size(80, 25));
        window.Arrange(new Rect(0, 0, 80, 25));

        window.SetFocus(fileMenu);

        var keyEvent = new KeyEventArgs(UIElement.KeyDownEvent, fileMenu) { Key = ConsoleKey.RightArrow };
        fileMenu.OnKeyDown(keyEvent);

        Assert.True(editMenu.IsFocused);
        Assert.True(keyEvent.Handled);
    }

    [Fact]
    public void MenuItem_OnKeyDown_LeftArrow_MenuBar_NavigatesPrev()
    {
        var window = new TuiWindow();
        var menuBar = new MenuBar();
        window.Content = menuBar;

        var fileMenu = new MenuItem { Header = new TextBlock { Text = "File" } };
        var editMenu = new MenuItem { Header = new TextBlock { Text = "Edit" } };
        menuBar.AddChild(fileMenu);
        menuBar.AddChild(editMenu);

        window.Measure(new Size(80, 25));
        window.Arrange(new Rect(0, 0, 80, 25));

        window.SetFocus(editMenu);

        var keyEvent = new KeyEventArgs(UIElement.KeyDownEvent, editMenu) { Key = ConsoleKey.LeftArrow };
        editMenu.OnKeyDown(keyEvent);

        Assert.True(fileMenu.IsFocused);
        Assert.True(keyEvent.Handled);
    }

    [Fact]
    public void MenuItem_OnKeyDown_RightArrow_WithItems_OpensSubMenu()
    {
        var window = new TuiWindow();
        var menuBar = new MenuBar();
        window.Content = menuBar;

        var fileMenu = new MenuItem { Header = new TextBlock { Text = "File" } };
        menuBar.AddChild(fileMenu);

        var openItem = new MenuItem { Header = new TextBlock { Text = "Open" } };
        var recentItem = new MenuItem { Header = new TextBlock { Text = "Recent" } };
        fileMenu.Items.Add(openItem);
        openItem.Items.Add(recentItem);

        window.Measure(new Size(80, 25));
        window.Arrange(new Rect(0, 0, 80, 25));

        window.SetFocus(fileMenu);
        fileMenu.OpenSubMenu();

        Assert.True(openItem.IsFocused);
        Assert.False(openItem.IsExpanded);

        var keyEvent = new KeyEventArgs(UIElement.KeyDownEvent, openItem) { Key = ConsoleKey.RightArrow };
        openItem.OnKeyDown(keyEvent);

        Assert.True(openItem.IsExpanded);
        Assert.True(recentItem.IsFocused);
        Assert.True(keyEvent.Handled);
    }

    [Fact]
    public void MenuItem_OnKeyDown_UpArrow_NotMenuBar_NavigatesPrevSibling()
    {
        var window = new TuiWindow();
        var menuBar = new MenuBar();
        window.Content = menuBar;

        var fileMenu = new MenuItem { Header = new TextBlock { Text = "File" } };
        menuBar.AddChild(fileMenu);

        var openItem = new MenuItem { Header = new TextBlock { Text = "Open" } };
        var saveItem = new MenuItem { Header = new TextBlock { Text = "Save" } };
        fileMenu.Items.Add(openItem);
        fileMenu.Items.Add(saveItem);

        window.Measure(new Size(80, 25));
        window.Arrange(new Rect(0, 0, 80, 25));

        window.SetFocus(fileMenu);
        fileMenu.OpenSubMenu();

        window.SetFocus(saveItem);

        var keyEvent = new KeyEventArgs(UIElement.KeyDownEvent, saveItem) { Key = ConsoleKey.UpArrow };
        saveItem.OnKeyDown(keyEvent);

        Assert.True(openItem.IsFocused);
        Assert.True(keyEvent.Handled);
    }

    [Fact]
    public void MenuItem_OnKeyDown_DownArrow_NotMenuBar_NavigatesNextSibling()
    {
        var window = new TuiWindow();
        var menuBar = new MenuBar();
        window.Content = menuBar;

        var fileMenu = new MenuItem { Header = new TextBlock { Text = "File" } };
        menuBar.AddChild(fileMenu);

        var openItem = new MenuItem { Header = new TextBlock { Text = "Open" } };
        var saveItem = new MenuItem { Header = new TextBlock { Text = "Save" } };
        fileMenu.Items.Add(openItem);
        fileMenu.Items.Add(saveItem);

        window.Measure(new Size(80, 25));
        window.Arrange(new Rect(0, 0, 80, 25));

        window.SetFocus(fileMenu);
        fileMenu.OpenSubMenu();

        Assert.True(openItem.IsFocused);

        var keyEvent = new KeyEventArgs(UIElement.KeyDownEvent, openItem) { Key = ConsoleKey.DownArrow };
        openItem.OnKeyDown(keyEvent);

        Assert.True(saveItem.IsFocused);
        Assert.True(keyEvent.Handled);
    }

    [Fact]
    public void MenuItem_OnKeyDown_LeftArrow_DeeperNesting_ClosesSubMenuAndNavigatesParentSibling()
    {
        var window = new TuiWindow();
        var menuBar = new MenuBar();
        window.Content = menuBar;

        var fileMenu = new MenuItem { Header = new TextBlock { Text = "File" } };
        var editMenu = new MenuItem { Header = new TextBlock { Text = "Edit" } };
        menuBar.AddChild(fileMenu);
        menuBar.AddChild(editMenu);

        var openItem = new MenuItem { Header = new TextBlock { Text = "Open" } };
        fileMenu.Items.Add(openItem);

        var recentItem = new MenuItem { Header = new TextBlock { Text = "Recent" } };
        openItem.Items.Add(recentItem);

        window.Measure(new Size(80, 25));
        window.Arrange(new Rect(0, 0, 80, 25));

        fileMenu.OpenSubMenu();
        openItem.OpenSubMenu();

        Assert.True(recentItem.IsFocused);

        var keyEvent = new KeyEventArgs(UIElement.KeyDownEvent, recentItem) { Key = ConsoleKey.LeftArrow };
        recentItem.OnKeyDown(keyEvent);

        Assert.False(openItem.IsExpanded);
        Assert.True(openItem.IsFocused);

        var keyEvent2 = new KeyEventArgs(UIElement.KeyDownEvent, openItem) { Key = ConsoleKey.LeftArrow };
        openItem.OnKeyDown(keyEvent2);

        Assert.False(fileMenu.IsExpanded);
        Assert.True(fileMenu.IsFocused);
    }

    [Fact]
    public void MenuItem_OnMouseDown_AlreadyHandled_ReturnsEarly()
    {
        var menuItem = new MenuItem();
        var args = new MouseEventArgs(UIElement.MouseDownEvent, menuItem) { Handled = true };
        menuItem.OnMouseDown(args);
        Assert.False(menuItem.IsFocused);
    }

    [Fact]
    public void MenuItem_OnMouseDown_WithItems_TogglesSubMenu()
    {
        var window = new TuiWindow();
        var menuBar = new MenuBar();
        window.Content = menuBar;
        var fileMenu = new MenuItem { Header = new TextBlock { Text = "File" } };
        menuBar.AddChild(fileMenu);
        var openItem = new MenuItem { Header = new TextBlock { Text = "Open" } };
        fileMenu.Items.Add(openItem);
        window.Measure(new Size(80, 25));
        window.Arrange(new Rect(0, 0, 80, 25));

        var args = new MouseEventArgs(UIElement.MouseDownEvent, fileMenu);
        fileMenu.OnMouseDown(args);

        Assert.True(fileMenu.IsExpanded);
        Assert.True(args.Handled);

        var args2 = new MouseEventArgs(UIElement.MouseDownEvent, fileMenu);
        fileMenu.OnMouseDown(args2);

        Assert.False(fileMenu.IsExpanded);
    }

    [Fact]
    public void MenuItem_OnMouseDown_WithoutItems_InvokesCommandAndClosesMenu()
    {
        var window = new TuiWindow();
        var menuBar = new MenuBar();
        window.Content = menuBar;
        var fileMenu = new MenuItem { Header = new TextBlock { Text = "File" } };
        menuBar.AddChild(fileMenu);

        bool invoked = false;
        fileMenu.Command = () => invoked = true;

        window.Measure(new Size(80, 25));
        window.Arrange(new Rect(0, 0, 80, 25));

        var args = new MouseEventArgs(UIElement.MouseDownEvent, fileMenu);
        fileMenu.OnMouseDown(args);

        Assert.True(invoked);
        Assert.True(args.Handled);
    }

// Test properties
    [Fact]
    public void MenuItem_Properties_CanSetAndGet()
    {
        var menuItem = new MenuItem();

        menuItem.HighlightBackground = TuiColor.Red;
        Assert.Equal(TuiColor.Red, menuItem.HighlightBackground);

        menuItem.HighlightForeground = TuiColor.Blue;
        Assert.Equal(TuiColor.Blue, menuItem.HighlightForeground);

        menuItem.PopupBackground = TuiColor.Green;
        Assert.Equal(TuiColor.Green, menuItem.PopupBackground);

        menuItem.PopupBorderColor = TuiColor.Yellow;
        Assert.Equal(TuiColor.Yellow, menuItem.PopupBorderColor);
    }

    [Fact]
    public void MenuItem_GetVisualChild_ThrowsWhenIndexOutOfRange()
    {
        var menuItem = new MenuItem();
        // VisualChildrenCount is 0 because Header is null
        Assert.Throws<ArgumentOutOfRangeException>(() => menuItem.GetVisualChild(0));

        menuItem.Header = new TextBlock { Text = "Test" };
        // VisualChildrenCount is 1
        Assert.Throws<ArgumentOutOfRangeException>(() => menuItem.GetVisualChild(1));
    }

    [Fact]
    public void MenuItem_MeasureOverride_WithoutHeader_ReturnsZeroSize()
    {
        var menuItem = new MenuItem();
        menuItem.Measure(new Size(100, 100));
        Assert.Equal(new Rect(0, 0, 0, 0), menuItem.RenderSize);
    }


    [Fact]
    public void MenuItem_IsMenuSessionActive_ReturnsFalseWhenNotExpandedAndNoSiblingExpanded()
    {
        var window = new TuiWindow();
        var menuBar = new MenuBar();
        window.Content = menuBar;

        var fileMenu = new MenuItem { Header = new TextBlock { Text = "File" } };
        var editMenu = new MenuItem { Header = new TextBlock { Text = "Edit" } };
        menuBar.AddChild(fileMenu);
        menuBar.AddChild(editMenu);

        window.Measure(new Size(80, 25));
        window.Arrange(new Rect(0, 0, 80, 25));

        // Send mouse move, since neither is expanded, IsMenuSessionActive returns false, doesn't focus
        var mouseEvent = new MouseEventArgs(UIElement.MouseMoveEvent, editMenu);
        editMenu.OnMouseMove(mouseEvent);

        Assert.False(editMenu.IsFocused);
    }

    [Fact]
    public void MenuItem_IsMenuSessionActive_ReturnsTrueWhenSiblingIsExpanded()
    {
        var window = new TuiWindow();
        var menuBar = new MenuBar();
        window.Content = menuBar;

        var fileMenu = new MenuItem { Header = new TextBlock { Text = "File" } };
        var editMenu = new MenuItem { Header = new TextBlock { Text = "Edit" } };
        menuBar.AddChild(fileMenu);
        menuBar.AddChild(editMenu);

        var openItem = new MenuItem { Header = new TextBlock { Text = "Open" } };
        fileMenu.Items.Add(openItem);

        window.Measure(new Size(80, 25));
        window.Arrange(new Rect(0, 0, 80, 25));

        // Setup active menu session
        fileMenu.OpenSubMenu();

        // Send mouse move to edit menu
        var mouseEvent = new MouseEventArgs(UIElement.MouseMoveEvent, editMenu);
        editMenu.OnMouseMove(mouseEvent);

        Assert.True(editMenu.IsFocused);
    }

    [Fact]
    public void MenuItem_OnKeyDown_UpArrow_MenuBar_DoesNothing()
    {
        var window = new TuiWindow();
        var menuBar = new MenuBar();
        window.Content = menuBar;
        var fileMenu = new MenuItem { Header = new TextBlock { Text = "File" } };
        menuBar.AddChild(fileMenu);
        window.Measure(new Size(80, 25));
        window.Arrange(new Rect(0, 0, 80, 25));

        var keyEvent = new KeyEventArgs(UIElement.KeyDownEvent, fileMenu) { Key = ConsoleKey.UpArrow };
        fileMenu.OnKeyDown(keyEvent);

        Assert.True(keyEvent.Handled);
    }

    [Fact]
    public void MenuItem_OnKeyDown_LeftArrow_DeeperNesting_WithoutGrandParent()
    {
        var window = new TuiWindow();
        var rootPanel = new StackPanel();
        window.Content = rootPanel;

        // Mock a ContextMenu-like scenario where a submenu is opened without a MenuBar
        var contextMenuItem = new MenuItem { Header = new TextBlock { Text = "Root" } };
        rootPanel.Children.Add(contextMenuItem);

        var subItem = new MenuItem { Header = new TextBlock { Text = "Sub" } };
        contextMenuItem.Items.Add(subItem);

        window.Measure(new Size(80, 25));
        window.Arrange(new Rect(0, 0, 80, 25));

        contextMenuItem.OpenSubMenu();

        // subItem is now in a popup, ParentMenuItem is contextMenuItem
        Assert.Equal(contextMenuItem, subItem.ParentMenuItem);

        var keyEvent = new KeyEventArgs(UIElement.KeyDownEvent, subItem) { Key = ConsoleKey.LeftArrow };
        subItem.OnKeyDown(keyEvent);

        Assert.True(contextMenuItem.IsFocused);
        Assert.False(contextMenuItem.IsExpanded);
    }

    [Fact]
    public void MenuItem_OpenSubMenu_DoesNotFocusIfNoFocusableItems()
    {
        var window = new TuiWindow();
        var menuBar = new MenuBar();
        window.Content = menuBar;

        var fileMenu = new MenuItem { Header = new TextBlock { Text = "File" } };
        menuBar.AddChild(fileMenu);

        var nonFocusableItem = new MenuItem { Header = new TextBlock { Text = "NonFocusable" } };
        nonFocusableItem.Focusable = false;
        fileMenu.Items.Add(nonFocusableItem);

        window.Measure(new Size(80, 25));
        window.Arrange(new Rect(0, 0, 80, 25));

        fileMenu.OpenSubMenu();

        Assert.True(fileMenu.IsExpanded);
        Assert.False(nonFocusableItem.IsFocused);
    }
}
