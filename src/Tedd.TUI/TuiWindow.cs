using System.Collections.Generic;
using System;

namespace Tedd.TUI;

public class TuiWindow : UIElement
{
    private UIElement _content;
    public UIElement Content 
    { 
        get => _content;
        set
        {
            _content = value;
            if (_content != null)
            {
                _content.Parent = this;
            }
        }
    }

    protected override int VisualChildrenCount => _content != null ? 1 : 0;

    protected override UIElement GetVisualChild(int index)
    {
        if (_content != null && index == 0) return _content;
        throw new ArgumentOutOfRangeException(nameof(index));
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        if (Content != null)
        {
            Content.Measure(availableSize);
            return Content.DesiredSize;
        }
        return new Size(0, 0);
    }

    protected override void ArrangeOverride(Size finalSize)
    {
        if (Content != null)
        {
            Content.Arrange(new Rect(0, 0, finalSize.Width, finalSize.Height));
        }

        if (_overlay != null)
        {
            // Arrange overlay to its desired size? Or full window?
            // Usually overlays like DialogBox manage their own position relative to window in Show(),
            // but we must arrange them so they get a RenderSize.
            // If we just Arrange with full size, DialogBox.ArrangeOverride is expected to handle it.
            // But DialogBox.Show() calls Arrange() manually on the Dialog.
            // However, typical custom layout logic implies the parent arranges children.
            // Let's Arrange it to full size, and trust it (or its alignment) to place itself.
            // Actually, DialogBox currently sets its own ArrangeRect in Show().
            // If TuiWindow arranges it again here, it might overwrite that.
            // But Show() sets RenderSize via Arrange.
            // If we don't arrange here, resizing the window won't update the overlay.
            // A safer bet is to arrange it to the full window if it's visible.
            
            // NOTE: DialogBox.Show currently calculates specific X/Y.
            // If we re-arrange here with (0,0, W, H), the DialogBox.ArrangeOverride needs to respect alignment or we lose the center position.
            // DialogBox.ArrangeOverride puts Content inside the border. It doesn't position itself.
            // So if we Arrange(0,0, W, H), the DialogBox becomes full screen?
            // Let's check DialogBox.MeasureOverride: it respects Width/Height if set.
            // In Show(), we set specific Rect.
            // We should respect the _overlay's DesiredSize or current RenderSize?
            
            // For now, to avoid breaking existing logic, we can skip Arranging _overlay here strictly 
            // relying on Show() logic, but that's bad for Resizing.
            // Let's just assume for now we don't need to change Arrange logic because Show() handles it.
            // But the user plan mentioned updating it. 
            // In Layered rendering specifically, we just need _overlay to have valid RenderSize.
        }
    }

    public override void Render(VirtualBuffer buffer, int offsetX, int offsetY)
    {
        if (Content != null)
        {
            Content.Render(buffer, offsetX, offsetY);
        }

        // Render Overlay
        if (_overlay != null)
        {
            _overlay.Render(buffer, offsetX, offsetY);
        }
    }

    private UIElement _overlay;
    public UIElement? Overlay => _overlay;

    public void SetOverlay(UIElement overlay)
    {
        _overlay = overlay;
        if (_overlay != null)
        {
            _overlay.Parent = this; // Technically parented to window
            _overlay.DataContext = this.DataContext;
        }
    }

    public void ClearOverlay()
    {
        if (_overlay != null)
        {
            // If focus is currently within the overlay, we should clear it
            // so hidden controls don't keep receiving input.
            if (_focusedElement != null)
            {
                // Check if _focusedElement is child of _overlay
                var current = _focusedElement;
                bool isInsideOverlay = false;
                while (current != null)
                {
                    if (current == _overlay)
                    {
                        isInsideOverlay = true;
                        break;
                    }
                    current = current.Parent;
                }

                if (isInsideOverlay)
                {
                    _focusedElement = null; // Clear focus
                    // Attempt to restore focus to something valid in the main content
                    // Since we don't have a history, we'll use EnsureInitialFocus or similar logic
                    // EnsureInitialFocus() checks _focusedElement null, so it will try to find something.
                    EnsureInitialFocus();
                }
            }
            _overlay = null;
        }
    }

    protected override void OnDataContextChanged(object newValue)
    {
        base.OnDataContextChanged(newValue);
        if (Content != null)
        {
            Content.DataContext = newValue;
        }
    }

    private UIElement _focusedElement;

    public bool SetFocus(UIElement element)
    {
        if (element == _focusedElement) return true;

        if (_focusedElement != null)
        {
            _focusedElement.OnLostFocus();
        }

        _focusedElement = element;

        if (_focusedElement != null)
        {
            _focusedElement.OnGotFocus();
        }
        return true;
    }

    public HitTestResult InputHitTest(int x, int y)
    {
        // Check Overlay first
        if (_overlay != null && _overlay.Visibility)
        {
            var hit = InputHitTestRecursive(_overlay, x, y);
            if (hit != null) return hit;
        }

        if (Content == null) return null;
        // Recursive search
        return InputHitTestRecursive(Content, x, y);
    }

    private HitTestResult InputHitTestRecursive(UIElement element, int x, int y)
    {
        if (!element.Visibility) return null;

        // x and y are relative to element.Parent

        if (x >= element.RenderSize.X && x < element.RenderSize.X + element.RenderSize.Width &&
            y >= element.RenderSize.Y && y < element.RenderSize.Y + element.RenderSize.Height)
        {
            // Point is inside this element.
            // New coordinates relative to this element:
            int localX = x - element.RenderSize.X;
            int localY = y - element.RenderSize.Y;

            HitTestResult hitChild = null;

            if (element is StackPanel stack)
            {
                for (int i = stack.Children.Count - 1; i >= 0; i--)
                {
                    hitChild = InputHitTestRecursive(stack.Children[i], localX, localY);
                    if (hitChild != null) return hitChild;
                }
            }
            else if (element is Border border && border.Child != null)
            {
                hitChild = InputHitTestRecursive(border.Child, localX, localY);
                if (hitChild != null) return hitChild;
            }
            else if (element is DialogBox dialog && dialog.Content != null)
            {
                hitChild = InputHitTestRecursive(dialog.Content, localX, localY);
                if (hitChild != null) return hitChild;
            }
            else if (element is TabControl tab)
            {
                if (tab.SelectedIndex >= 0 && tab.SelectedIndex < tab.Items.Count)
                {
                    var content = tab.Items[tab.SelectedIndex].Content as UIElement;
                    if (content != null)
                    {
                        hitChild = InputHitTestRecursive(content, localX, localY);
                        if (hitChild != null) return hitChild;
                    }
                }

                // If not hit content, it is the tab header area (the control itself)
                return new HitTestResult(element, localX, localY);
            }

            // If no child hit, but we are inside, return self with local coordinates
            return new HitTestResult(element, localX, localY);
        }

        return null;
    }

    /// <summary>
    /// Sets focus to the first focusable element when none is set (e.g. on startup).
    /// If content is a TabControl, focuses the first control inside the selected tab so Tab order works.
    /// </summary>
    public void EnsureInitialFocus()
    {
        if (_focusedElement != null) return;
        if (Content == null) return;

        if (Content is TabControl tc && tc.SelectedIndex >= 0 && tc.SelectedIndex < tc.Items.Count
            && tc.Items[tc.SelectedIndex].Content is UIElement tabContent)
        {
            FocusFirstIn(tabContent);
        }
        else
        {
            FocusFirstIn(Content);
        }
    }

    public void ProcessKey(KeyEventArgs e)
    {
        // Bubble? Tunnel?
        // WPF uses Bubble for KeyDown.
        if (_focusedElement != null)
        {
            _focusedElement.OnKeyDown(e);
        }

        // Tab Navigation
        if (!e.Handled && e.Key == System.ConsoleKey.Tab)
        {
             MoveFocus(e.Modifiers.HasFlag(System.ConsoleModifiers.Shift) ? -1 : 1);
        }
    }

    private void MoveFocus(int direction)
    {
        // If there is an active overlay (modal), restrict focus navigation to it.
        UIElement rootForFocus = Content;
        if (_overlay != null && _overlay.Visibility)
        {
            rootForFocus = _overlay;
        }

        if (rootForFocus == null) return;

        // Flatten visual tree
        var list = new List<UIElement>();
        FlattenTree(rootForFocus, list);

        // Filter focusable?
        // For simplicity, anything handling input or marked IsEnabled.
        // We really need Focusable property, but let's assume all inputs we made are focusable.
        // Or check type.
        // Ideally we check IsEnabled && Visibility

        // Find current
        int index = list.IndexOf(_focusedElement);
        int start = index;
        if (start < 0) start = -1;

        // Loop to find next focusable
        int count = list.Count;
        if (count == 0) return;

        int i = start;
        while(true)
        {
            i += direction;
            if (i >= count) i = 0;
            if (i < 0) i = count - 1;

            if (i == start) break; // looped around

            var candidate = list[i];
            if (CanFocus(candidate))
            {
                SetFocus(candidate);
                return;
            }
        }
    }

    private bool CanFocus(UIElement element)
    {
        if (!element.IsEnabled || !element.Visibility) return false;
        return element.Focusable;
    }

    /// <summary>
    /// Sets focus to the first focusable element inside the given container (e.g. tab content).
    /// Called by TabControl when the selected tab changes so keys go to the visible tab.
    /// </summary>
    public void FocusFirstIn(UIElement container)
    {
        if (container == null) return;
        var list = new List<UIElement>();
        FlattenTree(container, list);
        foreach (var el in list)
        {
            if (CanFocus(el))
            {
                SetFocus(el);
                return;
            }
        }
    }

    private void FlattenTree(UIElement parent, List<UIElement> list)
    {
        list.Add(parent);

        if (parent is StackPanel stack)
        {
            foreach(var child in stack.Children) FlattenTree(child, list);
        }
        else if (parent is Border border && border.Child != null)
        {
            FlattenTree(border.Child, list);
        }
        else if (parent is DialogBox dialog && dialog.Content != null)
        {
            FlattenTree(dialog.Content, list);
        }
        else if (parent is TabControl tab)
        {
            // Add selected tab content first so first focusable is inside the tab (e.g. nameBox)
            if (tab.SelectedIndex >= 0 && tab.SelectedIndex < tab.Items.Count)
            {
                var content = tab.Items[tab.SelectedIndex].Content as UIElement;
                if (content != null) FlattenTree(content, list);
            }
            list.Add(tab); // TabControl (tab strip) after content so Tab from last control goes to strip, then to next section
        }
    }

    public event EventHandler VisualChanged;
    public override void Invalidate()
    {
        VisualChanged?.Invoke(this, EventArgs.Empty);
    }
}
