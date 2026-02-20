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

        // 2. Check Overlay
        if (_overlay != null && _overlay.Visibility)
        {
            var hit = InputHitTestRecursive(_overlay, x, y);
            if (hit != null) return hit;

            // If overlay is a modal dialog, block input to background
            if (_overlay is DialogBox dialog && dialog.IsModal)
            {
                return null;
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
        UIElement rootForFocus = Content;
        if (_overlay != null && _overlay.Visibility)
        {
            rootForFocus = _overlay;
        }

        if (rootForFocus == null) return;

        UIElement firstFocusable = null;
        UIElement lastFocusable = null;
        UIElement target = null;
        UIElement previousFocusable = null;
        bool foundCurrent = false;

        foreach (var el in GetVisualTree(rootForFocus))
        {
            if (CanFocus(el))
            {
                if (firstFocusable == null) firstFocusable = el;
                lastFocusable = el;

                if (direction > 0)
                {
                    if (foundCurrent)
                    {
                        target = el;
                        break;
                    }
                    if (el == _focusedElement) foundCurrent = true;
                }
                else
                {
                    if (el == _focusedElement && !foundCurrent)
                    {
                        target = previousFocusable;
                        foundCurrent = true;
                        // We continue iteration to ensure lastFocusable is correctly identified for wraparound
                    }
                    previousFocusable = el;
                }
            }
        }

        if (target == null)
        {
            // If we didn't find a target, it's either because we reached the end (wrap around)
            // or the current focused element wasn't in this tree.
            if (direction > 0) target = firstFocusable;
            else target = lastFocusable;
        }

        if (target != null)
        {
            SetFocus(target);
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
        foreach (var el in GetVisualTree(container))
        {
            if (CanFocus(el))
            {
                SetFocus(el);
                return;
            }
        }
    }

    private IEnumerable<UIElement> GetVisualTree(UIElement root)
    {
        var stack = new Stack<(UIElement element, bool secondPass)>();
        stack.Push((root, false));

        while (stack.Count > 0)
        {
            var (current, secondPass) = stack.Pop();
            yield return current;

            if (!secondPass)
            {
                if (current is TabControl tab)
                {
                    // Second yield for tab strip
                    stack.Push((current, true));
                    // Content
                    if (tab.SelectedIndex >= 0 && tab.SelectedIndex < tab.Items.Count)
                    {
                        var content = tab.Items[tab.SelectedIndex].Content as UIElement;
                        if (content != null) stack.Push((content, false));
                    }
                }
                else
                {
                    // Normal children in reverse order
                    for (int i = current.VisualChildrenCount - 1; i >= 0; i--)
                    {
                        stack.Push((current.GetVisualChild(i), false));
                    }
                }
            }
        }
    }

    public event EventHandler VisualChanged;
    public override void Invalidate()
    {
        VisualChanged?.Invoke(this, EventArgs.Empty);
    }
}
