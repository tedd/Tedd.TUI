using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using Tedd.TUI;

namespace Tedd.TUI.Tests;

public class FocusOrderTests
{
    [Fact]
    public void TestVisualTreeOrder_StackPanel()
    {
        var root = new StackPanel();
        var b1 = new Button { Content = "B1" };
        var b2 = new Button { Content = "B2" };
        root.AddChild(b1);
        root.AddChild(b2);

        var window = new TuiWindow();
        var tree = GetTree(window, root).ToList();

        // Order: StackPanel, B1 (tree), B2 (tree)
        // Button tree depth has increased due to ControlTemplate (Button -> Border -> (ContentPresenter, ScrollBar?) -> TextBlock).
        // Actual node count is 11.
        // 1 (SP) + 5 (B1) + 5 (B2) = 11.
        Assert.Equal(11, tree.Count);
        Assert.Same(root, tree[0]);
        Assert.Same(b1, tree[1]);
        // B2 is at index 1 + 5 = 6
        Assert.Same(b2, tree[6]);
    }

    [Fact]
    public void TestVisualTreeOrder_TabControl()
    {
        var tc = new TabControl();
        var b1 = new Button { Content = "B1" };
        tc.AddItem(new TabItem { Header = "Tab1", Content = b1 });
        tc.SelectedIndex = 0;

        var window = new TuiWindow();
        var tree = GetTree(window, tc).ToList();

        // Order: TabControl, B1 (tree), TabControl (special double yield)
        // 1 (Tab) + 5 (B1) + 1 (Tab) = 7.
        Assert.Equal(7, tree.Count);
        Assert.Same(tc, tree[0]);
        Assert.Same(b1, tree[1]);
        Assert.Same(tc, tree[6]);
    }

    [Fact]
    public void TestMoveFocusForward()
    {
        var window = new TuiWindow();
        var root = new StackPanel();
        var b1 = new Button { Content = "B1", Focusable = true };
        var b2 = new Button { Content = "B2", Focusable = true };
        root.AddChild(b1);
        root.AddChild(b2);
        window.Content = root;

        window.SetFocus(b1);
        // Move focus forward
        var method = typeof(TuiWindow).GetMethod("MoveFocus", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        method.Invoke(window, new object[] { 1 });

        // Should be at b2
        var focused = typeof(TuiWindow).GetField("_focusedElement", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(window);
        Assert.Same(b2, focused);

        // Move forward again (wrap around)
        // Note: StackPanel is NOT focusable by default (Focusable = false in UIElement)
        // Buttons ARE focusable.
        method.Invoke(window, new object[] { 1 });
        focused = typeof(TuiWindow).GetField("_focusedElement", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(window);
        Assert.Same(b1, focused);
    }

    [Fact]
    public void TestMoveFocusBackward()
    {
        var window = new TuiWindow();
        var root = new StackPanel();
        var b1 = new Button { Content = "B1", Focusable = true };
        var b2 = new Button { Content = "B2", Focusable = true };
        root.AddChild(b1);
        root.AddChild(b2);
        window.Content = root;

        window.SetFocus(b1);
        var method = typeof(TuiWindow).GetMethod("MoveFocus", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        // Move focus backward (wrap around to b2)
        method.Invoke(window, new object[] { -1 });
        var focused = typeof(TuiWindow).GetField("_focusedElement", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(window);
        Assert.Same(b2, focused);

        // Move backward again to b1
        method.Invoke(window, new object[] { -1 });
        focused = typeof(TuiWindow).GetField("_focusedElement", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(window);
        Assert.Same(b1, focused);
    }

    private IEnumerable<UIElement> GetTree(TuiWindow window, UIElement root)
    {
        var method = typeof(TuiWindow).GetMethod("GetVisualTree", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        return (IEnumerable<UIElement>)method.Invoke(window, new object[] { root });
    }
}
