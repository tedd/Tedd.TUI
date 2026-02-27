using System;
using System.Collections.Generic;
using Tedd.TUI;
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
}
