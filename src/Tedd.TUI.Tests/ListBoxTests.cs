using System;
using Xunit;
using Tedd.TUI;
using System.Collections.Generic;
using System.Linq;
using Tedd.TUI.Tests.TestInfrastructure;

namespace Tedd.TUI.Tests;

public class ListBoxTests
{
    [Fact]
    public void MouseClick_NestedListBoxes_SelectsOnlyClickedRowsAndHonorsScrollOffset()
    {
        var first = new ListBox { Width = 9, Height = 3 };
        var second = new ListBox { Width = 9, Height = 3 };
        for (var i = 0; i < 6; i++)
        {
            first.Items.Add($"A{i}");
            second.Items.Add($"B{i}");
        }

        var lists = new StackPanel { Orientation = Orientation.Horizontal };
        lists.AddChild(first);
        lists.AddChild(new TextBlock { Text = "  " });
        lists.AddChild(second);

        var surface = new StackPanel();
        surface.AddChild(new TextBlock { Text = "Pick rows" });
        surface.AddChild(lists);
        surface.AddChild(new TextBlock { Text = "unused surface" });

        var host = new ControlTestHost(new Border { Child = surface }, 24, 8);

        host.Click(first, 2, 1);
        Assert.Equal(1, first.SelectedIndex);
        Assert.Equal(-1, second.SelectedIndex);

        host.Click(second, 2, 2);
        Assert.Equal(1, first.SelectedIndex);
        Assert.Equal(2, second.SelectedIndex);

        // Click the first list's scrollbar down arrow, then the same visible row.
        host.Click(first, first.RenderSize.Width - 1, first.RenderSize.Height - 1);
        host.Click(first, 2, 2);

        Assert.Equal(3, first.SelectedIndex);
        Assert.Equal("A3", first.SelectedItem);
        Assert.Equal(2, second.SelectedIndex);
        Assert.Equal("B2", second.SelectedItem);
    }

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

    [Fact]
    public void Render_UsesItemTemplate_WhenSet()
    {
        var listBox = new ListBox();
        listBox.Width = 10;
        listBox.Height = 2;

        var template = new DataTemplate(() =>
        {
            var tb = new TextBlock();
            tb.SetBinding(TextBlock.TextProperty, new Binding("Name"));
            return tb;
        });
        listBox.ItemTemplate = template;

        listBox.Items.Add(new { Name = "Alice" });
        listBox.Items.Add(new { Name = "Bob" });

        listBox.Measure(new Size(10, 2));
        listBox.Arrange(new Rect(0, 0, 10, 2));

        var buffer = new VirtualBuffer(10, 2);
        listBox.Render(buffer, 0, 0);

        // The template renders a TextBlock bound to "Name" – verify the bound text appears in the buffer
        var row0 = string.Concat(Enumerable.Range(0, 5).Select(dx => buffer.GetPixel(dx, 0).Character));
        var row1 = string.Concat(Enumerable.Range(0, 3).Select(dx => buffer.GetPixel(dx, 1).Character));

        Assert.Equal("Alice", row0);
        Assert.Equal("Bob", row1);
    }

    [Fact]
    public void Render_FallsBackToGetItemText_WhenNoTemplate()
    {
        var listBox = new ListBox();
        listBox.Width = 10;
        listBox.Height = 2;
        listBox.Items.Add("Hello");
        listBox.Items.Add("World");

        listBox.Measure(new Size(10, 2));
        listBox.Arrange(new Rect(0, 0, 10, 2));

        var buffer = new VirtualBuffer(10, 2);
        listBox.Render(buffer, 0, 0);

        var row0 = string.Concat(Enumerable.Range(0, 5).Select(dx => buffer.GetPixel(dx, 0).Character));
        var row1 = string.Concat(Enumerable.Range(0, 5).Select(dx => buffer.GetPixel(dx, 1).Character));

        Assert.Equal("Hello", row0);
        Assert.Equal("World", row1);
    }
}
