using System;
using Xunit;
using Tedd.TUI;
using System.Collections.Generic;

namespace Tedd.TUI.Tests;

public class ListBoxTests
{
    [Fact]
    public void SelectionChange_ShouldInvalidate()
    {
        var window = new TuiWindow();
        var listBox = new ListBox();
        window.Content = listBox;
        listBox.Items.Add("Item 1");
        listBox.Items.Add("Item 2");

        bool invalidated = false;
        window.VisualChanged += (s, e) => invalidated = true;

        // Action
        listBox.SelectedIndex = 1;

        // Assert
        Assert.True(invalidated, "Changing SelectedIndex should trigger Invalidate via VisualChanged on Window");
    }

    [Fact]
    public void KeyDown_Arrow_ShouldInvalidate()
    {
        var window = new TuiWindow();
        var listBox = new ListBox();
        window.Content = listBox;
        listBox.Items.Add("Item 1");
        listBox.Items.Add("Item 2");
        listBox.SelectedIndex = 0;

        bool invalidated = false;
        window.VisualChanged += (s, e) => invalidated = true;

        // Action
        listBox.OnKeyDown(new KeyEventArgs { Key = ConsoleKey.DownArrow });

        // Assert
        Assert.Equal(1, listBox.SelectedIndex);
        Assert.True(invalidated, "Arrow key selection change should trigger Invalidate via VisualChanged on Window");
    }
}
