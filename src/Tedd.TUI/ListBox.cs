using System;
using System.Reflection;

namespace Tedd.TUI;

public class ListBox : Selector
{
    public ListBox()
    {
        Focusable = true;

        Foreground = ConsoleColor.Gray;

        Template = new ControlTemplate(parent =>
        {
            var sv = new ScrollViewer
            {
                VerticalScrollBarVisibility = true,
                HorizontalScrollBarVisibility = false // TUI ListBox usually does not scroll horizontally by default
            };

            var ip = new ItemsPresenter();
            ip.TemplatedParent = parent;

            sv.Content = ip;

            return sv;
        });

        // Listen to SelectedEvent from children to update selection
        AddHandler(ListBoxItem.SelectedEvent, new RoutedEventHandler(OnItemSelected));
    }

    private void OnItemSelected(object? sender, RoutedEventArgs e)
    {
        if (e.OriginalSource is ListBoxItem item)
        {
            int index = ItemsPanelRoot?.Children.IndexOf(item) ?? -1;
            if (index >= 0 && index != SelectedIndex)
            {
                SelectedIndex = index;
            }
        }
    }

    /// <summary>
    /// When true (default), selection is visible even when unfocused.
    /// When false, selection highlighting is only shown while focused.
    /// </summary>
    public bool ShowSelection { get; set; } = true;

    public new static readonly DependencyProperty ForegroundProperty = UIElement.ForegroundProperty;

    public static readonly DependencyProperty SelectionForegroundProperty =
        DependencyProperty.Register("SelectionForeground", typeof(ConsoleColor), typeof(ListBox), ConsoleColor.Black);

    public ConsoleColor SelectionForeground
    {
        get => (ConsoleColor)GetValue(SelectionForegroundProperty);
        set => SetValue(SelectionForegroundProperty, value);
    }

    public static readonly DependencyProperty SelectionBackgroundProperty =
        DependencyProperty.Register("SelectionBackground", typeof(ConsoleColor), typeof(ListBox), ConsoleColor.White);

    public ConsoleColor SelectionBackground
    {
        get => (ConsoleColor)GetValue(SelectionBackgroundProperty);
        set => SetValue(SelectionBackgroundProperty, value);
    }

    public static readonly DependencyProperty FocusedSelectionForegroundProperty =
        DependencyProperty.Register("FocusedSelectionForeground", typeof(ConsoleColor), typeof(ListBox), ConsoleColor.White);

    public ConsoleColor FocusedSelectionForeground
    {
        get => (ConsoleColor)GetValue(FocusedSelectionForegroundProperty);
        set => SetValue(FocusedSelectionForegroundProperty, value);
    }

    public static readonly DependencyProperty FocusedSelectionBackgroundProperty =
        DependencyProperty.Register("FocusedSelectionBackground", typeof(ConsoleColor), typeof(ListBox), ConsoleColor.Blue);

    public ConsoleColor FocusedSelectionBackground
    {
        get => (ConsoleColor)GetValue(FocusedSelectionBackgroundProperty);
        set => SetValue(FocusedSelectionBackgroundProperty, value);
    }

    protected internal override bool IsItemItsOwnContainerOverride(object item)
    {
        return item is ListBoxItem;
    }

    protected internal override UIElement GetContainerForItemOverride()
    {
        return new ListBoxItem();
    }

    protected internal override void PrepareContainerForItemOverride(UIElement element, object item)
    {
        base.PrepareContainerForItemOverride(element, item);
        if (element is ListBoxItem lbi)
        {
            // Set content correctly based on ItemTemplate or fallback
            if (ItemTemplate != null)
            {
                lbi.ContentTemplate = ItemTemplate;
                lbi.Content = item;
            }
            else if (item is UIElement uiElement)
            {
                // Preserve UIElement items as content so they render and interact correctly
                lbi.Content = uiElement;
            }
            else
            {
                lbi.Content = GetItemText(item);
            }

            // Sync IsSelected
            int index = Items.IndexOf(item);
            if (index == SelectedIndex)
            {
                lbi.IsSelected = true;
            }
            else
            {
                lbi.IsSelected = false;
            }
        }
    }

    public override void OnGotFocus()
    {
        base.OnGotFocus();
        NotifyContainersVisualStateChanged();
    }

    public override void OnLostFocus()
    {
        base.OnLostFocus();
        NotifyContainersVisualStateChanged();
    }

    private void NotifyContainersVisualStateChanged()
    {
        if (ItemsPanelRoot != null)
        {
            for (int i = 0; i < ItemsPanelRoot.Children.Count; i++)
            {
                if (ItemsPanelRoot.Children[i] is ListBoxItem lbi)
                    lbi.UpdateVisualState();
            }
        }
    }

    public override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (e.Handled) return;

        if (e.Key == ConsoleKey.UpArrow)
        {
            if (SelectedIndex > 0)
            {
                SelectedIndex--;
                EnsureItemVisible(SelectedIndex);
            }
            e.Handled = true;
        }
        else if (e.Key == ConsoleKey.DownArrow)
        {
            if (SelectedIndex < Items.Count - 1)
            {
                SelectedIndex++;
                EnsureItemVisible(SelectedIndex);
            }
            e.Handled = true;
        }
        else if (e.Key == ConsoleKey.Enter || e.Key == ConsoleKey.Spacebar)
        {
            OnSelectionChanged();
            e.Handled = true;
        }
    }

    private void EnsureItemVisible(int index)
    {
        // Find ScrollViewer inside the template
        if (TemplateRoot is ScrollViewer sv)
        {
            // A simple way to scroll into view based on index.
            // In WPF, we would call BringIntoView on the item.
            // Here, we can just manipulate the scrollviewer.
            int offset = sv.VerticalOffset;
            int viewport = sv.RenderSize.Height; // Approximate viewport size

            if (index < offset)
            {
                sv.ScrollToVerticalOffset(index);
            }
            else if (index >= offset + viewport)
            {
                sv.ScrollToVerticalOffset(index - viewport + 1);
            }
        }
    }

    // In Selector, OnSelectionChanged is fired when SelectedIndex/SelectedItem changes.
    // We override it to sync IsSelected to the containers.
    protected override void OnSelectionChanged()
    {
        base.OnSelectionChanged();

        if (ItemsPanelRoot != null)
        {
            for (int i = 0; i < ItemsPanelRoot.Children.Count; i++)
            {
                if (ItemsPanelRoot.Children[i] is ListBoxItem lbi)
                {
                    lbi.IsSelected = (i == SelectedIndex);
                }
            }
        }
        Invalidate();
    }
}
