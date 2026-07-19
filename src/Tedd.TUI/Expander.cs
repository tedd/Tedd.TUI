using System;

namespace Tedd.TUI;

public class Expander : HeaderedContentControl
{
    public static readonly DependencyProperty IsExpandedProperty =
        DependencyProperty.Register("IsExpanded", typeof(bool), typeof(Expander), false, bindsTwoWayByDefault: true);

    public bool IsExpanded
    {
        get => (bool)GetValue(IsExpandedProperty);
        set => SetValue(IsExpandedProperty, value);
    }

    public static readonly RoutedEvent ExpandedEvent =
        RoutedEvent.Register("Expanded", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(Expander));

    public event RoutedEventHandler Expanded
    {
        add { AddHandler(ExpandedEvent, value); }
        remove { RemoveHandler(ExpandedEvent, value); }
    }

    public static readonly RoutedEvent CollapsedEvent =
        RoutedEvent.Register("Collapsed", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(Expander));

    public event RoutedEventHandler Collapsed
    {
        add { AddHandler(CollapsedEvent, value); }
        remove { RemoveHandler(CollapsedEvent, value); }
    }

    // Default template construction
    public Expander()
    {
        Focusable = true;

        Template = new ControlTemplate(parent =>
        {
            var expander = (Expander)parent;

            var stack = new StackPanel { Orientation = Orientation.Vertical };
            stack.TemplatedParent = expander;

            // 1. Header (acts as the toggle button)
            var headerContainer = new Border();
            headerContainer.TemplatedParent = expander;

            var headerPanel = new StackPanel { Orientation = Orientation.Horizontal };
            headerPanel.TemplatedParent = expander;

            var indicator = new TextBlock { Text = expander.IsExpanded ? "[-] " : "[+] " };
            indicator.TemplatedParent = expander;
            headerPanel.Children.Add(indicator);

            var headerPresenter = new ContentPresenter();
            headerPresenter.TemplatedParent = expander;

            var headerBinding = new Binding("Header") { RelativeSource = RelativeSource.TemplatedParent };
            headerPresenter.SetBinding(ContentPresenter.ContentProperty, headerBinding);

            var headerTemplateBinding = new Binding("HeaderTemplate") { RelativeSource = RelativeSource.TemplatedParent };
            headerPresenter.SetBinding(ContentPresenter.ContentTemplateProperty, headerTemplateBinding);

            headerPanel.Children.Add(headerPresenter);
            headerContainer.Content = headerPanel;

            // Handle input explicitly at the Expander level to toggle state based on header boundaries.
            stack.Children.Add(headerContainer);

            // 2. Content
            var contentPresenter = new ContentPresenter();
            contentPresenter.TemplatedParent = expander;

            var contentBinding = new Binding("Content") { RelativeSource = RelativeSource.TemplatedParent };
            contentPresenter.SetBinding(ContentPresenter.ContentProperty, contentBinding);

            var contentTemplateBinding = new Binding("ContentTemplate") { RelativeSource = RelativeSource.TemplatedParent };
            contentPresenter.SetBinding(ContentPresenter.ContentTemplateProperty, contentTemplateBinding);

            // Bind HorizontalAlignment to parent.HorizontalContentAlignment
            var hAlignBinding = new Binding("HorizontalContentAlignment");
            hAlignBinding.RelativeSource = RelativeSource.TemplatedParent;
            contentPresenter.SetBinding(UIElement.HorizontalAlignmentProperty, hAlignBinding);

            // Bind VerticalAlignment to parent.VerticalContentAlignment
            var vAlignBinding = new Binding("VerticalContentAlignment");
            vAlignBinding.RelativeSource = RelativeSource.TemplatedParent;
            contentPresenter.SetBinding(UIElement.VerticalAlignmentProperty, vAlignBinding);

            contentPresenter.Visibility = expander.IsExpanded;

            stack.Children.Add(contentPresenter);

            return stack;
        });
    }

    protected override void OnPropertyChanged(DependencyProperty dp)
    {
        base.OnPropertyChanged(dp);
        if (dp == IsExpandedProperty)
        {
            // Reapply template or update visual parts manually
            UpdateVisualState();

            if (IsExpanded)
            {
                RaiseEvent(new RoutedEventArgs(ExpandedEvent, this));
            }
            else
            {
                RaiseEvent(new RoutedEventArgs(CollapsedEvent, this));
            }
        }
    }

    private void UpdateVisualState()
    {
        // Traverse the template root to manually update visual state parts, preserving existing focus context.
        if (TemplateRoot is StackPanel rootStack && rootStack.Children.Count == 2)
        {
            // Header
            if (rootStack.Children[0] is Border headerBorder && headerBorder.Content is StackPanel headerPanel && headerPanel.Children.Count > 0)
            {
                if (headerPanel.Children[0] is TextBlock indicator)
                {
                    indicator.Text = IsExpanded ? "[-] " : "[+] ";
                }
            }

            // Content
            if (rootStack.Children[1] is ContentPresenter contentPresenter)
            {
                contentPresenter.Visibility = IsExpanded;
            }
        }
    }

    // Bug: Clicking nested focusable child inside Expander content causes focus to be stolen by Expander.
    // Root cause: Expander.OnMouseDown unconditionally invokes base.OnMouseDown and continues evaluation.
    // Fix: Return early if the mouse down event has already been handled.
    // Regression: Covered by FocusOverlayTests & general focus routing
    public override void OnMouseDown(MouseEventArgs e)
    {
        if (e.Handled) return;
        base.OnMouseDown(e);

        // Evaluate if the input interaction lands within the boundaries of the header container.
        if (TemplateRoot is StackPanel rootStack && rootStack.Children.Count > 0)
        {
            var header = rootStack.Children[0];

            if (e.Y >= 0 && e.Y < header.RenderSize.Height)
            {
                Focus();
                IsExpanded = !IsExpanded;
                e.Handled = true;
            }
        }
    }

    public override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (e.Key == ConsoleKey.Enter || e.Key == ConsoleKey.Spacebar)
        {
            IsExpanded = !IsExpanded;
            e.Handled = true;
        }
    }
}
