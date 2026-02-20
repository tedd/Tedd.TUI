using System;
using Xunit;
using Tedd.TUI;

namespace Tedd.TUI.Tests;

public class TabControlTests
{
    [Fact]
    public void Properties_DefaultValues()
    {
        var tc = new TabControl();
        Assert.Equal(0, tc.SelectedIndex);
        Assert.Empty(tc.Items);
        Assert.Equal(BoxStyle.Single, tc.BoxStyle);
    }

    [Fact]
    public void AddItem_IncreasesCount()
    {
        var tc = new TabControl();
        var item1 = new TabItem { Header = "H1", Content = new Button() };
        tc.AddItem(item1);
        Assert.Single(tc.Items);
        Assert.Equal(0, tc.SelectedIndex);
        Assert.Equal(tc, item1.Parent);
        Assert.Equal(tc, ((UIElement)item1.Content).Parent);
    }

    [Fact]
    public void SelectedIndex_Change_UpdatesContent()
    {
        var tc = new TabControl();
        var item1 = new TabItem { Header = "H1", Content = new Button() };
        var item2 = new TabItem { Header = "H2", Content = new Button() };
        tc.AddItem(item1);
        tc.AddItem(item2);

        tc.SelectedIndex = 1;
        Assert.Equal(1, tc.SelectedIndex);
        // VisualChild should be item2's content
        Assert.Equal(item2.Content, tc.GetVisualChild(0));
    }

    [Fact]
    public void OnKeyDown_Navigation()
    {
        var tc = new TabControl();
        tc.AddItem(new TabItem { Header = "1" });
        tc.AddItem(new TabItem { Header = "2" });
        tc.AddItem(new TabItem { Header = "3" });

        tc.SelectedIndex = 0;
        tc.OnKeyDown(new KeyEventArgs { Key = ConsoleKey.RightArrow });
        Assert.Equal(1, tc.SelectedIndex);

        tc.OnKeyDown(new KeyEventArgs { Key = ConsoleKey.RightArrow });
        Assert.Equal(2, tc.SelectedIndex);

        tc.OnKeyDown(new KeyEventArgs { Key = ConsoleKey.RightArrow });
        Assert.Equal(0, tc.SelectedIndex); // Wrap

        tc.OnKeyDown(new KeyEventArgs { Key = ConsoleKey.LeftArrow });
        Assert.Equal(2, tc.SelectedIndex); // Wrap back
    }
}
