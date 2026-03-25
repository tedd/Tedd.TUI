using System;
using System.Reflection;

namespace Tedd.TUI;

public class ListBox : Selector
{
    public ListBox()
    {
        Focusable = true;
        Foreground = ConsoleColor.Gray;

        // Default ControlTemplate uses ScrollViewer wrapping ItemsPresenter
        Template = new ControlTemplate(parent =>
        {
            var sv = new ScrollViewer
            {
                VerticalScrollBarVisibility = true,
                HorizontalScrollBarVisibility = false,
                TemplatedParent = parent
            };

            var itemsPresenter = new ItemsPresenter
            {
                TemplatedParent = parent
            };

            sv.Content = itemsPresenter;
            return sv;
        });

        // Add class handler for selection changes from ListBoxItem
        // We use instance handlers for now since there's no RegisterClassHandler in Tedd.TUI
        AddHandler(ListBoxItem.SelectedEvent, new RoutedEventHandler(OnItemSelected));
        AddHandler(ListBoxItem.UnselectedEvent, new RoutedEventHandler(OnItemUnselected));
    }

    private void OnItemSelected(object? sender, RoutedEventArgs e)
    {
        if (e.OriginalSource is ListBoxItem lbi)
        {
            // Find the index of this item in Items collection
            var item = ItemContainerGenerator_ItemFromContainer(lbi);
            if (item != null)
            {
                int idx = Items.IndexOf(item);
                if (idx >= 0 && SelectedIndex != idx)
                {
                    SelectedIndex = idx;
                }
            }
        }
    }

    private void OnItemUnselected(object? sender, RoutedEventArgs e)
    {
        if (e.OriginalSource is ListBoxItem lbi)
        {
            var item = ItemContainerGenerator_ItemFromContainer(lbi);
            if (item != null)
            {
                int idx = Items.IndexOf(item);
                if (idx >= 0 && SelectedIndex == idx)
                {
                    // Optionally clear selection if unselected, but standard ListBox handles multiple or we just set to -1
                    SelectedIndex = -1;
                }
            }
        }
    }

    private object? ItemContainerGenerator_ItemFromContainer(UIElement container)
    {
        // Simple hack since ItemContainerGenerator doesn't exist:
        // iterate items and if IsItemItsOwnContainer, return it, otherwise check container
        if (ItemsPanelRoot != null)
        {
            int index = ItemsPanelRoot.Children.IndexOf(container);
            if (index >= 0 && index < Items.Count)
            {
                return Items[index];
            }
        }
        return null;
    }

    protected override void OnSelectionChanged()
    {
        base.OnSelectionChanged();

        // Update IsSelected on containers
        if (ItemsPanelRoot != null)
        {
            for (int i = 0; i < Items.Count; i++)
            {
                if (i < ItemsPanelRoot.Children.Count && ItemsPanelRoot.Children[i] is ListBoxItem lbi)
                {
                    lbi.IsSelected = (i == SelectedIndex);
                }
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
        if (element is ListBoxItem lbi)
        {
            if (ItemTemplate != null)
            {
                lbi.ContentTemplate = ItemTemplate;
                lbi.Content = item;
            }
            else
            {
                lbi.Content = GetItemText(item);
            }

            int index = Items.IndexOf(item);
            if (index == SelectedIndex)
            {
                lbi.IsSelected = true;
            }
        }
        else
        {
            base.PrepareContainerForItemOverride(element, item);
        }
    }

    public override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (e.Key == ConsoleKey.UpArrow)
        {
            if (SelectedIndex > 0)
            {
                SelectedIndex--;
                EnsureVisible(SelectedIndex);
            }
            e.Handled = true;
        }
        else if (e.Key == ConsoleKey.DownArrow)
        {
            if (SelectedIndex < Items.Count - 1)
            {
                SelectedIndex++;
                EnsureVisible(SelectedIndex);
            }
            e.Handled = true;
        }
        else if (e.Key == ConsoleKey.Enter || e.Key == ConsoleKey.Spacebar)
        {
            // Enter and space usually just select item or do nothing since arrows already selected
            e.Handled = true;
        }
    }

    private void EnsureVisible(int index)
    {
        // Now handled by ScrollViewer, so we should attempt to scroll the ScrollViewer
        if (TemplateRoot is ScrollViewer sv && ItemsPanelRoot != null && index >= 0 && index < ItemsPanelRoot.Children.Count)
        {
            var itemElement = ItemsPanelRoot.Children[index];
            // Compute Y position of itemElement relative to ItemsPanelRoot
            int itemY = itemElement.RenderSize.Y;
            int itemHeight = itemElement.RenderSize.Height > 0 ? itemElement.RenderSize.Height : 1;

            int viewportHeight = sv.RenderSize.Height;
            // ScrollViewer subtracts padding/scrollbar, but for approximation RenderSize.Height is okay
            // Better approximation: Height of scroll viewer content available:
            if (sv.HorizontalScrollBarVisibility) viewportHeight--;

            if (itemY < sv.VerticalOffset)
            {
                sv.ScrollToVerticalOffset(itemY);
            }
            else if (itemY + itemHeight > sv.VerticalOffset + viewportHeight)
            {
                sv.ScrollToVerticalOffset(itemY + itemHeight - viewportHeight);
            }
        }
    }
}
