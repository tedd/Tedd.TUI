using System;
using System.Collections.Generic;
using Tedd.TUI;
using Tedd.TUI.Tests.TestInfrastructure;
using Xunit;

namespace Tedd.TUI.Tests;

public class TabControlTests
{
    [Fact]
    public void TabControl_Initialization_Defaults()
    {
        var tabControl = new TabControl();
        Assert.NotNull(tabControl.Items);
        Assert.Empty(tabControl.Items);
        Assert.Equal(-1, tabControl.SelectedIndex);
        Assert.Null(tabControl.SelectedItem);
    }

    [Fact]
    public void TabControl_AddTabItem_UpdatesSelection()
    {
        var tabControl = new TabControl();
        var tab1 = new TabItem { Header = "Tab 1", Content = new TextBlock { Text = "Content 1" } };

        tabControl.Items.Add(tab1);

        // First item should be auto-selected
        Assert.Equal(0, tabControl.SelectedIndex);
        Assert.Equal(tab1, tabControl.SelectedItem);
        Assert.True(tab1.IsSelected);
    }

    [Fact]
    public void TabControl_SelectionChange_UpdatesIsSelected()
    {
        var tabControl = new TabControl();
        var tab1 = new TabItem { Header = "Tab 1" };
        var tab2 = new TabItem { Header = "Tab 2" };

        tabControl.Items.Add(tab1);
        tabControl.Items.Add(tab2);

        // Default: 0 selected
        Assert.True(tab1.IsSelected);
        Assert.False(tab2.IsSelected);

        // Change selection
        tabControl.SelectedIndex = 1;

        Assert.False(tab1.IsSelected);
        Assert.True(tab2.IsSelected);
        Assert.Equal(tab2, tabControl.SelectedItem);
    }

    [Fact]
    public void TabControl_Content_ParentPropagation()
    {
        var tabControl = new TabControl();
        var content = new TextBlock { Text = "Content" };
        var tab1 = new TabItem { Header = "Tab 1", Content = content };

        tabControl.Items.Add(tab1);

        // Verify Logical Tree
        Assert.Equal(tabControl, tab1.Parent);
        Assert.Equal(tab1, content.Parent);
    }

    [Fact]
    public void TabControl_DataContext_Inheritance()
    {
        var tabControl = new TabControl();
        var content = new TextBlock();
        var tab1 = new TabItem { Header = "Tab 1", Content = content };
        tabControl.Items.Add(tab1);

        var dataContext = new object();
        tabControl.DataContext = dataContext;

        // Verify DataContext flows down
        Assert.Equal(dataContext, tab1.DataContext);
        Assert.Equal(dataContext, content.DataContext);
    }

    [Fact]
    public void TabItem_HeaderedContentControl_Properties()
    {
        var tab = new TabItem();
        tab.Header = "My Header";
        Assert.Equal("My Header", tab.Header);
    }

    [Theory]
    [InlineData(ConsoleKey.RightArrow, 0, 1)] // Go right
    [InlineData(ConsoleKey.RightArrow, 1, 2)] // Go right
    [InlineData(ConsoleKey.RightArrow, 2, 0)] // Wrap right
    [InlineData(ConsoleKey.LeftArrow, 1, 0)] // Go left
    [InlineData(ConsoleKey.LeftArrow, 0, 2)] // Wrap left
    [InlineData(ConsoleKey.Enter, 0, 0)] // Ignored
    public void TabControl_OnKeyDown_NavigatesTabs(ConsoleKey key, int initialIndex, int expectedIndex)
    {
        var tabControl = new TabControl();
        tabControl.Items.Add(new TabItem { Header = "Tab 1" });
        tabControl.Items.Add(new TabItem { Header = "Tab 2" });
        tabControl.Items.Add(new TabItem { Header = "Tab 3" });

        tabControl.SelectedIndex = initialIndex;

        tabControl.OnKeyDown(new KeyEventArgs { Key = key });

        Assert.Equal(expectedIndex, tabControl.SelectedIndex);
    }

    [Fact]
    public void TabControl_OnKeyDown_NoItems_IgnoresKey()
    {
        var tabControl = new TabControl();
        tabControl.OnKeyDown(new KeyEventArgs { Key = ConsoleKey.RightArrow });
        Assert.Equal(-1, tabControl.SelectedIndex);
    }

    [Fact]
    public void MouseClick_NestedTabControl_SwitchesOnlyClickedHeader()
    {
        var tabControl = new TabControl { Width = 20, Height = 6 };
        tabControl.Items.Add(new TabItem { Header = "T1" }); // " T1 " length = 4
        tabControl.Items.Add(new TabItem { Header = "Tab 2" }); // " Tab 2 " length = 7
        tabControl.Items.Add(new TabItem { Header = "T3" }); // " T3 " length = 4
        tabControl.SelectedIndex = 0;
        var sibling = new Button { Content = "Other", Width = 8 };
        var row = new StackPanel { Orientation = Orientation.Horizontal };
        row.AddChild(tabControl);
        row.AddChild(new TextBlock { Text = "  " });
        row.AddChild(sibling);
        var surface = new StackPanel();
        surface.AddChild(new TextBlock { Text = "Tabs" });
        surface.AddChild(row);
        surface.AddChild(new TextBlock { Text = "status" });
        var host = new ControlTestHost(new Border { Child = surface }, 32, 10);

        // Click on T1 (X: 0 to 3)
        host.Click(tabControl, 2, 0);
        Assert.Equal(0, tabControl.SelectedIndex);

        // Click on Tab 2 (X: 5 to 11) -> 4 + 1(gap) = 5
        host.Click(tabControl, 6, 0);
        Assert.Equal(1, tabControl.SelectedIndex);

        // Click on T3 (X: 13 to 16) -> 5 + 7 + 1 = 13
        host.Click(tabControl, 14, 0);
        Assert.Equal(2, tabControl.SelectedIndex);

        // Click the header gap, content separator, surrounding surface, and sibling.
        host.Click(tabControl, 4, 0);
        host.Click(tabControl, 2, 1);
        host.Click(30, 8);
        host.Click(sibling, 2, 1);
        Assert.Equal(2, tabControl.SelectedIndex);
        Assert.True(sibling.IsFocused);
    }

    [Fact]
    public void TabControl_Render_DrawsHeadersAndContent()
    {
        var tabControl = new TabControl();
        tabControl.Width = 20;
        tabControl.Height = 10;
        var content = new TextBlock { Text = "Content", Width = 10, Height = 5 };
        var tab1 = new TabItem { Header = "Tab 1", Content = content };
        tabControl.Items.Add(tab1);

        // Force layout
        tabControl.Measure(new Size(20, 10));
        tabControl.Arrange(new Rect(0, 0, 20, 10));

        var buffer = new VirtualBuffer(20, 10);
        tabControl.Render(buffer, 0, 0);

        // Header format is " Tab 1 "
        Assert.Equal('T', buffer.GetPixel(1, 0).Character);
        Assert.Equal('a', buffer.GetPixel(2, 0).Character);
        Assert.Equal('b', buffer.GetPixel(3, 0).Character);

        // Assert line separator
        Assert.Equal(BoxDrawingChars.Get(BoxStyle.Single).Horizontal, buffer.GetPixel(0, 1).Character);

        // Assert content is drawn (starts at y=2)
        Assert.Equal('C', buffer.GetPixel(0, 2).Character);
    }

    [Fact]
    public void TabControl_ClickChild_KeepsChildFocus()
    {
        // Arrange
        var window = new TuiWindow();
        var tabControl = new TabControl();
        var textBox = new TextBox { Text = "Hello" };
        var tab1 = new TabItem { Header = "Tab 1", Content = textBox };
        tabControl.Items.Add(tab1);
        window.Content = tabControl;

        window.Measure(new Size(80, 25));
        window.Arrange(new Rect(0, 0, 80, 25));

        // 1. Initially focus textBox
        window.SetFocus(textBox);
        var focused = typeof(TuiWindow)
            .GetField("_focusedElement", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.GetValue(window) as UIElement;
        Assert.Equal(textBox, focused);

        // 2. Simulate mouse down event that hits the TextBox (at y >= 2 in TabControl coordinates)
        var mouseDownArgs = new MouseEventArgs(UIElement.MouseDownEvent, textBox)
        {
            GlobalX = 5,
            GlobalY = 5
        };
        textBox.RaiseEvent(mouseDownArgs);

        // 3. Verify focus was not stolen by TabControl
        focused = typeof(TuiWindow)
            .GetField("_focusedElement", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.GetValue(window) as UIElement;
        Assert.Equal(textBox, focused);
    }
}
