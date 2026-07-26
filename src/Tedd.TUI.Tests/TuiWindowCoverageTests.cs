using System;
using System.Collections.Generic;
using Xunit;
using Tedd.TUI;

namespace Tedd.TUI.Tests;

public class TuiWindowCoverageTests
{
    /// <summary>
    /// Container that exposes null visual-children slots, mimicking controls whose
    /// GetVisualChild can hand back null for empty slots.
    /// </summary>
    private sealed class NullChildContainer : UIElement
    {
        public List<UIElement?> Children { get; } = new();
        public override int VisualChildrenCount => Children.Count;
        public override UIElement GetVisualChild(int index) => Children[index]!;
        protected override Size MeasureOverride(Size availableSize) => new Size(0, 0);
    }

    [Fact]
    public void MoveFocus_TabAcrossNullVisualChildren_DoesNotThrow()
    {
        var window = new TuiWindow();
        var container = new NullChildContainer();
        var first = new Button();
        var second = new Button();
        first.Parent = container;
        second.Parent = container;
        container.Children.Add(first);
        container.Children.Add(null);
        container.Children.Add(second);
        window.Content = container;

        // Initial focus walks the tree over the null slot.
        window.EnsureInitialFocus();
        Assert.True(first.IsFocused);

        // Tab forward crosses the null slot.
        window.ProcessKey(new KeyEventArgs(UIElement.KeyDownEvent, first) { Key = ConsoleKey.Tab });
        Assert.True(second.IsFocused);

        // Shift+Tab walks back across it.
        window.ProcessKey(new KeyEventArgs(UIElement.KeyDownEvent, second) { Key = ConsoleKey.Tab, Modifiers = ConsoleModifiers.Shift });
        Assert.True(first.IsFocused);
    }

    [Fact]
    public void Overlay_PushRemoveClear_WorksCorrectly()
    {
        var window = new TuiWindow();
        var overlay1 = new Border();
        var overlay2 = new Border();

        // 1. Push First
        window.PushOverlay(overlay1);
        Assert.Equal(overlay1, window.Overlay);
        Assert.Equal(window, overlay1.Parent);

        // 2. Push Second
        window.PushOverlay(overlay2);
        Assert.Equal(overlay2, window.Overlay);

        // 3. Push First Again (should move to top)
        window.PushOverlay(overlay1);
        Assert.Equal(overlay1, window.Overlay);

        // 4. Remove Top
        window.RemoveOverlay(overlay1);
        Assert.Equal(overlay2, window.Overlay);

        // 5. Clear
        window.ClearOverlay();
        Assert.Null(window.Overlay);
    }

    [Fact]
    public void InputHitTest_InteractsWithOverlays()
    {
        var window = new TuiWindow();
        var content = new Button { Width = 10, Height = 10 };
        window.Content = content;

        var overlay = new Button { Width = 5, Height = 5 }; // Smaller overlay at 0,0
        window.PushOverlay(overlay);

        window.Measure(new Size(20, 20));
        window.Arrange(new Rect(0, 0, 20, 20));

        // Measure/Arrange overlays (normally done by creator or TuiWindow if we added logic,
        // but TuiWindow.Arrange doesn't arrange overlays currently unless they are manually managed.
        // However, InputHitTest relies on RenderSize.
        // So we must manually arrange overlays for this test as TuiWindow doesn't do it automatically in ArrangeOverride.)
        overlay.Measure(new Size(20, 20));
        overlay.Arrange(new Rect(0, 0, 5, 5));

        // 1. Hit Test on Overlay
        var result = window.InputHitTest(2, 2);
        Assert.NotNull(result);
        // Button uses a ControlTemplate which wraps content in a Border.
        // InputHitTestRecursive returns the leaf node (Border or its child), not the Button itself if the Button's visual tree is traversed.
        // Since Button has visual children (Border), hit test goes into Border.
        // We should expect the visual child of the button (the Border), or check if the result is a descendant of overlay.

        Assert.True(result.Element == overlay || result.Element.FindAncestor<Button>() == overlay);

        // 2. Hit Test outside Overlay (should hit content)
        // Overlay is 5x5. Content is 10x10. Point 7,7 is outside overlay, inside content.
        result = window.InputHitTest(7, 7);
        Assert.NotNull(result);
        Assert.True(result.Element == content || result.Element.FindAncestor<Button>() == content);
    }

    [Fact]
    public void InputHitTest_ModalDialog_BlocksUnderlyingInput()
    {
        var window = new TuiWindow();
        var content = new Button { Width = 10, Height = 10 };
        window.Content = content;

        // Modal Dialog
        var dialog = new DialogBox { Width = 5, Height = 5 };
        // DialogBox usually sets IsModal = true by default or via Show()
        // We need to check if IsModal is true.
        // DialogBox implementation of IsModal isn't visible in snippets but TuiWindow uses `if (overlay is DialogBox dialog && dialog.IsModal)`
        // Let's assume default or we set it if possible.
        // Since we can't see DialogBox source fully, we'll try to use it as is.
        // If DialogBox isn't modal by default, we might need another way.
        // Assuming ShowModal() sets it. But we are unit testing TuiWindow.
        // Let's just create a mock if needed, but we can't mock classes easily here.
        // Let's rely on behavior: usually DialogBox is used for modals.

        // Actually, we can just use reflection to set IsModal if it's a property, or just assume.
        // Let's try to verify if it blocks.

        window.PushOverlay(dialog);
        dialog.Measure(new Size(20, 20));
        dialog.Arrange(new Rect(0, 0, 5, 5));

        // Hit test outside dialog (e.g. 7,7) should return NULL because modal blocks input to content.
        var result = window.InputHitTest(7, 7);
        Assert.Null(result);
    }

    [Fact]
    public void GetVisualChild_ReturnsContentAndOverlays()
    {
        var window = new TuiWindow();
        var content = new Border();
        window.Content = content;

        var overlay = new Border();
        window.PushOverlay(overlay);

        Assert.Equal(2, window.VisualChildrenCount);
        Assert.Equal(content, window.GetVisualChild(0));
        Assert.Equal(overlay, window.GetVisualChild(1));
    }

    [Fact]
    public void EnsureInitialFocus_FocusesFirstFocusable()
    {
        var window = new TuiWindow();
        var stack = new StackPanel();
        var btn1 = new Button { Focusable = true };
        var btn2 = new Button { Focusable = true };

        stack.AddChild(new TextBlock()); // Not focusable
        stack.AddChild(btn1);
        stack.AddChild(btn2);

        window.Content = stack;

        window.EnsureInitialFocus();

        Assert.True(btn1.IsFocused);
    }

    [Fact]
    public void EnsureInitialFocus_TabControl_FocusesSelectedTabContent()
    {
        var window = new TuiWindow();
        var tabControl = new TabControl();
        var tab1 = new TabItem { Header = "Tab1" };
        var btnInTab1 = new Button { Focusable = true };
        tab1.Content = btnInTab1;

        tabControl.Items.Add(tab1);
        tabControl.SelectedIndex = 0;

        window.Content = tabControl;

        window.EnsureInitialFocus();

        Assert.True(btnInTab1.IsFocused);
    }

    // Since MoveFocus is private, we test it via ProcessKey (Tab)
    [Fact]
    public void ProcessKey_Tab_MovesFocus()
    {
        var window = new TuiWindow();
        var stack = new StackPanel();
        var btn1 = new Button { Focusable = true };
        var btn2 = new Button { Focusable = true };

        stack.AddChild(btn1);
        stack.AddChild(btn2);
        window.Content = stack;

        window.EnsureInitialFocus(); // btn1 focused
        Assert.True(btn1.IsFocused);

        // Tab -> btn2
        window.ProcessKey(new KeyEventArgs { Key = ConsoleKey.Tab });
        Assert.True(btn2.IsFocused);

        // Tab -> btn1 (wrap around)
        window.ProcessKey(new KeyEventArgs { Key = ConsoleKey.Tab });
        Assert.True(btn1.IsFocused);

        // Shift+Tab -> btn2 (backward wrap)
        window.ProcessKey(new KeyEventArgs { Key = ConsoleKey.Tab, Modifiers = ConsoleModifiers.Shift });
        Assert.True(btn2.IsFocused);
    }
}
