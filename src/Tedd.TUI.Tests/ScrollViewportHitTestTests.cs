using Tedd.TUI.Tests.TestInfrastructure;
using Xunit;

namespace Tedd.TUI.Tests;

/// <summary>
/// Hit testing must look through the same window onto scrolled content that rendering
/// draws: a click on the frame around it belongs to the frame, not to whichever row the
/// scroll offset happens to have parked behind it.
/// </summary>
public class ScrollViewportHitTestTests
{
    private static (StackPanel Panel, List<Button> Buttons) MakeButtons(int count)
    {
        var panel = new StackPanel();
        var buttons = new List<Button>();
        for (int i = 0; i < count; i++)
        {
            var button = new Button { Content = $"B{i}", BoxStyle = BoxStyle.None };
            buttons.Add(button);
            panel.AddChild(button);
        }
        return (panel, buttons);
    }

    private static bool IsWithin(UIElement? element, UIElement ancestor)
    {
        for (var current = element; current != null; current = current.Parent)
            if (ReferenceEquals(current, ancestor)) return true;
        return false;
    }

    [Fact]
    public void ScrolledDialog_ClickOnFrameDoesNotReachClippedContent()
    {
        var (panel, buttons) = MakeButtons(20);
        var dialog = new DialogBox { Title = "T", Width = 30, Height = 12, Content = panel };
        var host = new ControlTestHost(new TextBlock { Text = "background" }, 60, 24);
        host.Window.PushOverlay(dialog);
        dialog.Show();

        dialog.ScrollToVerticalOffset(4);

        int x = dialog.RenderSize.X + 5;
        int top = dialog.RenderSize.Y;
        int bottom = top + dialog.RenderSize.Height - 1;

        // Title bar and top padding gutter: frame, even though rows 2 and 3 of the
        // content sit behind them once the dialog is scrolled.
        Assert.Same(dialog, host.Window.InputHitTest(x, top)!.Element);
        Assert.Same(dialog, host.Window.InputHitTest(x, top + 1)!.Element);

        // Bottom padding gutter and bottom border.
        Assert.Same(dialog, host.Window.InputHitTest(x, bottom - 1)!.Element);
        Assert.Same(dialog, host.Window.InputHitTest(x, bottom)!.Element);

        // The first viewport row still resolves to the button actually drawn there.
        var hit = host.Window.InputHitTest(x, top + 2);
        Assert.True(IsWithin(hit!.Element, buttons[4]));
    }

    [Fact]
    public void ScrolledDialog_ClickOnFrameDoesNotFireClippedButton()
    {
        var (panel, buttons) = MakeButtons(20);
        var dialog = new DialogBox { Title = "T", Width = 30, Height = 12, Content = panel };
        var host = new ControlTestHost(new TextBlock { Text = "background" }, 60, 24);
        host.Window.PushOverlay(dialog);
        dialog.Show();

        dialog.ScrollToVerticalOffset(4);

        int clicked = 0;
        foreach (var button in buttons)
            button.Click += (_, _) => clicked++;

        host.Click(dialog.RenderSize.X + 5, dialog.RenderSize.Y);
        Assert.Equal(0, clicked);

        host.Click(dialog.RenderSize.X + 5, dialog.RenderSize.Y + 2);
        Assert.Equal(1, clicked);
    }

    [Fact]
    public void ScrolledBorder_ClickOnLineAndPaddingDoesNotReachClippedContent()
    {
        var (panel, buttons) = MakeButtons(20);
        var border = new Border
        {
            Child = panel,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto
        };
        var host = new ControlTestHost(border, 30, 12);

        border.ScrollToVerticalOffset(4);

        Assert.Same(border, host.Window.InputHitTest(5, 0)!.Element);
        Assert.Same(border, host.Window.InputHitTest(5, 1)!.Element);
        Assert.Same(border, host.Window.InputHitTest(5, 11)!.Element);

        var hit = host.Window.InputHitTest(5, 2);
        Assert.True(IsWithin(hit!.Element, buttons[4]));
    }

    [Fact]
    public void ScrolledViewer_ClickBesideTheScrollBarStillReachesContent()
    {
        // A plain viewer has no frame: every row and every column left of its scrollbar
        // is viewport, so clipping must not eat into it.
        var (panel, buttons) = MakeButtons(20);
        var viewer = new ScrollViewer { Content = panel };
        var host = new ControlTestHost(viewer, 30, 12);

        viewer.ScrollToVerticalOffset(4);

        Assert.True(IsWithin(host.Window.InputHitTest(1, 0)!.Element, buttons[4]));
        Assert.True(IsWithin(host.Window.InputHitTest(1, 11)!.Element, buttons[15]));

        // The last column is the scrollbar, not content.
        Assert.IsType<ScrollBar>(host.Window.InputHitTest(29, 5)!.Element);
    }

    [Fact]
    public void BorderlessBorder_PassesTheWholeBoxThroughToContent()
    {
        var (panel, buttons) = MakeButtons(20);
        var border = new Border { Child = panel, BoxStyle = BoxStyle.None };
        var host = new ControlTestHost(border, 30, 12);

        Assert.True(IsWithin(host.Window.InputHitTest(1, 0)!.Element, buttons[0]));
        Assert.True(IsWithin(host.Window.InputHitTest(1, 11)!.Element, buttons[11]));
    }
}
