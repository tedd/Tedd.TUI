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
    public UIElement Parent { get; internal set; }

    public static readonly DependencyProperty BackgroundProperty =
        DependencyProperty.Register("Background", typeof(ConsoleColor?), typeof(UIElement), null);

    public ConsoleColor? Background
    {
        get { return (ConsoleColor?)GetValue(BackgroundProperty); }
        set { SetValue(BackgroundProperty, value); }
    }

    public static readonly DependencyProperty IsFocusedProperty =
        DependencyProperty.Register("IsFocused", typeof(bool), typeof(UIElement), false);

    public bool IsFocused
    {
        get { return (bool)GetValue(IsFocusedProperty); }
        set { SetValue(IsFocusedProperty, value); }
    }

    public static readonly DependencyProperty IsEnabledProperty =
        DependencyProperty.Register("IsEnabled", typeof(bool), typeof(UIElement), true);

    public bool IsEnabled
    {
        get { return (bool)GetValue(IsEnabledProperty); }
        set { SetValue(IsEnabledProperty, value); }
    }

    public static readonly DependencyProperty VisibilityProperty =
        DependencyProperty.Register("Visibility", typeof(bool), typeof(UIElement), true);

    public bool Visibility
    {
        get { return (bool)GetValue(VisibilityProperty); }
        set { SetValue(VisibilityProperty, value); }
    }

    public static readonly DependencyProperty FocusableProperty =
        DependencyProperty.Register("Focusable", typeof(bool), typeof(UIElement), false);

    public bool Focusable
    {
        get { return (bool)GetValue(FocusableProperty); }
        set { SetValue(FocusableProperty, value); }
    }

    public static readonly DependencyProperty WidthProperty =
        DependencyProperty.Register("Width", typeof(int), typeof(UIElement), -1); // -1 for Auto

    public int Width
    {
        get { return (int)GetValue(WidthProperty); }
        set { SetValue(WidthProperty, value); }
    }

    public static readonly DependencyProperty HeightProperty =
        DependencyProperty.Register("Height", typeof(int), typeof(UIElement), -1); // -1 for Auto

    public int Height
    {
        get { return (int)GetValue(HeightProperty); }
        set { SetValue(HeightProperty, value); }
    }

    public static readonly DependencyProperty HorizontalAlignmentProperty =
        DependencyProperty.Register("HorizontalAlignment", typeof(HorizontalAlignment), typeof(UIElement), HorizontalAlignment.Stretch);

    public HorizontalAlignment HorizontalAlignment
    {
        get { return (HorizontalAlignment)GetValue(HorizontalAlignmentProperty); }
        set { SetValue(HorizontalAlignmentProperty, value); }
    }

    public static readonly DependencyProperty VerticalAlignmentProperty =
        DependencyProperty.Register("VerticalAlignment", typeof(VerticalAlignment), typeof(UIElement), VerticalAlignment.Stretch);

    public VerticalAlignment VerticalAlignment
    {
        get { return (VerticalAlignment)GetValue(VerticalAlignmentProperty); }
        set { SetValue(VerticalAlignmentProperty, value); }
    }

    public static readonly DependencyProperty DataContextProperty =
        DependencyProperty.Register("DataContext", typeof(object), typeof(UIElement), null);

    public object DataContext
    {
        get { return GetValue(DataContextProperty); }
        set 
        { 
            SetValue(DataContextProperty, value);
            // Notify bindings? For now, we rely on SetBinding to trigger initial update 
            // or property change notification.
            // In a real system, changing DataContext should propagate down and update all bindings.
            // We'll implement propagation roughly via OnPropertyChanged override if needed.
        }
    }

    private readonly List<BindingExpression> _bindings = new List<BindingExpression>();

    public void SetBinding(DependencyProperty dp, Binding binding)
    {
        var expr = new BindingExpression(this, dp, binding);
        _bindings.Add(expr);
        expr.UpdateTarget();
    }

    protected override void OnPropertyChanged(DependencyProperty dp)
    {
        base.OnPropertyChanged(dp);
        if (dp == DataContextProperty)
        {
            // Update local bindings
            foreach (var binding in _bindings)
            {
                binding.UpdateTarget();
            }
            
            OnDataContextChanged(this.DataContext);
        }
    }

    protected virtual void OnDataContextChanged(object newValue)
    {
        // To be overridden by containers to propagate
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

        // Apply Margin etc here if we had it.
        
        Size desired = MeasureOverride(availableSize);
        
        // Respect Width/Height properties
        int width = Width;
        int height = Height;

        if (width >= 0) desired.Width = width;
        if (height >= 0) desired.Height = height;

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

        // Check alignment and adjust finalRect
        Size desired = DesiredSize;
        int width = finalRect.Width;
        int height = finalRect.Height;
        int x = finalRect.X;
        int y = finalRect.Y;

        // Horizontal Alignment
        if (HorizontalAlignment == HorizontalAlignment.Left)
        {
            width = desired.Width;
        }
        else if (HorizontalAlignment == HorizontalAlignment.Right)
        {
            x += width - desired.Width;
            width = desired.Width;
        }
        else if (HorizontalAlignment == HorizontalAlignment.Center)
        {
            x += (width - desired.Width) / 2;
            width = desired.Width;
        }
        // Stretch takes full width (already set)

        // Vertical Alignment
        if (VerticalAlignment == VerticalAlignment.Top)
        {
            height = desired.Height;
        }
        else if (VerticalAlignment == VerticalAlignment.Bottom)
        {
            y += height - desired.Height;
            height = desired.Height;
        }
        else if (VerticalAlignment == VerticalAlignment.Center)
        {
            y += (height - desired.Height) / 2;
            height = desired.Height;
        }

        // Constrain to available finalRect?
        if (width < 0) width = 0;
        if (height < 0) height = 0;

        Rect arrangedRect = new Rect(x, y, width, height);
        RenderSize = arrangedRect; // Storing position and size relative to parent Canvas

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
    public virtual void OnKeyDown(KeyEventArgs e) { }
    public virtual void OnKeyUp(KeyEventArgs e) { }
    public virtual void OnMouseDown(MouseEventArgs e) { }
    public virtual void OnMouseUp(MouseEventArgs e) { }

    public virtual void OnGotFocus()
    {
        IsFocused = true;
    }

    public virtual void OnLostFocus()
    {
        IsFocused = false;
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
}

public class KeyEventArgs
{
    public ConsoleKey Key { get; set; }
    public char KeyChar { get; set; }
    public ConsoleModifiers Modifiers { get; set; }
    public bool Handled { get; set; }
}

public class MouseEventArgs
{
    public int X { get; set; }
    public int Y { get; set; }
    public bool Handled { get; set; }
    // Add Buttons state etc if needed
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
