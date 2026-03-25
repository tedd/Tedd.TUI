using System;
using Xunit;
using Tedd.TUI;
using System.Collections.Generic;

namespace Tedd.TUI.Tests;

public class ListBoxMeasureTests
{
    [Fact]
    public void Measure_FixedHeight_UsesFixedHeight()
    {
        var listBox = new ListBox();
        listBox.Height = 10;
        listBox.Items.Add("Item 1");

        listBox.Measure(new Size(100, 100));

        Assert.Equal(10, listBox.DesiredSize.Height);
    }

    [Fact]
    public void Measure_AutoHeight_UsesItemCount()
    {
        var listBox = new ListBox();
        // Default Height is -1 (Auto)
        listBox.Items.Add("Item 1");
        listBox.Items.Add("Item 2");
        listBox.Items.Add("Item 3");

        // Available size is large enough
        listBox.Measure(new Size(100, 100));

        // Expect height to be 3 (number of items)
        Assert.Equal(3, listBox.DesiredSize.Height);
    }

    [Fact]
    public void Measure_AutoHeight_ConstrainedByAvailableSize()
    {
        var listBox = new ListBox();
        // Default Height is -1 (Auto)
        for (int i = 0; i < 10; i++)
        {
            listBox.Items.Add($"Item {i}");
        }

        // Available size is smaller (5)
        listBox.Measure(new Size(100, 5));

        // Expect height to be 5 (available size)
        Assert.Equal(5, listBox.DesiredSize.Height);
    }

    [Fact]
    public void Measure_FixedWidth_UsesFixedWidth()
    {
        var listBox = new ListBox();
        listBox.Width = 20;
        listBox.Items.Add("Long Item Name");

        listBox.Measure(new Size(100, 100));

        Assert.Equal(20, listBox.DesiredSize.Width);
    }

    [Fact]
    public void Measure_AutoWidth_UsesMaxItemWidth()
    {
        var listBox = new ListBox();
        // Default Width is -1 (Auto)
        listBox.Items.Add("Short");
        listBox.Items.Add("Long Item Name"); // Length 14

        // Available size is large enough
        listBox.Measure(new Size(100, 100));

        // Expect width to accommodate "Long Item Name" (14)
        Assert.Equal(15, listBox.DesiredSize.Width);
    }

    [Fact]
    public void Measure_AutoWidth_WithScrollbar()
    {
        var listBox = new ListBox();
        // Default Width is -1 (Auto)
        // Add items > available height to trigger scrollbar
        for (int i = 0; i < 10; i++)
        {
            listBox.Items.Add("Item"); // Length 4
        }

        // Available height 5, so scrollbar needed.
        listBox.Measure(new Size(100, 5));

        // Height should be 5.
        Assert.Equal(5, listBox.DesiredSize.Height);

        // Width should be 4 (Item) + 1 (ScrollBar) = 5.
        Assert.Equal(5, listBox.DesiredSize.Width);
    }
}
