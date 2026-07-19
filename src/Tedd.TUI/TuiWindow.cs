using System.Collections.Generic;
using System.Collections;
using System;

namespace Tedd.TUI;

public class TuiWindow : UIElement
{
    public TuiWindow()
    {
        // Weakly tracked so a global theme swap can refresh this window's tree.
        ThemeManager.RegisterWindow(this);
    }

    /// <summary>
    /// Called by <see cref="ThemeManager"/> when the global theme is replaced:
    /// refreshes cached themed state throughout the tree and schedules a re-render.
    /// </summary>
    internal void OnGlobalThemeChanged()
    {
        NotifyThemeChanged();
        Invalidate();
    }

    /// <summary>
    /// Capabilities of the surface this window is rendered on. Controls call
    /// <see cref="UIElement.GetCapabilities"/> to read this. Defaults to
    /// <see cref="SurfaceCapabilities.TextOnly"/>; renderers should set their own profile
    /// (e.g. <see cref="SurfaceCapabilities.SupportsGraphics"/> = true for HTML surfaces).
    /// </summary>
    public SurfaceCapabilities Capabilities { get; set; } = SurfaceCapabilities.TextOnly;

    public UIElement Content
    {
        get => field;
        set
        {
            field = value;
            if (field != null) field.Parent = this;
        }
    }

    public override int VisualChildrenCount => (Content != null ? 1 : 0) + _overlays.Count;

    public override UIElement GetVisualChild(int index)
    {
        int contentCount = Content != null ? 1 : 0;
        if (index < contentCount)
            return Content!;

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

        // Overlays are absolutely positioned by their creators (DialogBox.Show, menu /
        // combobox popups). The window re-fits them on every layout pass so a terminal
        // resize can't strand them: dialogs re-center (mirroring DialogBox.Show),
        // everything else keeps its position but is clamped into the new bounds.
        for (int i = 0; i < _overlays.Count; i++)
        {
            var overlay = _overlays[i];

            if (overlay is DialogBox)
            {
                overlay.Measure(new Size(finalSize.Width, finalSize.Height));
                int w = Math.Min(overlay.DesiredSize.Width, finalSize.Width);
                int h = Math.Min(overlay.DesiredSize.Height, finalSize.Height);
                int x = Math.Max(0, (finalSize.Width - w) / 2);
                int y = Math.Max(0, (finalSize.Height - h) / 2);
                overlay.Arrange(new Rect(x, y, w, h));
            }
            else
            {
                var r = overlay.RenderSize;
                if (r.Width <= 0 || r.Height <= 0) continue; // not arranged by its creator yet

                int w = Math.Min(r.Width, finalSize.Width);
                int h = Math.Min(r.Height, finalSize.Height);
                int x = Math.Clamp(r.X, 0, Math.Max(0, finalSize.Width - w));
                int y = Math.Clamp(r.Y, 0, Math.Max(0, finalSize.Height - h));

                if (x != r.X || y != r.Y || w != r.Width || h != r.Height)
                {
                    overlay.Arrange(new Rect(x, y, w, h));
                }
            }
        }
    }

    /// <summary>
    /// Composites the window content and any pushed overlays into <paramref name="buffer"/>.
    /// Each overlay is rendered through the layered pipeline so transparent shadows or
    /// translucent dialogs blend correctly against whatever lies below them.
    /// </summary>
    public override void Render(VirtualBuffer buffer, int offsetX, int offsetY)
    {
        // Themed desktops (Turbo Pascal / QuickBasic blue, Light gray, ...) set the
        // window Background; fill the surface first so content and empty areas sit on
        // it. Unthemed windows keep the historical behavior of not painting anything.
        var windowBackground = Background;
        if (windowBackground.HasValue)
        {
            int w = RenderSize.Width > 0 ? RenderSize.Width : buffer.Width;
            int h = RenderSize.Height > 0 ? RenderSize.Height : buffer.Height;
            buffer.FillRect(RenderSize.X + offsetX, RenderSize.Y + offsetY, w, h, ' ', Foreground, windowBackground.Value);
        }

        // Base content lives directly on the destination buffer to avoid an extra allocation.
        Content?.Render(buffer, offsetX, offsetY);

        if (_overlays.Count == 0) return;

        // Each overlay gets its own transparent layer buffer; the compositor blends them
        // back onto the destination so per-cell alpha (button shadows, semi-opaque dialog
        // backgrounds, …) survives.
        var layers = AcquireOverlayLayers(buffer.Width, buffer.Height);
        try
        {
            for (int i = 0; i < _overlays.Count; i++)
            {
                var overlay = _overlays[i];
                if (overlay == null || !overlay.Visibility) continue;

                var layer = layers[i];
                layer.Buffer.Clear(TuiColor.Transparent);
                overlay.Render(layer.Buffer, offsetX, offsetY);
            }

            LayerCompositor.Flatten(layers, buffer);
        }
        finally
        {
            ReleaseOverlayLayers(layers);
        }
    }

    private RenderLayer[] _overlayLayerPool = Array.Empty<RenderLayer>();
    private int _overlayLayerPoolWidth;
    private int _overlayLayerPoolHeight;

    private RenderLayer[] AcquireOverlayLayers(int width, int height)
    {
        int count = _overlays.Count;

        // Re-allocate when the surface size changes; the pool is per-window so this is rare.
        if (_overlayLayerPoolWidth != width || _overlayLayerPoolHeight != height)
        {
            _overlayLayerPool = Array.Empty<RenderLayer>();
            _overlayLayerPoolWidth = width;
            _overlayLayerPoolHeight = height;
        }

        if (_overlayLayerPool.Length < count)
        {
            var grown = new RenderLayer[count];
            Array.Copy(_overlayLayerPool, grown, _overlayLayerPool.Length);
            for (int i = _overlayLayerPool.Length; i < count; i++)
            {
                grown[i] = new RenderLayer(width, height, 1000 + i);
            }
            _overlayLayerPool = grown;
        }

        // Build a fresh array sized to the current overlay count so the compositor only
        // touches active layers (and so the array we hand it matches IReadOnlyList semantics).
        var slice = new RenderLayer[count];
        for (int i = 0; i < count; i++)
        {
            slice[i] = _overlayLayerPool[i];
            slice[i].OffsetX = 0;
            slice[i].OffsetY = 0;
            slice[i].Opacity = 1f;
            slice[i].IsVisible = true;
        }
        return slice;
    }

    private static void ReleaseOverlayLayers(RenderLayer[] _) { /* layers stay pooled */ }

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

    private UIElement _focusedElement;

    public override void OnPreviewMouseDown(MouseEventArgs e)
    {
        base.OnPreviewMouseDown(e);

        var hit = InputHitTest(e.GlobalX, e.GlobalY);
        if (hit != null)
        {
            // Bug: Menus and ComboBox dropdowns do not close when clicking outside them.
            // Root cause: No automatic click-outside detection exists for active overlays.
            // Fix: Detect clicks outside active menu/combobox popups and close them.
            // Regression: Verified by checking if hit element is within the active popup hierarchies.
            CloseMenusIfClickOutside(hit.Element);
            CloseComboBoxesIfClickOutside(hit.Element);
            CloseDatePickersIfClickOutside(hit.Element);
        }
    }

    private void CloseMenusIfClickOutside(UIElement? clickedElement)
    {
        bool hasMenuOverlay = false;
        for (int i = 0; i < _overlays.Count; i++)
        {
            if (_overlays[i] is MenuItem.MenuPopupBorder)
            {
                hasMenuOverlay = true;
                break;
            }
        }

        if (hasMenuOverlay)
        {
            bool insideMenu = false;
            var current = clickedElement;
            while (current != null)
            {
                if (current is MenuItem.MenuPopupBorder || current is MenuItem || current is MenuBar)
                {
                    insideMenu = true;
                    break;
                }
                current = current.Parent;
            }

            if (!insideMenu)
            {
                var popupBorders = new List<MenuItem.MenuPopupBorder>();
                for (int i = 0; i < _overlays.Count; i++)
                {
                    if (_overlays[i] is MenuItem.MenuPopupBorder mpb)
                    {
                        popupBorders.Add(mpb);
                    }
                }

                foreach (var mpb in popupBorders)
                {
                    mpb.Owner.CloseSubMenu();
                }
            }
        }
    }

    private void CloseComboBoxesIfClickOutside(UIElement? clickedElement)
    {
        var cbPopups = new List<ComboBox.ComboBoxPopupBorder>();
        for (int i = 0; i < _overlays.Count; i++)
        {
            if (_overlays[i] is ComboBox.ComboBoxPopupBorder cbp)
            {
                cbPopups.Add(cbp);
            }
        }

        foreach (var cbp in cbPopups)
        {
            bool insideComboBox = false;
            var current = clickedElement;
            while (current != null)
            {
                if (current == cbp || current == cbp.Owner)
                {
                    insideComboBox = true;
                    break;
                }
                current = current.Parent;
            }

            if (!insideComboBox)
            {
                cbp.Owner.CloseDropdown(restoreFocus: false);
            }
        }
    }

    private void CloseDatePickersIfClickOutside(UIElement? clickedElement)
    {
        var dpPopups = new List<DatePicker.DatePickerPopupBorder>();
        for (int i = 0; i < _overlays.Count; i++)
        {
            if (_overlays[i] is DatePicker.DatePickerPopupBorder dpb)
            {
                dpPopups.Add(dpb);
            }
        }

        foreach (var dpb in dpPopups)
        {
            bool insideDatePicker = false;
            var current = clickedElement;
            while (current != null)
            {
                if (current == dpb || current == dpb.Owner)
                {
                    insideDatePicker = true;
                    break;
                }
                current = current.Parent;
            }

            if (!insideDatePicker)
            {
                dpb.Owner.CloseDropdown(restoreFocus: false);
            }
        }
    }

    private void CloseMenusIfFocusLost(UIElement? newFocus)
    {
        CloseMenusIfClickOutside(newFocus);
    }

    private void CloseComboBoxesIfFocusLost(UIElement? newFocus)
    {
        CloseComboBoxesIfClickOutside(newFocus);
    }

    public bool SetFocus(UIElement element)
    {
        if (element == _focusedElement) return true;

        // Bug: Menus and ComboBox dropdowns do not close when focus changes (e.g. via Tab).
        // Root cause: No focus-lost detection exists to close the active overlays.
        // Fix: Check if focus is moving outside the menu or combobox dropdown bounds, and auto-close them.
        CloseMenusIfFocusLost(element);
        CloseComboBoxesIfFocusLost(element);
        CloseDatePickersIfClickOutside(element);

        _focusedElement?.OnLostFocus();
        _focusedElement = element;
        _focusedElement?.OnGotFocus();
        return true;
    }

    private UIElement _capturedElement;
    public UIElement? CapturedElement => _capturedElement;

    // Elements currently under the mouse, deepest first (hit element .. root).
    // The full chain is stored (rather than just the deepest element) so IsMouseOver
    // can be cleared on every ancestor even if part of the old chain is detached
    // from the tree in the meantime (e.g. an overlay closing).
    private readonly List<UIElement> _hoverChain = new();

    /// <summary>The deepest element currently under the mouse, or null.</summary>
    public UIElement? HoveredElement => _hoverChain.Count > 0 ? _hoverChain[0] : null;

    /// <summary>
    /// Maintains IsMouseOver along the ancestor chain of the element under the mouse and
    /// raises MouseLeave/MouseEnter for elements that left or joined the chain. Driven by
    /// <see cref="ProcessMouse"/>, so hover state only updates on platforms that report
    /// mouse events.
    /// </summary>
    private void UpdateMouseOver(UIElement? target, int globalX, int globalY)
    {
        bool unchanged = target == null
            ? _hoverChain.Count == 0
            : _hoverChain.Count > 0 && ReferenceEquals(_hoverChain[0], target);
        if (unchanged)
            return;

        var newChain = new List<UIElement>();
        for (var current = target; current != null; current = current.Parent)
            newChain.Add(current);

        // Leave elements no longer under the mouse, deepest first.
        foreach (var element in _hoverChain)
        {
            if (!newChain.Contains(element))
            {
                element.IsMouseOver = false;
                element.RaiseEvent(new MouseEventArgs(UIElement.MouseLeaveEvent, element)
                {
                    GlobalX = globalX,
                    GlobalY = globalY
                });
            }
        }

        // Enter newly hovered elements, outermost first.
        for (int i = newChain.Count - 1; i >= 0; i--)
        {
            var element = newChain[i];
            if (!_hoverChain.Contains(element))
            {
                element.IsMouseOver = true;
                element.RaiseEvent(new MouseEventArgs(UIElement.MouseEnterEvent, element)
                {
                    GlobalX = globalX,
                    GlobalY = globalY
                });
            }
        }

        _hoverChain.Clear();
        _hoverChain.AddRange(newChain);
    }

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
        // PointToScreen applies the ScrollViewer content translation, so captured-mouse
        // coordinates stay correct for elements inside scrolled content.
        return element.PointToScreen(new Point(0, 0));
    }

    private HitTestResult InputHitTestRecursive(UIElement element, int x, int y)
    {
        if (!element.Visibility || !element.IsEnabled) return null;

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

                // For a ScrollViewer, the Content child is rendered with a translation
                // of (-HorizontalOffset, -VerticalOffset). Hit testing must apply the
                // same translation when descending into Content so coordinates match
                // what the user sees. Scrollbars / title / status bar are NOT translated.
                int childX = localX;
                int childY = localY;
                if (element is ScrollViewer sv && ReferenceEquals(child, sv.Content))
                {
                    childX += sv.HorizontalOffset;
                    childY += sv.VerticalOffset;
                }

                hitChild = InputHitTestRecursive(child, childX, childY);
                if (hitChild != null) return hitChild;
            }

            // If no child hit, but we are inside, return self with local coordinates
            return new HitTestResult(element, localX, localY);
        }

        return null;
    }

    /// <summary>
    /// Routes a mouse event through hit testing, focus acquisition, preview tunneling,
    /// and bubbling. Platform input managers should translate native coordinates and
    /// delegate here so all front ends share the same interaction semantics.
    /// </summary>
    public void ProcessMouse(MouseEventArgs e)
    {
        ArgumentNullException.ThrowIfNull(e);

        RoutedEvent? previewEvent = null;
        if (e.RoutedEvent == UIElement.MouseDownEvent)
            previewEvent = UIElement.PreviewMouseDownEvent;
        else if (e.RoutedEvent == UIElement.MouseUpEvent)
            previewEvent = UIElement.PreviewMouseUpEvent;
        else if (e.RoutedEvent == UIElement.MouseMoveEvent)
            previewEvent = UIElement.PreviewMouseMoveEvent;
        else if (e.RoutedEvent == UIElement.MouseWheelEvent)
            previewEvent = UIElement.PreviewMouseWheelEvent;
        else
            throw new ArgumentException("The routed event must be a mouse down, up, move, or wheel event.", nameof(e));

        var hit = InputHitTest(e.GlobalX, e.GlobalY);
        UpdateMouseOver(hit?.Element, e.GlobalX, e.GlobalY);
        if (hit == null)
            return;

        if (e.RoutedEvent == UIElement.MouseDownEvent)
        {
            UIElement? focusTarget = hit.Element;
            while (focusTarget != null && focusTarget != this)
            {
                if (CanFocus(focusTarget))
                {
                    SetFocus(focusTarget);
                    break;
                }

                focusTarget = focusTarget.Parent;
            }
        }

        MouseEventArgs previewArgs = e is MouseWheelEventArgs wheelArgs
            ? new MouseWheelEventArgs(previewEvent, hit.Element) { Delta = wheelArgs.Delta }
            : new MouseEventArgs(previewEvent, hit.Element);
        previewArgs.GlobalX = e.GlobalX;
        previewArgs.GlobalY = e.GlobalY;
        previewArgs.GlobalXF = e.GlobalXF;
        previewArgs.GlobalYF = e.GlobalYF;

        hit.Element.RaiseEvent(previewArgs);
        if (previewArgs.Handled)
        {
            e.Handled = true;
            return;
        }

        hit.Element.RaiseEvent(e);
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
            && tc.Items[tc.SelectedIndex] is TabItem ti && ti.Content is UIElement tabContent)
            FocusFirstIn(tabContent);
        else
            FocusFirstIn(Content);
    }

    public void ProcessKey(KeyEventArgs e)
    {
        if (_focusedElement == null) return;

        // Two-phase event dispatch for WPF parity: Tunneling (Preview) then Bubbling
        RoutedEvent previewEvent = null;
        if (e.RoutedEvent == UIElement.KeyDownEvent) previewEvent = UIElement.PreviewKeyDownEvent;
        else if (e.RoutedEvent == UIElement.KeyUpEvent) previewEvent = UIElement.PreviewKeyUpEvent;

        if (previewEvent != null)
        {
            var previewArgs = new KeyEventArgs(previewEvent, e.Source ?? _focusedElement)
            {
                Key = e.Key,
                KeyChar = e.KeyChar,
                Modifiers = e.Modifiers
            };

            _focusedElement.RaiseEvent(previewArgs);

            if (previewArgs.Handled)
            {
                e.Handled = true;
                return;
            }
        }

        _focusedElement.RaiseEvent(e);

        // Bug: Tab navigation runs twice on Windows console (moving focus away immediately).
        // Root cause: ProcessKey is called for both KeyDown and KeyUp events, but does not check the event type before tabbing.
        // Fix: Restrict Tab navigation to KeyDown events only.
        // Regression: Handled by checking e.RoutedEvent == UIElement.KeyDownEvent.
        if (!e.Handled && e.RoutedEvent == UIElement.KeyDownEvent && e.Key == System.ConsoleKey.Tab)
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
        if (!element.Focusable) return false;

        // Descendants of a hidden or disabled container are not interactive even
        // when their own local flags remain true.
        UIElement? current = element;
        while (current != null)
        {
            if (!current.IsEnabled || !current.Visibility)
                return false;

            current = current.Parent;
        }

        return true;
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

    internal VisualTreeEnumerable GetVisualTree(UIElement root)
    {
        return new VisualTreeEnumerable(root);
    }

    internal readonly struct VisualTreeEnumerable : IEnumerable<UIElement>
    {
        private readonly UIElement _root;
        public VisualTreeEnumerable(UIElement root) => _root = root;
        public VisualTreeEnumerator GetEnumerator() => new VisualTreeEnumerator(_root);

        IEnumerator<UIElement> IEnumerable<UIElement>.GetEnumerator() => GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    internal struct VisualTreeEnumerator : IEnumerator<UIElement>
    {
        private PooledStack<(UIElement element, bool secondPass)> _stack;
        private UIElement _current;

        public VisualTreeEnumerator(UIElement root)
        {
            _stack = new PooledStack<(UIElement element, bool secondPass)>(16);
            if (root != null)
                _stack.Push((root, false));
            _current = default!;
        }

        public UIElement Current => _current;
        object IEnumerator.Current => _current;

        public void Dispose()
        {
            _stack.Dispose();
        }

        public void Reset() => throw new NotSupportedException();

        public bool MoveNext()
        {
            if (_stack.Count == 0) return false;

            var (current, secondPass) = _stack.Pop();
            _current = current;

            if (!secondPass)
            {
                if (current is TabControl tab)
                {
                    // Second yield for tab strip
                    _stack.Push((current, true));
                    // Content
                    if (tab.SelectedIndex >= 0 && tab.SelectedIndex < tab.Items.Count)
                    {
                        var item = tab.Items[tab.SelectedIndex];
                        // Need to check if item is TabItem or just UIElement content
                        UIElement? content = null;
                        if (item is TabItem ti) content = ti.Content as UIElement;
                        else content = item as UIElement;

                        if (content != null) _stack.Push((content, false));
                    }
                }
                else
                {
                    // Normal children in reverse order
                    for (int i = current.VisualChildrenCount - 1; i >= 0; i--)
                    {
                        _stack.Push((current.GetVisualChild(i), false));
                    }
                }
            }
            return true;
        }
    }

    private struct PooledStack<T> : IDisposable
    {
        private T[] _array;
        private int _count;

        public PooledStack(int capacity)
        {
            _array = System.Buffers.ArrayPool<T>.Shared.Rent(capacity);
            _count = 0;
        }

        public int Count => _count;

        public void Push(T item)
        {
            if (_array == null)
            {
                _array = System.Buffers.ArrayPool<T>.Shared.Rent(4);
            }

            if (_count == _array.Length)
            {
                var newArray = System.Buffers.ArrayPool<T>.Shared.Rent(_array.Length * 2);
                Array.Copy(_array, newArray, _count);
                System.Buffers.ArrayPool<T>.Shared.Return(_array, clearArray: true);
                _array = newArray;
            }
            _array[_count++] = item;
        }

        public T Pop()
        {
            if (_count == 0) throw new InvalidOperationException("Stack is empty");
            var item = _array[--_count];
            _array[_count] = default!; // Clear reference
            return item;
        }

        public void Dispose()
        {
            if (_array != null)
            {
                System.Buffers.ArrayPool<T>.Shared.Return(_array, clearArray: true);
                _array = null!;
                _count = 0;
            }
        }
    }

    public event EventHandler VisualChanged;
    public override void Invalidate()
    {
        VisualChanged?.Invoke(this, EventArgs.Empty);
    }
}
