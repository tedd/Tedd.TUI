using System.Collections.Generic;

namespace Tedd.TUI;

public enum HorizontalAlignment
{
    Left,
    Center,
    Right,
    Stretch
}

public enum VerticalAlignment
{
    Top,
    Center,
    Bottom,
    Stretch
}

public abstract class UIElement : DependencyObject
{
    public string Name { get; set; }

    // TemplatedParent for TemplateBinding
    public DependencyObject TemplatedParent
    {
        get => field;
        internal set
        {
            if (field != value)
            {
                field = value;
                OnTemplatedParentChanged();
            }
        }
    }

    protected virtual void OnTemplatedParentChanged()
    {
        // Update bindings that might rely on TemplatedParent
        foreach (var binding in _bindings)
        {
            binding.UpdateTarget();
        }
    }

    public UIElement Parent
    {
        get => field;
        internal set
        {
            if (field != value)
            {
                field = value;
                OnParentChanged();
            }
        }
    }
    protected override DependencyObject InheritanceParent => Parent;

    protected virtual void OnParentChanged()
    {
        // Notify that inherited DataContext might have changed
        OnPropertyChanged(DataContextProperty);
    }

    public virtual UIElement FindName(string name)
    {
        if (this.Name == name) return this;
        int count = VisualChildrenCount;
        for (int i = 0; i < count; i++)
        {
            var child = GetVisualChild(i);
            var found = child?.FindName(name);
            if (found != null) return found;
        }
        return null;
    }

    public static readonly DependencyProperty BackgroundProperty =
        DependencyProperty.Register(nameof(Background), typeof(ConsoleColor?), typeof(UIElement), null);

    public ConsoleColor? Background
    {
        get => (ConsoleColor?)GetValue(BackgroundProperty);
        set => SetValue(BackgroundProperty, value);
    }

    public static readonly DependencyProperty ForegroundProperty =
        DependencyProperty.Register(nameof(Foreground), typeof(ConsoleColor), typeof(UIElement), ConsoleColor.White, isInherited: true);

    public ConsoleColor Foreground
    {
        get => (ConsoleColor)GetValue(ForegroundProperty);
        set => SetValue(ForegroundProperty, value);
    }

    public static readonly DependencyProperty IsFocusedProperty =
        DependencyProperty.Register(nameof(IsFocused), typeof(bool), typeof(UIElement), false);

    public bool IsFocused
    {
        get => (bool)GetValue(IsFocusedProperty);
        set => SetValue(IsFocusedProperty, value);
    }

    public static readonly DependencyProperty IsEnabledProperty =
        DependencyProperty.Register(nameof(IsEnabled), typeof(bool), typeof(UIElement), true);

    public bool IsEnabled
    {
        get => (bool)GetValue(IsEnabledProperty);
        set => SetValue(IsEnabledProperty, value);
    }

    public static readonly DependencyProperty VisibilityProperty =
        DependencyProperty.Register(nameof(Visibility), typeof(bool), typeof(UIElement), true);

    public bool Visibility
    {
        get => (bool)GetValue(VisibilityProperty);
        set => SetValue(VisibilityProperty, value);
    }

    public static readonly DependencyProperty FocusableProperty =
        DependencyProperty.Register(nameof(Focusable), typeof(bool), typeof(UIElement), false);

    public bool Focusable
    {
        get => (bool)GetValue(FocusableProperty);
        set => SetValue(FocusableProperty, value);
    }

    public static readonly DependencyProperty MarginProperty =
        DependencyProperty.Register(nameof(Margin), typeof(Thickness), typeof(UIElement), new Thickness(0));

    public Thickness Margin
    {
        get => (Thickness)GetValue(MarginProperty);
        set => SetValue(MarginProperty, value);
    }

    public static readonly DependencyProperty WidthProperty =
        DependencyProperty.Register(nameof(Width), typeof(int), typeof(UIElement), -1); // -1 for Auto

    public int Width
    {
        get => (int)GetValue(WidthProperty);
        set => SetValue(WidthProperty, value);
    }

    public static readonly DependencyProperty HeightProperty =
        DependencyProperty.Register(nameof(Height), typeof(int), typeof(UIElement), -1); // -1 for Auto

    public int Height
    {
        get => (int)GetValue(HeightProperty);
        set => SetValue(HeightProperty, value);
    }

    public static readonly DependencyProperty HorizontalAlignmentProperty =
        DependencyProperty.Register(nameof(HorizontalAlignment), typeof(HorizontalAlignment), typeof(UIElement), HorizontalAlignment.Stretch);

    public HorizontalAlignment HorizontalAlignment
    {
        get => (HorizontalAlignment)GetValue(HorizontalAlignmentProperty);
        set => SetValue(HorizontalAlignmentProperty, value);
    }

    public static readonly DependencyProperty VerticalAlignmentProperty =
        DependencyProperty.Register(nameof(VerticalAlignment), typeof(VerticalAlignment), typeof(UIElement), VerticalAlignment.Stretch);

    public VerticalAlignment VerticalAlignment
    {
        get => (VerticalAlignment)GetValue(VerticalAlignmentProperty);
        set => SetValue(VerticalAlignmentProperty, value);
    }

    public static readonly DependencyProperty DataContextProperty =
        DependencyProperty.Register(nameof(DataContext), typeof(object), typeof(UIElement), null, isInherited: true);

    public object DataContext
    {
        get => GetValue(DataContextProperty);
        set
        {
            SetValue(DataContextProperty, value);
            // Notify bindings? For now, we rely on SetBinding to trigger initial update 
            // or property change notification.
            // In a real system, changing DataContext should propagate down and update all bindings.
            // We'll implement propagation roughly via OnPropertyChanged override if needed.
        }
    }

    private readonly List<BindingExpression> _bindings = [];

    public void SetBinding(DependencyProperty dp, Binding binding)
    {
        var expr = new BindingExpression(this, dp, binding);
        _bindings.Add(expr);
        expr.UpdateTarget();
    }

    protected override void OnPropertyChanged(DependencyProperty dp)
    {
        base.OnPropertyChanged(dp);

        if (dp == Panel.ZIndexProperty && Parent is Panel p)
        {
            p.InvalidateZState();
        }
        if (dp == DataContextProperty)
        {
            // Update local bindings
            foreach (var binding in _bindings)
            {
                binding.UpdateTarget();
            }

            OnDataContextChanged(this.DataContext);
        }

        if (dp.IsInherited)
        {
            int count = VisualChildrenCount;
            for (int i = 0; i < count; i++)
            {
                var child = GetVisualChild(i);
                if (child != null && !child.HasLocalValue(dp))
                {
                    child.OnPropertyChanged(dp);
                }
            }
        }
        Invalidate();
    }

    protected virtual void OnDataContextChanged(object newValue)
    {
        // Called automatically when DataContext changes; override in derived classes to respond to DataContext changes.
    }

    public virtual int VisualChildrenCount => 0;

    public virtual UIElement GetVisualChild(int index)
    {
        throw new System.ArgumentOutOfRangeException(nameof(index));
    }

    // Layout System
    public Size DesiredSize { get; private set; }
    public Rect RenderSize { get; private set; } // The actual size and position relative to parent? Or just Size? WPF has RenderSize as Size.

    // WPF uses ArrangeRect for the final position inside parent.
    // We will store actual position maybe?
    // Let's stick to standard Measure/Arrange pattern.

    public void Measure(Size availableSize)
    {
        if (!Visibility)
        {
            DesiredSize = new Size(0, 0);
            return;
        }

        Thickness margin = Margin;
        int marginWidth = margin.Left + margin.Right;
        int marginHeight = margin.Top + margin.Bottom;

        Size innerAvailableSize = new Size(
            System.Math.Max(0, availableSize.Width - marginWidth),
            System.Math.Max(0, availableSize.Height - marginHeight)
        );

        Size desired = MeasureOverride(innerAvailableSize);

        // Respect Width/Height properties
        int width = Width;
        int height = Height;

        if (width >= 0) desired.Width = width;
        if (height >= 0) desired.Height = height;

        desired.Width += marginWidth;
        desired.Height += marginHeight;

        // Clip to available size? Usually not in Measure, but we return what we want.
        // But we should probably not ask for more than available if we can help it?
        // WPF Measure: "A parent element calls this method to form a recursive layout update."

        DesiredSize = desired;
    }

    protected virtual Size MeasureOverride(Size availableSize)
    {
        return new Size(0, 0);
    }

    public void Arrange(Rect finalRect)
    {
        if (!Visibility) return;

        Thickness margin = Margin;
        int marginWidth = margin.Left + margin.Right;
        int marginHeight = margin.Top + margin.Bottom;

        // The size available for alignment and rendering is reduced by margins
        int width = System.Math.Max(0, finalRect.Width - marginWidth);
        int height = System.Math.Max(0, finalRect.Height - marginHeight);

        // The desired size of the core element (without margins)
        Size desiredCore = new Size(
            System.Math.Max(0, DesiredSize.Width - marginWidth),
            System.Math.Max(0, DesiredSize.Height - marginHeight)
        );

        int x = finalRect.X + margin.Left;
        int y = finalRect.Y + margin.Top;

        // Horizontal Alignment
        if (HorizontalAlignment == HorizontalAlignment.Left)
        {
            width = desiredCore.Width;
        }
        else if (HorizontalAlignment == HorizontalAlignment.Right)
        {
            x += width - desiredCore.Width;
            width = desiredCore.Width;
        }
        else if (HorizontalAlignment == HorizontalAlignment.Center)
        {
            x += (width - desiredCore.Width) / 2;
            width = desiredCore.Width;
        }
        // Stretch takes full width (already set)

        // Vertical Alignment
        if (VerticalAlignment == VerticalAlignment.Top)
        {
            height = desiredCore.Height;
        }
        else if (VerticalAlignment == VerticalAlignment.Bottom)
        {
            y += height - desiredCore.Height;
            height = desiredCore.Height;
        }
        else if (VerticalAlignment == VerticalAlignment.Center)
        {
            y += (height - desiredCore.Height) / 2;
            height = desiredCore.Height;
        }

        if (width < 0) width = 0;
        if (height < 0) height = 0;

        Rect arrangedRect = new Rect(x, y, width, height);
        RenderSize = arrangedRect;

        ArrangeOverride(new Size(width, height));
    }

    protected virtual void ArrangeOverride(Size finalSize)
    {
        // Default implementation does nothing (for leaf nodes)
    }

    public virtual void Render(VirtualBuffer buffer)
    {
        Render(buffer, 0, 0);
    }

    public virtual void Render(VirtualBuffer buffer, int offsetX, int offsetY)
    {
        // Default implementation does nothing
    }

    // Input & Event System

    private Dictionary<RoutedEvent, List<RoutedEventHandlerInfo>> _eventHandlers;

    private struct RoutedEventHandlerInfo
    {
        public Delegate Handler;
        public bool HandledEventsToo;

        public RoutedEventHandlerInfo(Delegate handler, bool handledEventsToo)
        {
            Handler = handler;
            HandledEventsToo = handledEventsToo;
        }
    }

    public void AddHandler(RoutedEvent routedEvent, Delegate handler, bool handledEventsToo = false)
    {
        if (routedEvent == null) throw new ArgumentNullException(nameof(routedEvent));
        if (handler == null) throw new ArgumentNullException(nameof(handler));

        if (_eventHandlers == null)
            _eventHandlers = new Dictionary<RoutedEvent, List<RoutedEventHandlerInfo>>();

        if (!_eventHandlers.TryGetValue(routedEvent, out var handlers))
        {
            handlers = new List<RoutedEventHandlerInfo>();
            _eventHandlers[routedEvent] = handlers;
        }

        handlers.Add(new RoutedEventHandlerInfo(handler, handledEventsToo));
    }

    public void RemoveHandler(RoutedEvent routedEvent, Delegate handler)
    {
        if (routedEvent == null || handler == null || _eventHandlers == null) return;

        if (_eventHandlers.TryGetValue(routedEvent, out var handlers))
        {
            for (int i = 0; i < handlers.Count; i++)
            {
                if (handlers[i].Handler == handler)
                {
                    handlers.RemoveAt(i);
                    break;
                }
            }
        }
    }

    public void RaiseEvent(RoutedEventArgs e)
    {
        if (e == null) throw new ArgumentNullException(nameof(e));

        e.Source = this;
        if (e.OriginalSource == null) e.OriginalSource = this;

        // Optimization: Zero-allocation Route Building
        // Time Complexity: O(h) where h is the depth of the visual tree from this node to root.
        // Space Complexity: O(1) allocation overhead utilizing System.Buffers.ArrayPool.
        // Calculate depth
        int depth = 0;
        var current = this;
        while (current != null)
        {
            depth++;
            current = current.Parent;
        }

        // Rent array
        UIElement[] array = System.Buffers.ArrayPool<UIElement>.Shared.Rent(depth);
        try
        {
            // Populate array
            current = this;
            int idx = 0;
            while (current != null)
            {
                array[idx++] = current;
                current = current.Parent;
            }

            var route = array.AsSpan(0, depth);

            // Tunnel Phase (Root -> Source)
            if (e.RoutedEvent.RoutingStrategy == RoutingStrategy.Tunnel)
            {
                for (int i = route.Length - 1; i >= 0; i--)
                {
                    route[i].InvokeHandler(e);
                }
            }
            // Bubble Phase (Source -> Root)
            else if (e.RoutedEvent.RoutingStrategy == RoutingStrategy.Bubble)
            {
                for (int i = 0; i < route.Length; i++)
                {
                    route[i].InvokeHandler(e);
                    // Continue bubbling even if handled, so parents can see handled events if they subscribed with handledEventsToo
                }
            }
            else // Direct
            {
                InvokeHandler(e);
            }
        }
        finally
        {
            // Clear only the used segment to avoid O(n) clearing of the full rented buffer.
            int limit = depth < array.Length ? depth : array.Length;
            for (int i = 0; i < limit; i++)
            {
                array[i] = null!;
            }

            System.Buffers.ArrayPool<UIElement>.Shared.Return(array, clearArray: false);
        }
    }

    private void InvokeHandler(RoutedEventArgs e)
    {
        // Update Local Coordinates for MouseEvents
        if (e is MouseEventArgs me)
        {
            var local = this.PointFromScreen(new Point(me.GlobalX, me.GlobalY));
            me.X = local.X;
            me.Y = local.Y;
        }

        // 1. Class Handler (virtual method)
        OnEvent(e);

        // 2. Instance Handlers
        if (_eventHandlers != null && _eventHandlers.TryGetValue(e.RoutedEvent, out var handlers))
        {
            // Clone list to allow modification during event? Or just iterate carefully.
            // Using for loop is safer.
            for (int i = 0; i < handlers.Count; i++)
            {
                var info = handlers[i];
                if (!e.Handled || info.HandledEventsToo)
                {
                    if (info.Handler is RoutedEventHandler reh)
                    {
                        reh(this, e);
                    }
                    else
                    {
                        info.Handler.DynamicInvoke(this, e);
                    }
                }
            }
        }
    }

    public static readonly RoutedEvent PreviewKeyDownEvent = RoutedEvent.Register("PreviewKeyDown", RoutingStrategy.Tunnel, typeof(RoutedEventHandler), typeof(UIElement));
    public static readonly RoutedEvent PreviewKeyUpEvent = RoutedEvent.Register("PreviewKeyUp", RoutingStrategy.Tunnel, typeof(RoutedEventHandler), typeof(UIElement));
    public static readonly RoutedEvent PreviewMouseDownEvent = RoutedEvent.Register("PreviewMouseDown", RoutingStrategy.Tunnel, typeof(RoutedEventHandler), typeof(UIElement));
    public static readonly RoutedEvent PreviewMouseUpEvent = RoutedEvent.Register("PreviewMouseUp", RoutingStrategy.Tunnel, typeof(RoutedEventHandler), typeof(UIElement));
    public static readonly RoutedEvent PreviewMouseMoveEvent = RoutedEvent.Register("PreviewMouseMove", RoutingStrategy.Tunnel, typeof(RoutedEventHandler), typeof(UIElement));

    public static readonly RoutedEvent KeyDownEvent = RoutedEvent.Register("KeyDown", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(UIElement));
    public static readonly RoutedEvent KeyUpEvent = RoutedEvent.Register("KeyUp", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(UIElement));
    public static readonly RoutedEvent MouseDownEvent = RoutedEvent.Register("MouseDown", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(UIElement));
    public static readonly RoutedEvent MouseUpEvent = RoutedEvent.Register("MouseUp", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(UIElement));
    public static readonly RoutedEvent MouseMoveEvent = RoutedEvent.Register("MouseMove", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(UIElement));
    public static readonly RoutedEvent GotFocusEvent = RoutedEvent.Register("GotFocus", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(UIElement));
    public static readonly RoutedEvent LostFocusEvent = RoutedEvent.Register("LostFocus", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(UIElement));

    protected virtual void OnEvent(RoutedEventArgs e)
    {
        if (e.RoutedEvent == PreviewKeyDownEvent) OnPreviewKeyDown((KeyEventArgs)e);
        else if (e.RoutedEvent == PreviewKeyUpEvent) OnPreviewKeyUp((KeyEventArgs)e);
        else if (e.RoutedEvent == PreviewMouseDownEvent) OnPreviewMouseDown((MouseEventArgs)e);
        else if (e.RoutedEvent == PreviewMouseUpEvent) OnPreviewMouseUp((MouseEventArgs)e);
        else if (e.RoutedEvent == PreviewMouseMoveEvent) OnPreviewMouseMove((MouseEventArgs)e);
        else if (e.RoutedEvent == KeyDownEvent) OnKeyDown((KeyEventArgs)e);
        else if (e.RoutedEvent == KeyUpEvent) OnKeyUp((KeyEventArgs)e);
        else if (e.RoutedEvent == MouseDownEvent) OnMouseDown((MouseEventArgs)e);
        else if (e.RoutedEvent == MouseUpEvent) OnMouseUp((MouseEventArgs)e);
        else if (e.RoutedEvent == MouseMoveEvent) OnMouseMove((MouseEventArgs)e);
        else if (e.RoutedEvent == GotFocusEvent) OnGotFocus();
        else if (e.RoutedEvent == LostFocusEvent) OnLostFocus();
    }

    public virtual void OnPreviewKeyDown(KeyEventArgs e) { }
    public virtual void OnPreviewKeyUp(KeyEventArgs e) { }
    public virtual void OnPreviewMouseDown(MouseEventArgs e) { }
    public virtual void OnPreviewMouseUp(MouseEventArgs e) { }
    public virtual void OnPreviewMouseMove(MouseEventArgs e) { }

    public virtual void OnKeyDown(KeyEventArgs e) { }
    public virtual void OnKeyUp(KeyEventArgs e) { }
    public virtual void OnMouseDown(MouseEventArgs e) { }
    public virtual void OnMouseUp(MouseEventArgs e) { }
    public virtual void OnMouseMove(MouseEventArgs e) { }

    public virtual void OnGotFocus()
    {
        IsFocused = true;
        Invalidate();
    }

    public virtual void OnLostFocus()
    {
        IsFocused = false;
        Invalidate();
    }

    public bool Focus()
    {
        if (IsEnabled && Visibility)
        {
            // Traverse up to Window/Root to set focus
            var root = GetRoot();
            if (root is TuiWindow window)
            {
                return window.SetFocus(this);
            }
        }
        return false;
    }

    public UIElement GetRoot()
    {
        var current = this;
        while (current.Parent != null)
        {
            current = current.Parent;
        }
        return current;
    }

    public virtual void Invalidate()
    {
        if (Parent != null) Parent.Invalidate();
    }

    public T? FindAncestor<T>() where T : UIElement
    {
        var current = Parent;
        while (current != null)
        {
            if (current is T match) return match;
            current = current.Parent;
        }
        return null;
    }

    public Point PointToScreen(Point point)
    {
        int x = point.X;
        int y = point.Y;
        var current = this;
        while (current != null)
        {
            x += current.RenderSize.X;
            y += current.RenderSize.Y;
            current = current.Parent;
        }
        return new Point(x, y);
    }

    public Point PointFromScreen(Point point)
    {
        int x = point.X;
        int y = point.Y;
        var current = this;
        // This is tricky because we need to subtract parent's offsets.
        // Or we just calculate this.PointToScreen(0,0) and subtract it from point.
        var screenPos = PointToScreen(new Point(0, 0));
        return new Point(x - screenPos.X, y - screenPos.Y);
    }
}

public class KeyEventArgs : RoutedEventArgs
{
    public ConsoleKey Key { get; set; }
    public char KeyChar { get; set; }
    public ConsoleModifiers Modifiers { get; set; }

    public KeyEventArgs(RoutedEvent routedEvent, object source) : base(routedEvent, source)
    {
    }

    public KeyEventArgs(RoutedEvent routedEvent) : base(routedEvent)
    {
    }

    public KeyEventArgs() : base(UIElement.KeyDownEvent)
    {
    }
}

public class MouseEventArgs : RoutedEventArgs
{
    public int X { get; set; }
    public int Y { get; set; }

    // Global Coordinates (Screen/Console space)
    public int GlobalX { get; set; }
    public int GlobalY { get; set; }

    public MouseEventArgs(RoutedEvent routedEvent, object source) : base(routedEvent, source)
    {
    }

    public MouseEventArgs(RoutedEvent routedEvent) : base(routedEvent)
    {
    }

    public MouseEventArgs() : base(UIElement.MouseDownEvent)
    {
    }

    public Point GetPosition(UIElement relativeTo)
    {
        if (relativeTo == null) return new Point(GlobalX, GlobalY);
        return relativeTo.PointFromScreen(new Point(GlobalX, GlobalY));
    }
}

public class HitTestResult
{
    public UIElement Element { get; set; }
    public int LocalX { get; set; }
    public int LocalY { get; set; }

    public HitTestResult(UIElement element, int localX, int localY)
    {
        Element = element;
        LocalX = localX;
        LocalY = localY;
    }
}
