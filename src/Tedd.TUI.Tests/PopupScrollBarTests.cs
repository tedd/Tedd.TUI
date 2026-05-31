using Xunit;
using Tedd.TUI;
using System.Reflection;

namespace Tedd.TUI.Tests;

public class PopupScrollBarTests
{
    [Fact]
    public void MenuItem_PopupBorder_UsesAutoScrollBarVisibility()
    {
        // Arrange
        var window = new TuiWindow();
        var menuBar = new MenuBar() { VerticalAlignment = VerticalAlignment.Top };
        window.Content = menuBar;

        var fileMenu = new MenuItem { Header = new TextBlock { Text = "File" } };
        fileMenu.Items.Add(new MenuItem { Header = new TextBlock { Text = "Open" } });
        menuBar.AddChild(fileMenu);

        window.Measure(new Size(80, 25));
        window.Arrange(new Rect(0, 0, 80, 25));

        // Act
        fileMenu.OpenSubMenu();

        // Assert
        var overlay = window.Overlay as Border;
        Assert.NotNull(overlay);
        Assert.Equal(ScrollBarVisibility.Auto, overlay.VerticalScrollBarVisibility);
    }

    [Fact]
    public void ComboBox_PopupBorder_UsesAutoScrollBarVisibility()
    {
        // Arrange
        var window = new TuiWindow();
        var comboBox = new ComboBox();
        comboBox.Items.Add("Item 1");
        comboBox.Items.Add("Item 2");
        window.Content = comboBox;

        window.Measure(new Size(80, 25));
        window.Arrange(new Rect(0, 0, 80, 25));

        // Act
        // ComboBox uses internal OpenDropdown via ToggleDropdown which calls GetRoot()
        // We simulate a mouse down to open the dropdown
        comboBox.OnMouseDown(new MouseEventArgs { X = 0, Y = 0 });

        // Assert
        var overlay = window.Overlay as Border;
        Assert.NotNull(overlay);
        Assert.Equal(ScrollBarVisibility.Auto, overlay.VerticalScrollBarVisibility);
    }

    [Fact]
    public void Border_DefaultsToDisabledScrollBars()
    {
        var border = new Border();
        Assert.Equal(ScrollBarVisibility.Disabled, border.VerticalScrollBarVisibility);
        Assert.Equal(ScrollBarVisibility.Disabled, border.HorizontalScrollBarVisibility);
    }

    [Fact]
    public void GroupBox_UsesDisabledScrollBarsInTemplate()
    {
        var groupBox = new GroupBox();
        groupBox.Measure(new Size(20, 20));
        groupBox.Arrange(new Rect(0, 0, 20, 20));

        // Find template internal border
        var child = groupBox.GetVisualChild(0) as Border;
        Assert.NotNull(child);
        Assert.Equal(ScrollBarVisibility.Disabled, child.VerticalScrollBarVisibility);
        Assert.Equal(ScrollBarVisibility.Disabled, child.HorizontalScrollBarVisibility);
    }
}
