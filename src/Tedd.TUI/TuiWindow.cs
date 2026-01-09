using System.Collections.Generic;

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
                _content.DataContext = this.DataContext;
            }
        }
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
        _overlay = null;
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
        if (Content == null) return;

        // Flatten visual tree
        var list = new List<UIElement>();
        FlattenTree(Content, list);

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
        // Check if it is a control that accepts focus.
        // For this demo: TextBox, Button, CheckBox, RadioButton, ComboBox, ListBox.
        // StackPanel, Border etc usually not focusable.
        // We can check if it overrides OnMouseDown/KeyDown or has specific types.

        return element is TextBox ||
               element is Button ||
               element is CheckBox ||
               element is RadioButton ||
               element is ComboBox ||
               element is ListBox;
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
        else if (parent is TabControl tab)
        {
            if (tab.SelectedIndex >= 0 && tab.SelectedIndex < tab.Items.Count)
            {
                var content = tab.Items[tab.SelectedIndex].Content as UIElement;
                if (content != null) FlattenTree(content, list);
            }
        }
    }
}
