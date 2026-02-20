using System;
using System.Collections.Generic;

namespace Tedd.TUI;

public class MenuItem : UIElement
{
    private UIElement _header;
    public UIElement Header
    {
        get => _header;
        set
        {
            if (_header != value)
            {
                if (_header != null) _header.Parent = null;
                _header = value;
                if (_header != null) _header.Parent = this;
                Invalidate();
            }
        }
    }

    public List<UIElement> Items { get; } = new List<UIElement>();
    public Action? Command { get; set; }
    public bool IsExpanded { get; private set; }

    private Border? _popupBorder;

    public MenuItem()
    {
        Focusable = true;
    }

    public override int VisualChildrenCount => _header != null ? 1 : 0;

    public override UIElement GetVisualChild(int index)
    {
        if (index == 0 && _header != null) return _header;
        throw new ArgumentOutOfRangeException(nameof(index));
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        if (_header != null)
        {
            _header.Measure(availableSize);
            return _header.DesiredSize;
        }
        return new Size(0, 0);
    }

    protected override void ArrangeOverride(Size finalSize)
    {
        if (_header != null)
        {
            _header.Arrange(new Rect(0, 0, finalSize.Width, finalSize.Height));
        }
    }

    public override void Render(VirtualBuffer buffer, int offsetX, int offsetY)
    {
        int x = RenderSize.X + offsetX;
        int y = RenderSize.Y + offsetY;

        bool isActive = IsFocused || IsExpanded;
        // Turbo Pascal Style:
        // Active: Green Background, Black Text
        // Inactive: 
        //   - MenuBar: Gray Background, Black Text (inherited/default)
        //   - Popup: Gray Background, Black Text (inherited from Border)
        
        var bg = isActive ? ConsoleColor.Green : (Parent is MenuBar ? (ConsoleColor?)null : ConsoleColor.Gray);
        var fg = ConsoleColor.Black; // Always black text

        // Draw background
        for (int i = 0; i < RenderSize.Width; i++)
        {
            for (int j = 0; j < RenderSize.Height; j++)
            {
                // If it's a top-level menu item and not active, we might depend on MenuBar background, 
                // but let's be explicit if needed. 
                // If bg is null, we don't draw (transparent).
                if (bg.HasValue)
                {
                    buffer.SetPixel(x + i, y + j, ' ', fg, bg.Value);
                }
            }
        }
        
        // Update Header color if it's a TextBlock
        if (_header is TextBlock tb)
        {
            tb.Foreground = fg;
            tb.Background = bg; // Ensure text block background matches item background
        }

        if (_header != null)
        {
            _header.Render(buffer, x, y);
        }
        
        // Draw sub-menu arrow indicator if needed
        if (Items.Count > 0 && !(Parent is MenuBar)) 
        {
             buffer.SetPixel(x + RenderSize.Width - 1, y, '\u25BA', fg, bg ?? ConsoleColor.Gray); // Arrow
        }
    }

    public override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        // If mouse moves over this item, focus it.
        // This gives the "hover selection" effect.
        if (!IsFocused)
        {
            Focus();
            // If the parent has an open submenu (and it's not THIS item's submenu), 
            // implies we are switching between siblings in a menu.
            // But if we are in a MenuBar, creating "hover open" behavior usually requires click first?
            // User request: "Mouseover on items does not change their color." -> Implies highlight.
            // Focusing handles the highlight (Render checks IsFocused).
        }
    }

    public override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        Focus();
        if (Items.Count > 0)
        {
            if (IsExpanded) CloseSubMenu();
            else OpenSubMenu();
        }
        else
        {
            Command?.Invoke();
            CloseParentMenu();
        }
        e.Handled = true;
    }

    public override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        
        if (e.Key == ConsoleKey.Enter || e.Key == ConsoleKey.Spacebar)
        {
            if (Items.Count > 0)
            {
                 if (IsExpanded) CloseSubMenu();
                 else OpenSubMenu();
            }
            else
            {
                Command?.Invoke();
                CloseParentMenu();
            }
            e.Handled = true;
            return;
        }

        if (e.Key == ConsoleKey.DownArrow)
        {
            if (Parent is MenuBar)
            {
                // Open and Focus first item
                OpenSubMenu();
            }
            else
            {
                // Next Sibling
                NavigateSibling(1);
            }
            e.Handled = true;
        }
        else if (e.Key == ConsoleKey.UpArrow)
        {
            if (Parent is MenuBar)
            {
                // Maybe nothing? Or cycle?
            }
            else
            {
                 // Prev Sibling
                 NavigateSibling(-1);
            }
            e.Handled = true;
        }
        else if (e.Key == ConsoleKey.RightArrow)
        {
            if (Items.Count > 0 && !(Parent is MenuBar)) // Submenu expanad
            {
                OpenSubMenu();
            }
            else
            {
                // Next Sibling (Top Level) or Parent's Next Sibling
                // If we are in MenuBar, Right goes to next top level.
                // If we are in SubMenu, Right *could* go to next top level if we want "cross-menu" navigation 
                // but usually Right on a leaf does nothing or goes to next column.
                // Simplified: If MenuBar, go next.
                if (Parent is MenuBar)
                {
                    NavigateSibling(1);
                }
            }
            e.Handled = true;
        }
        else if (e.Key == ConsoleKey.LeftArrow)
        {
             if (Parent is MenuBar)
             {
                 NavigateSibling(-1);
             }
             else
             {
                 // Close submenu (back to parent)
                 // We need to find our parent MenuItem.
                 // Parent is StackPanel (popup), its Parent is Border, Border doesn't know MenuItem.
                 // We relied on `GetRoot()?.ClearOverlay()` which closes ALL. 
                 // To navigate back one level, we need reference to specific parent MenuItem.
                 // We don't have it easily.
                 // HACK: Close all for now? No, specifically LEFT should close THIS level.
                 // But `CloseParentMenu` closes everything.
                 // `CloseSubMenu` is on the PARENT. 
                 // We are the CHILD. We want our PARENT to close its submenu.
                 // But we don't have ref to Parent MenuItem.
                 // Limitations of current structure.
                 // For now, let's just allow MenuBar navigation.
             }
             e.Handled = true;
        }
    }

    private void NavigateSibling(int offset)
    {
        if (Parent is StackPanel stack)
        {
            int index = stack.Children.IndexOf(this);
            if (index >= 0)
            {
                int next = index + offset;
                if (next >= 0 && next < stack.Children.Count)
                {
                    var sibling = stack.Children[next];
                    if (sibling.Focusable) sibling.Focus();
                }
            }
        }
    }

    public void OpenSubMenu()
    {
        if (IsExpanded) return;
        if (Items.Count == 0) return;

        var root = GetRoot() as TuiWindow;
        if (root == null) return;

        if (Parent is StackPanel parentStack)
        {
            foreach (var child in parentStack.Children)
            {
                if (child is MenuItem mi && mi != this && mi.IsExpanded)
                {
                    mi.CloseSubMenu();
                }
            }
        }

        IsExpanded = true;

        // Create popup container
        var stackPanel = new StackPanel { Orientation = Orientation.Vertical };
        foreach (var item in Items)
        {
            stackPanel.AddChild(item);
        }

        _popupBorder = new MenuPopupBorder
        {
            Child = stackPanel,
            BorderColor = ConsoleColor.Black,
            Background = ConsoleColor.Gray,
            BoxStyle = BoxStyle.Single,
            Owner = this
        };

        // Needs measurement to know size
        // We probably need to measure it first. 
        // In simple case, let's guess or measure with large constraints.
        _popupBorder.Measure(new Size(1000, 1000));
        
        int absX = RenderSize.X;
        int absY = RenderSize.Y;

        // Calculate absolute position
        var current = Parent;
        while (current != null && current != root)
        {
            absX += current.RenderSize.X;
            absY += current.RenderSize.Y;
            current = current.Parent;
        }

        // Position logic
        int popupX, popupY;
        if (Parent is MenuBar)
        {
            // Drop down
            popupX = absX;
            popupY = absY + RenderSize.Height;
        }
        else
        {
            // Side menu
            popupX = absX + RenderSize.Width;
            popupY = absY;
        }

        _popupBorder.Width = (int)_popupBorder.DesiredSize.Width;
        _popupBorder.Height = (int)_popupBorder.DesiredSize.Height;
        
        _popupBorder.Arrange(new Rect(popupX, popupY, _popupBorder.Width, _popupBorder.Height));

        root.PushOverlay(_popupBorder);
        // Focus first item?
        if (Items.Count > 0)
        {
             // We want to focus the first FOCUSABLE item.
             foreach(var item in Items)
             {
                 if (item.Focusable)
                 {
                     root.SetFocus(item);
                     break;
                 }
             }
        }
        Invalidate();
    }

    public void CloseSubMenu()
    {
        if (!IsExpanded) return;

        // Recursively close child submenus
        foreach (var item in Items)
        {
            if (item is MenuItem mi)
            {
                mi.CloseSubMenu();
            }
        }

        IsExpanded = false;

        var root = GetRoot() as TuiWindow;
        if (root != null && _popupBorder != null)
        {
             root.RemoveOverlay(_popupBorder);
        }
        
        _popupBorder = null;
        Invalidate();
    }

    private void CloseParentMenu()
    {
        // Walk up to find the root of the menu chain and close submenus
        var current = Parent;
        while (current != null)
        {
            if (current is MenuPopupBorder mpb)
            {
                var owner = mpb.Owner;
                owner.CloseSubMenu();
                current = owner.Parent;
            }
            else if (current is MenuBar)
            {
                 // Top level reached.
                 break;
            }
            else
            {
                current = current.Parent;
            }
        }
    }

    private class MenuPopupBorder : Border
    {
        public MenuItem Owner { get; set; }
    }
}
