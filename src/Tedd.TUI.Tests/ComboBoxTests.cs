using System;
using System.Reflection;
using Xunit;

namespace Tedd.TUI.Tests;

public class ComboBoxTests
{
    [Fact]
    public void TestDropdownHeight_WithManyItems_FitsInSpace()
    {
        // Setup
        var window = new TuiWindow();
        window.Measure(new Size(80, 25));
        window.Arrange(new Rect(0, 0, 80, 25));

        var comboBox = new ComboBox { VerticalAlignment = VerticalAlignment.Top };
        for (int i = 0; i < 10; i++)
        {
            comboBox.Items.Add($"Item {i}");
        }
        window.Content = comboBox;

        // Ensure layout
        window.Measure(new Size(80, 25));
        window.Arrange(new Rect(0, 0, 80, 25));

        // Act - Open dropdown
        var method = typeof(ComboBox).GetMethod("OpenDropdown", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(method);
        method.Invoke(comboBox, new object[] { window });

        // Assert - Check _popupListBox height
        var field = typeof(ComboBox).GetField("_popupListBox", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(field);
        var popupListBox = (ListBox)field.GetValue(comboBox);

        // Should be 10 (number of items) as there is plenty of space (24 lines below)
        Assert.Equal(10, popupListBox.Height);
    }

    [Fact]
    public void TestDropdownHeight_WithFewItems()
    {
        // Setup
        var window = new TuiWindow();
        window.Measure(new Size(80, 25));
        window.Arrange(new Rect(0, 0, 80, 25));

        var comboBox = new ComboBox { VerticalAlignment = VerticalAlignment.Top };
        comboBox.Items.Add("Item 1");
        comboBox.Items.Add("Item 2");
        window.Content = comboBox;

        // Ensure layout
        window.Measure(new Size(80, 25));
        window.Arrange(new Rect(0, 0, 80, 25));

        // Act - Open dropdown
        var method = typeof(ComboBox).GetMethod("OpenDropdown", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(method);
        method.Invoke(comboBox, new object[] { window });

        // Assert - Check _popupListBox height
        var field = typeof(ComboBox).GetField("_popupListBox", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(field);
        var popupListBox = (ListBox)field.GetValue(comboBox);

        // Should be 2 (number of items)
        Assert.Equal(2, popupListBox.Height);
    }

    [Fact]
    public void TestDropdownHeight_ClampedBySpace()
    {
        // Setup small window
        var window = new TuiWindow();
        // 5 height total. ComboBox takes 1. Space below is 4.
        // Border takes 2. Content space is 2.
        window.Measure(new Size(80, 5));
        window.Arrange(new Rect(0, 0, 80, 5));

        var comboBox = new ComboBox { VerticalAlignment = VerticalAlignment.Top };
        for (int i = 0; i < 10; i++)
        {
            comboBox.Items.Add($"Item {i}");
        }
        window.Content = comboBox;

        // Ensure layout
        window.Measure(new Size(80, 5));
        window.Arrange(new Rect(0, 0, 80, 5));

        // Act - Open dropdown
        var method = typeof(ComboBox).GetMethod("OpenDropdown", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(method);
        method.Invoke(comboBox, new object[] { window });

        // Assert
        var field = typeof(ComboBox).GetField("_popupListBox", BindingFlags.NonPublic | BindingFlags.Instance);
        var popupListBox = (ListBox)field.GetValue(comboBox);

        // Expected: Window H=5. ComboBox Y=0, H=1. Bottom=1. Space below=4.
        // Border H = ContentH + 2.
        // ContentH max = SpaceBelow - 2 = 2.
        Assert.Equal(2, popupListBox.Height);
    }
}
