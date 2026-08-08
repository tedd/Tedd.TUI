using Tedd.TUI.Tests.TestInfrastructure;
using Xunit;

namespace Tedd.TUI.Tests;

/// <summary>
/// A scroll surface nested directly inside another one (the classic "scrollable list
/// filling a dialog") must scroll itself rather than grow to its whole content and
/// leave the outer container to scroll in its place.
/// </summary>
public class NestedScrollingTests
{
    private const int Notch = MouseWheelEventArgs.WheelNotch;

    private static ScrollViewer MakeViewer(int lines = 20)
    {
        var stack = new StackPanel();
        for (int i = 0; i < lines; i++)
            stack.AddChild(new TextBlock { Text = $"line {i}" });
        return new ScrollViewer { Content = stack };
    }

    private static ControlTestHost ShowDialog(DialogBox dialog, int width = 60, int height = 24)
    {
        var host = new ControlTestHost(new TextBlock { Text = "background" }, width, height);
        host.Window.PushOverlay(dialog);
        dialog.Show();
        return host;
    }

    [Fact]
    public void ViewerFillingDialog_TakesTheDialogViewport()
    {
        var inner = MakeViewer();
        var dialog = new DialogBox { Title = "T", Width = 30, Height = 12, Content = inner };
        ShowDialog(dialog);

        // Frame (12) minus border (2) minus padding (2) = 8 rows of viewport, and the
        // viewer must not grow past it even though its content is 20 rows tall.
        Assert.Equal(8, inner.RenderSize.Height);
        Assert.True(inner.IsVerticalScrollBarShown);

        // The dialog itself has nothing left to scroll.
        Assert.False(dialog.IsVerticalScrollBarShown);
    }

    [Fact]
    public void ViewerFillingDialog_WheelScrollsTheInnerViewer()
    {
        var inner = MakeViewer();
        var dialog = new DialogBox { Title = "T", Width = 30, Height = 12, Content = inner };
        var host = ShowDialog(dialog);

        var point = inner.PointToScreen(new Point(2, 2));
        host.MouseWheel(point.X, point.Y, -Notch);

        Assert.Equal(3, inner.VerticalOffset);
        Assert.Equal(0, dialog.VerticalOffset);
    }

    [Fact]
    public void ViewerFillingDialog_ClickOnItsScrollBarScrolls()
    {
        var inner = MakeViewer();
        var dialog = new DialogBox { Title = "T", Width = 30, Height = 12, Content = inner };
        var host = ShowDialog(dialog);

        // Bottom cell of the inner bar is its "down" arrow.
        var arrow = inner.PointToScreen(new Point(inner.RenderSize.Width - 1, inner.RenderSize.Height - 1));
        var hit = host.Window.InputHitTest(arrow.X, arrow.Y);
        Assert.Same(inner, hit!.Element.Parent);

        host.Click(arrow.X, arrow.Y);
        Assert.Equal(1, inner.VerticalOffset);
        Assert.Equal(0, dialog.VerticalOffset);
    }

    [Fact]
    public void ViewerFillingDialog_ScrolledContentIsRendered()
    {
        var inner = MakeViewer();
        var dialog = new DialogBox { Title = "T", Width = 30, Height = 12, Content = inner };
        var host = ShowDialog(dialog);

        var text = VirtualBufferAssertions.GetText(host.Render());
        Assert.Contains("line 0", text);
        Assert.DoesNotContain("line 8", text);

        host.MouseWheel(inner.PointToScreen(new Point(2, 2)).X, inner.PointToScreen(new Point(2, 2)).Y, -Notch);

        text = VirtualBufferAssertions.GetText(host.Render());
        Assert.DoesNotContain("line 0", text);
        Assert.Contains("line 3", text);
    }

    [Fact]
    public void ViewerFillingViewer_TakesTheOuterViewport()
    {
        var inner = MakeViewer();
        var outer = new ScrollViewer { Content = inner };
        var host = new ControlTestHost(outer, 30, 12);

        Assert.Equal(12, inner.RenderSize.Height);

        host.MouseWheel(3, 3, -Notch);
        Assert.Equal(3, inner.VerticalOffset);
        Assert.Equal(0, outer.VerticalOffset);
    }

    [Fact]
    public void ViewerFillingScrollingBorder_TakesTheBorderViewport()
    {
        var inner = MakeViewer();
        var border = new Border
        {
            Child = inner,
            Width = 30,
            Height = 12,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto
        };
        // Inside a stack panel: the border is arranged at its own size, and is offered
        // an unbounded height along the stack axis -- the case the frame size resolves.
        var panel = new StackPanel();
        panel.AddChild(border);
        var host = new ControlTestHost(panel, 40, 20);

        // Border frame (12) minus border line (2) minus padding (2).
        Assert.Equal(8, inner.RenderSize.Height);
        Assert.False(border.IsVerticalScrollBarShown);

        var point = inner.PointToScreen(new Point(2, 2));
        host.MouseWheel(point.X, point.Y, -Notch);
        Assert.Equal(3, inner.VerticalOffset);
    }

    [Fact]
    public void NonScrollingContentStillDrivesTheOuterScrollBar()
    {
        // A plain (non-scrolling) child must keep reporting its natural extent so the
        // dialog can still discover the overflow and scroll it.
        var stack = new StackPanel();
        for (int i = 0; i < 20; i++)
            stack.AddChild(new TextBlock { Text = $"line {i}" });

        var dialog = new DialogBox { Title = "T", Width = 30, Height = 12, Content = stack };
        var host = ShowDialog(dialog);

        Assert.True(dialog.IsVerticalScrollBarShown);
        Assert.Equal(20, stack.RenderSize.Height);

        host.MouseWheel(dialog.RenderSize.X + 5, dialog.RenderSize.Y + 5, -Notch);
        Assert.Equal(3, dialog.VerticalOffset);
    }

    [Fact]
    public void ListBoxFillingDialog_TakesTheDialogViewportAndScrolls()
    {
        var list = new ListBox { Width = 20 };
        for (int i = 0; i < 30; i++)
            list.Items.Add($"Item{i}");

        var dialog = new DialogBox { Title = "T", Width = 30, Height = 12, Content = list };
        var host = ShowDialog(dialog);

        Assert.Equal(8, list.RenderSize.Height);
        Assert.False(dialog.IsVerticalScrollBarShown);

        var text = VirtualBufferAssertions.GetText(host.Render());
        Assert.Contains("Item0", text);
        Assert.DoesNotContain("Item8", text);

        var point = list.PointToScreen(new Point(2, 2));
        host.MouseWheel(point.X, point.Y, -Notch);

        Assert.Equal(0, dialog.VerticalOffset);
        text = VirtualBufferAssertions.GetText(host.Render());
        Assert.DoesNotContain("Item0", text);
        Assert.Contains("Item3", text);
    }

    [Fact]
    public void TreeViewFillingDialog_TakesTheDialogViewport()
    {
        var tree = new TreeView();
        for (int i = 0; i < 30; i++)
            tree.Items.Add(new TreeViewItem { Header = $"Node{i}" });

        var dialog = new DialogBox { Title = "T", Width = 30, Height = 12, Content = tree };
        ShowDialog(dialog);

        Assert.Equal(8, tree.RenderSize.Height);
        Assert.False(dialog.IsVerticalScrollBarShown);
    }

    [Fact]
    public void DataGridFillingDialog_TakesTheDialogViewport()
    {
        var grid = new DataGrid { ShowHeader = true, ShowBorder = false, AutoGenerateColumns = false };
        grid.Columns.Add(new DataGridColumn
        {
            Header = "Name",
            BindingPath = "Name",
            Width = new GridLength(10, GridUnitType.Pixel)
        });
        var rows = new List<object>();
        for (int i = 0; i < 30; i++) rows.Add(new { Name = $"Row{i}" });
        grid.ItemsSource = rows;

        var dialog = new DialogBox { Title = "T", Width = 30, Height = 12, Content = grid };
        ShowDialog(dialog);

        Assert.Equal(8, grid.RenderSize.Height);
        Assert.False(dialog.IsVerticalScrollBarShown);
    }

    [Fact]
    public void ViewerWithScrollingDisabled_StillPassesTheConstraintThrough()
    {
        // A viewer that cannot scroll vertically is not a vertical viewport: the dialog
        // must keep offering it the natural extent so the overflow is scrollable at all.
        var stack = new StackPanel();
        for (int i = 0; i < 20; i++)
            stack.AddChild(new TextBlock { Text = $"line {i}" });
        var inner = new ScrollViewer
        {
            Content = stack,
            VerticalScrollBarVisibility = ScrollBarVisibility.Disabled
        };

        var dialog = new DialogBox { Title = "T", Width = 30, Height = 12, Content = inner };
        ShowDialog(dialog);

        Assert.False(inner.ScrollsOwnContent(Orientation.Vertical));
        Assert.True(dialog.IsVerticalScrollBarShown);
    }
}
