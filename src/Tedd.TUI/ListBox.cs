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
            var sv = new ScrollViewer();
            sv.HorizontalScrollBarVisibility = false;
            sv.VerticalScrollBarVisibility = true;

            var ip = new ItemsPresenter();
            ip.TemplatedParent = parent;

            sv.Content = ip;
            return sv;
        });
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

        if (element is ListBoxItem listBoxItem)
        {
            if (ItemTemplate != null)
            {
                listBoxItem.ContentTemplate = ItemTemplate;
                listBoxItem.Content = item;
            }
            else
            {
                listBoxItem.Content = GetItemText(item);
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

    public override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        Focus();
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
            // Space/enter can toggle or just handle it. We already do selection change on arrows.
            e.Handled = true;
        }
    }

    private void EnsureVisible(int index)
    {
        if (TemplateRoot is ScrollViewer sv)
        {
            if (index < sv.VerticalOffset)
            {
                sv.ScrollToVerticalOffset(index);
            }
            else if (index >= sv.VerticalOffset + sv.RenderSize.Height)
            {
                // Simple approx
                sv.ScrollToVerticalOffset(index - sv.RenderSize.Height + 1);
            }
        }
    }
}
