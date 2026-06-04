using System;
using Xunit;
using Tedd.TUI;

namespace Tedd.TUI.Tests;

public class FocusOverlayTests
{
    [Fact]
    public void TabNavigation_ShouldOnlyRunOnKeyDown()
    {
        // Arrange
        var window = new TuiWindow();
        var panel = new StackPanel { Orientation = Orientation.Horizontal };
        var btn1 = new Button { Content = "Button 1" };
        var btn2 = new Button { Content = "Button 2" };
        panel.AddChild(btn1);
        panel.AddChild(btn2);
        window.Content = panel;

        window.Measure(new Size(80, 25));
        window.Arrange(new Rect(0, 0, 80, 25));

        window.SetFocus(btn1);
        Assert.Equal(btn1, GetFocusedElement(window));

        // Act & Assert 1: Tab KeyUp should not move focus
        var keyUpArgs = new KeyEventArgs(UIElement.KeyUpEvent, btn1)
        {
            Key = ConsoleKey.Tab
        };
        window.ProcessKey(keyUpArgs);
        Assert.Equal(btn1, GetFocusedElement(window)); // Focus should remain on btn1

        // Act & Assert 2: Tab KeyDown should move focus
        var keyDownArgs = new KeyEventArgs(UIElement.KeyDownEvent, btn1)
        {
            Key = ConsoleKey.Tab
        };
        window.ProcessKey(keyDownArgs);
        Assert.Equal(btn2, GetFocusedElement(window)); // Focus should move to btn2
    }

    [Fact]
    public void Menu_ShouldClose_OnClickOutside()
    {
        // Arrange
        var window = new TuiWindow();
        var mainPanel = new StackPanel { Orientation = Orientation.Vertical };
        var menuBar = new MenuBar();
        var fileMenu = new MenuItem { Header = new TextBlock { Text = "File" } };
        fileMenu.Items.Add(new MenuItem { Header = new TextBlock { Text = "New" } });
        menuBar.AddChild(fileMenu);
        mainPanel.AddChild(menuBar);

        var btn = new Button { Content = "Button" };
        mainPanel.AddChild(btn);
        window.Content = mainPanel;

        window.Measure(new Size(80, 25));
        window.Arrange(new Rect(0, 0, 80, 25));

        // Open menu
        window.SetFocus(fileMenu);
        fileMenu.OpenSubMenu();
        Assert.NotNull(window.Overlay); // Submenu popup should be open as an overlay

        // Act: Click on the button outside the menu
        var previewArgs = new MouseEventArgs(UIElement.PreviewMouseDownEvent, btn)
        {
            GlobalX = 10,
            GlobalY = 10 // Safe coordinates for click outside the menu popup
        };
        window.RaiseEvent(previewArgs);

        // Assert
        Assert.Null(window.Overlay); // Submenu popup overlay should be closed
    }

    [Fact]
    public void Menu_ShouldClose_OnFocusLost()
    {
        // Arrange
        var window = new TuiWindow();
        var mainPanel = new StackPanel { Orientation = Orientation.Vertical };
        var menuBar = new MenuBar();
        var fileMenu = new MenuItem { Header = new TextBlock { Text = "File" } };
        fileMenu.Items.Add(new MenuItem { Header = new TextBlock { Text = "New" } });
        menuBar.AddChild(fileMenu);
        mainPanel.AddChild(menuBar);

        var btn = new Button { Content = "Button" };
        mainPanel.AddChild(btn);
        window.Content = mainPanel;

        window.Measure(new Size(80, 25));
        window.Arrange(new Rect(0, 0, 80, 25));

        // Open menu
        window.SetFocus(fileMenu);
        fileMenu.OpenSubMenu();
        Assert.NotNull(window.Overlay);

        // Act: Move focus to the button outside the menu (simulating Tab navigation or programmatic focus change)
        window.SetFocus(btn);

        // Assert
        Assert.Null(window.Overlay); // Submenu popup overlay should be closed
    }

    [Fact]
    public void ComboBox_ShouldClose_OnClickOutside()
    {
        // Arrange
        var window = new TuiWindow();
        var mainPanel = new StackPanel { Orientation = Orientation.Vertical };
        var comboBox = new ComboBox();
        comboBox.Items.Add("Item 1");
        comboBox.Items.Add("Item 2");
        mainPanel.AddChild(comboBox);

        var btn = new Button { Content = "Button" };
        mainPanel.AddChild(btn);
        window.Content = mainPanel;

        window.Measure(new Size(80, 25));
        window.Arrange(new Rect(0, 0, 80, 25));

        // Open ComboBox dropdown
        window.SetFocus(comboBox);
        var mouseDownArgs = new MouseEventArgs(UIElement.MouseDownEvent, comboBox) { X = 0, Y = 0 };
        comboBox.OnMouseDown(mouseDownArgs);
        Assert.NotNull(window.Overlay); // Dropdown popup should be open as an overlay

        // Act: Click on the button outside the ComboBox dropdown
        var previewArgs = new MouseEventArgs(UIElement.PreviewMouseDownEvent, btn)
        {
            GlobalX = 10,
            GlobalY = 10
        };
        window.RaiseEvent(previewArgs);

        // Assert
        Assert.Null(window.Overlay); // Dropdown popup should be closed
    }

    [Fact]
    public void ComboBox_ShouldClose_OnFocusLost()
    {
        // Arrange
        var window = new TuiWindow();
        var mainPanel = new StackPanel { Orientation = Orientation.Vertical };
        var comboBox = new ComboBox();
        comboBox.Items.Add("Item 1");
        comboBox.Items.Add("Item 2");
        mainPanel.AddChild(comboBox);

        var btn = new Button { Content = "Button" };
        mainPanel.AddChild(btn);
        window.Content = mainPanel;

        window.Measure(new Size(80, 25));
        window.Arrange(new Rect(0, 0, 80, 25));

        // Open ComboBox dropdown
        window.SetFocus(comboBox);
        var mouseDownArgs = new MouseEventArgs(UIElement.MouseDownEvent, comboBox) { X = 0, Y = 0 };
        comboBox.OnMouseDown(mouseDownArgs);
        Assert.NotNull(window.Overlay);

        // Act: Set focus to the button outside the ComboBox
        window.SetFocus(btn);

        // Assert
        Assert.Null(window.Overlay); // Dropdown popup should be closed
    }

    private UIElement GetFocusedElement(TuiWindow window)
    {
        // Access private _focusedElement using reflection for verification
        var field = typeof(TuiWindow).GetField("_focusedElement", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        return (UIElement)field.GetValue(window);
    }
}
