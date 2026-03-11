using System;
using Xunit;
using Tedd.TUI;
using System.Collections.Generic;
using System.Linq;

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
    public void OnGotFocus_UpdatesSelectedItemVisualState()
    {
        // Arrange
        var window = new TuiWindow();
        var listBox = new ListBox();
        window.Content = listBox;
        listBox.Items.Add("Item 1");
        listBox.Items.Add("Item 2");
        listBox.SelectedIndex = 0;
        listBox.Measure(new Size(20, 5));
        listBox.Arrange(new Rect(0, 0, 20, 5));

        var container = listBox.ItemsPanelRoot?.Children[0] as ListBoxItem;
        Assert.NotNull(container);
        Assert.True(container.IsSelected);

        // Before focus, item should show unfocused selection colors
        listBox.OnLostFocus();
        var unfocusedBg = container.Background;
        var unfocusedFg = container.Foreground;

        // Act: ListBox gains focus
        listBox.OnGotFocus();

        // Assert: selected item should now show focused selection colors
        Assert.NotEqual(unfocusedBg, container.Background);
        Assert.NotEqual(unfocusedFg, container.Foreground);
        Assert.Equal(listBox.FocusedSelectionBackground, container.Background);
        Assert.Equal(listBox.FocusedSelectionForeground, container.Foreground);
    }

    [Fact]
    public void OnLostFocus_UpdatesSelectedItemVisualState()
    {
        // Arrange
        var window = new TuiWindow();
        var listBox = new ListBox();
        window.Content = listBox;
        listBox.Items.Add("Item 1");
        listBox.Items.Add("Item 2");
        listBox.SelectedIndex = 0;
        listBox.Measure(new Size(20, 5));
        listBox.Arrange(new Rect(0, 0, 20, 5));

        var container = listBox.ItemsPanelRoot?.Children[0] as ListBoxItem;
        Assert.NotNull(container);

        // First give focus so item shows focused selection colors
        listBox.OnGotFocus();
        Assert.Equal(listBox.FocusedSelectionBackground, container.Background);

        // Act: ListBox loses focus
        listBox.OnLostFocus();

        // Assert: selected item should now show unfocused selection colors (ShowSelection=true by default)
        Assert.Equal(listBox.SelectionBackground, container.Background);
        Assert.Equal(listBox.SelectionForeground, container.Foreground);
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
