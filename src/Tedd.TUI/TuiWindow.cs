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

    public override int VisualChildrenCount => _content != null ? 1 : 0;

    public override UIElement GetVisualChild(int index)
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

        // Overlays are managed/arranged manually by their creators (e.g. DialogBox.Show calls Arrange)
        // or we assume they have valid RenderSize.
    }

    public override void Render(VirtualBuffer buffer, int offsetX, int offsetY)
    {
        if (Content != null)
        {
            Content.Render(buffer, offsetX, offsetY);
        }

        // Render Overlays
        foreach (var overlay in _overlays)
        {
            overlay.Render(buffer, offsetX, offsetY);
        }
    }

    private readonly List<UIElement> _overlays = new List<UIElement>();
    public UIElement? Overlay => _overlays.Count > 0 ? _overlays[_overlays.Count - 1] : null;

    public void SetOverlay(UIElement overlay)
    {
        ClearOverlay();
        PushOverlay(overlay);
    }

    public void PushOverlay(UIElement overlay)
    {
        if (overlay == null) return;

        // Avoid duplicates by removing if existing, moving to top
        if (_overlays.Contains(overlay))
        {
            _overlays.Remove(overlay);
        }

        _overlays.Add(overlay);
        overlay.Parent = this;
        overlay.DataContext = this.DataContext;
    }

    public void RemoveOverlay(UIElement overlay)
    {
        if (overlay == null) return;

        if (_overlays.Contains(overlay))
        {
            CheckFocusInOverlay(overlay);
            _overlays.Remove(overlay);
        }
    }

    public void ClearOverlay()
    {
        // Clear all overlays
        // Iterate backwards to clean up focus cleanly
        for (int i = _overlays.Count - 1; i >= 0; i--)
        {
            var overlay = _overlays[i];
            CheckFocusInOverlay(overlay);
        }
        _overlays.Clear();
    }

    private void CheckFocusInOverlay(UIElement overlay)
    {
        // If focus is currently within the overlay, we should clear it
        if (_focusedElement != null)
        {
            // Check if _focusedElement is child of overlay
            var current = _focusedElement;
            bool isInsideOverlay = false;
            while (current != null)
            {
                if (current == overlay)
                {
                    isInsideOverlay = true;
                    break;
                }
                current = current.Parent;
            }

            if (isInsideOverlay)
            {
                _focusedElement = null; // Clear focus
                // Attempt to restore focus?
                EnsureInitialFocus();
            }
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

    private UIElement _capturedElement;
    public UIElement? CapturedElement => _capturedElement;

    public void CaptureMouse(UIElement element)
    {
        _capturedElement = element;
    }

    public void ReleaseMouseCapture()
    {
        _capturedElement = null;
    }

    public HitTestResult InputHitTest(int x, int y)
    {
        // 1. Mouse Capture Priority
        if (_capturedElement != null)
        {
            // If an element has captured the mouse, it receives all input regardless of position.
            // We need to calculate local coordinates relative to the captured element.
            var absPos = GetAbsolutePosition(_capturedElement);
            int localX = x - absPos.X;
            int localY = y - absPos.Y;
            return new HitTestResult(_capturedElement, localX, localY);
        }

        // 2. Check Overlays (top-most first)
        for (int idx = _overlays.Count - 1; idx >= 0; idx--)
        {
            var overlay = _overlays[idx];
            if (overlay.Visibility)
            {
                var hit = InputHitTestRecursive(overlay, x, y);
                if (hit != null) return hit;

                // If overlay is a modal dialog, block input to background/lower overlays
                if (overlay is DialogBox dialog && dialog.IsModal)
                {
                    return null;
                }
            }
        }

        if (Content == null) return null;
        // Recursive search
        return InputHitTestRecursive(Content, x, y);
    }

    private Point GetAbsolutePosition(UIElement element)
    {
         int x = 0;
         int y = 0;
         var current = element;
         while (current != null)
         {
             x += current.RenderSize.X;
             y += current.RenderSize.Y;
             current = current.Parent;
         }
         return new Point(x, y);
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

            // Iterate children in reverse order (top-most first)
            int count = element.VisualChildrenCount;
            for (int i = count - 1; i >= 0; i--)
            {
                var child = element.GetVisualChild(i);
                hitChild = InputHitTestRecursive(child, localX, localY);
                if (hitChild != null) return hitChild;
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
        // Use the top-most visible overlay.
        UIElement rootForFocus = Content;

        for (int idx = _overlays.Count - 1; idx >= 0; idx--)
        {
            var overlay = _overlays[idx];
            if (overlay.Visibility)
            {
                rootForFocus = overlay;
                break;
            }
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
