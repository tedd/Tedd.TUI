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
            _content?.Parent = this;
        }
    }

    public override int VisualChildrenCount => (_content != null ? 1 : 0) + _overlays.Count;

    public override UIElement GetVisualChild(int index)
    {
        int contentCount = _content != null ? 1 : 0;
        if (index < contentCount)
             return _content!;

        index -= contentCount;
        if (index < _overlays.Count)
            return _overlays[index];

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
        Content?.Arrange(new Rect(0, 0, finalSize.Width, finalSize.Height));

        // Overlays are managed/arranged manually by their creators (e.g. DialogBox.Show calls Arrange)
        // or we assume they have valid RenderSize.
        // Overlays are typically absolutely positioned by their creator (e.g. DialogBox.Show),
        // but we should ensure they are measured/arranged if the window resizes?
        // For now, we rely on the fact that Show() calls Arrange().
        // If we need to support resizing updates for overlays, we'd iterate _overlays here.
    }

    public override void Render(VirtualBuffer buffer, int offsetX, int offsetY)
    {
        Content?.Render(buffer, offsetX, offsetY);
        // Render Overlays
        foreach (var overlay in _overlays)
            overlay.Render(buffer, offsetX, offsetY);
    }

    private readonly List<UIElement> _overlays = new();

    // Returns the top-most overlay if any
    public UIElement? Overlay => _overlays.Count > 0 ? _overlays[_overlays.Count - 1] : null;

    [Obsolete("Use PushOverlay instead. This method clears all existing overlays.")]
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
            _overlays.Remove(overlay);

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
                // Attempt to restore focus to something valid
                EnsureInitialFocus();
            }
        }
    }

    protected override void OnDataContextChanged(object newValue)
    {
        base.OnDataContextChanged(newValue);
        Content?.DataContext = newValue;
    }

    private UIElement _focusedElement;

    public bool SetFocus(UIElement element)
    {
        if (element == _focusedElement) return true;

        _focusedElement?.OnLostFocus();
        _focusedElement = element;
        _focusedElement?.OnGotFocus();
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
            var absPos = GetAbsolutePosition(_capturedElement);
            int localX = x - absPos.X;
            int localY = y - absPos.Y;
            return new HitTestResult(_capturedElement, localX, localY);
        }

        // 2. Check Overlays (Top to Bottom)
        for (int i = _overlays.Count - 1; i >= 0; i--)
        {
            var overlay = _overlays[i];
            if (overlay.Visibility)
            {
                var hit = InputHitTestRecursive(overlay, x, y);
                if (hit != null) 
                return hit;

                // If overlay is a modal dialog, block input to background/lower overlays
                if (overlay is DialogBox dialog && dialog.IsModal)
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
            FocusFirstIn(tabContent);
        else
            FocusFirstIn(Content);
    }

    public void ProcessKey(KeyEventArgs e)
    {
        // Bubble? Tunnel?
        // WPF uses Bubble for KeyDown.
        _focusedElement?.OnKeyDown(e);

        // Tab Navigation
        if (!e.Handled && e.Key == System.ConsoleKey.Tab)
            MoveFocus(e.Modifiers.HasFlag(System.ConsoleModifiers.Shift) ? -1 : 1);
    }

    private void MoveFocus(int direction)
    {
        // If there is an active overlay (modal), restrict focus navigation to it.
        // Use the top-most visible overlay.
        UIElement rootForFocus = Content;

        for (int i = _overlays.Count - 1; i >= 0; i--)
        {
            var overlay = _overlays[i];
            if (overlay.Visibility)
            {
                rootForFocus = overlay;
                break;
            }
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
            SetFocus(target);
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
