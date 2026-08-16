using System;
using System.ComponentModel;
using Xunit;
using Tedd.TUI;
using Tedd.TUI.Markdown;

namespace Tedd.TUI.Tests;

/// <summary>
/// Regression tests for the idle-CPU render loop: a render pass (Measure/Arrange/Render,
/// exactly what TuiApp does per frame) must not itself raise VisualChanged once the UI
/// has settled, because every VisualChanged re-arms TuiApp's render wait handle. Before
/// the effective-value guard in DependencyObject.SetValue, controls that wrote dependency
/// properties during Measure/Render (MenuItem, Border, ListBox) re-triggered a frame from
/// within every frame, pinning a core at 100% CPU while the app was completely idle.
/// </summary>
public class RenderLoopTests
{
    [Fact]
    public void SetValue_SameValue_DoesNotRaisePropertyChanged()
    {
        var tb = new TextBlock { Text = "hello" };

        int changes = 0;
        ((INotifyPropertyChanged)tb).PropertyChanged += (s, e) => changes++;

        tb.Text = "hello"; // no-op write
        Assert.Equal(0, changes);

        tb.Text = "world"; // real change
        Assert.Equal(1, changes);
    }

    [Fact]
    public void SetValue_SameValue_DoesNotInvalidateWindow()
    {
        var window = new TuiWindow();
        var tb = new TextBlock { Text = "hello" };
        window.Content = tb;

        int invalidations = 0;
        window.VisualChanged += (s, e) => invalidations++;

        tb.Text = "hello";
        Assert.Equal(0, invalidations);

        tb.Text = "world";
        Assert.Equal(1, invalidations);
    }

    [Fact]
    public void SetValue_NullForValueTypeProperty_Throws()
    {
        var element = new TextBlock();
        Assert.Throws<ArgumentException>(() => element.SetValue(UIElement.WidthProperty, null));
    }

    [Fact]
    public void SteadyStateRenderPass_DoesNotInvalidate()
    {
        // A tree containing all three controls that used to write dependency
        // properties from inside Measure/Render.
        var window = new TuiWindow();
        var stack = new StackPanel { Orientation = Orientation.Vertical };
        window.Content = stack;

        var menuBar = new MenuBar();
        var fileMenu = new MenuItem { Header = new TextBlock { Text = "File" } };
        fileMenu.Items.Add(new MenuItem { Header = new TextBlock { Text = "Open" } });
        menuBar.AddChild(fileMenu);
        stack.AddChild(menuBar);

        var border = new Border { Child = new TextBlock { Text = "content" } };
        stack.AddChild(border);

        var listBox = new ListBox { Width = 20, Height = 5 };
        listBox.Items.Add("one");
        listBox.Items.Add("two");
        stack.AddChild(listBox);

        var buffer = new VirtualBuffer(80, 24);

        // First pass may legitimately invalidate (initial focus, first-time color sync).
        RenderPass(window, buffer);
        RenderPass(window, buffer);

        // From here on the UI is settled: a render pass must be side-effect free.
        int invalidations = 0;
        window.VisualChanged += (s, e) => invalidations++;

        RenderPass(window, buffer);
        Assert.Equal(0, invalidations);
    }

    [Fact]
    public void NestedAutoScrollbars_SteadyStateRenderPass_DoesNotInvalidate()
    {
        // The outer viewer measures once at the full width and again one column narrower
        // after resolving its automatic vertical scrollbar. A wide fenced code block has
        // its own automatic horizontal scrollbar, so both speculative measurements must
        // not re-arm the render loop through intermediate scrollbar range changes.
        string longLine = new('x', 80);
        string markdown = "```text\n" + string.Join('\n', new[]
        {
            longLine, longLine, longLine, longLine,
            longLine, longLine, longLine, longLine
        }) + "\n```";

        var markdownView = new MarkdownView { Text = markdown };
        var document = Assert.IsType<FlowDocument>(markdownView.GetVisualChild(0));
        var codeBlock = Assert.IsType<MarkdownCodeBlock>(document.Children[0]);
        var outer = new ScrollViewer
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            Content = markdownView
        };
        var window = new TuiWindow { Content = outer };
        var buffer = new VirtualBuffer(30, 6);

        RenderPass(window, buffer);
        RenderPass(window, buffer);

        Assert.True(outer.IsVerticalScrollBarShown);
        Assert.True(codeBlock.IsHorizontalScrollBarShown);
        Assert.False(codeBlock.IsVerticalScrollBarShown);

        int invalidations = 0;
        window.VisualChanged += (s, e) => invalidations++;

        RenderPass(window, buffer);

        Assert.Equal(0, invalidations);
    }

    private static void RenderPass(TuiWindow window, VirtualBuffer buffer)
    {
        window.EnsureInitialFocus();
        window.Measure(new Size(buffer.Width, buffer.Height));
        window.Arrange(new Rect(0, 0, buffer.Width, buffer.Height));
        buffer.Clear();
        window.Render(buffer);
    }
}

public class BindingModeTests
{
    [Fact]
    public void TwoWayBinding_TargetChange_UpdatesSource()
    {
        var vm = new TestViewModel { TestProperty = "initial" };
        var tb = new TextBlock { DataContext = vm };
        tb.SetBinding(TextBlock.TextProperty, new Binding("TestProperty") { Mode = BindingMode.TwoWay });

        Assert.Equal("initial", tb.Text);

        tb.Text = "from target";
        Assert.Equal("from target", vm.TestProperty);

        vm.TestProperty = "from source";
        Assert.Equal("from source", tb.Text);
    }

    [Fact]
    public void OneTimeBinding_TransfersOnce_ThenIgnoresSourceChanges()
    {
        var vm = new TestViewModel { TestProperty = "initial" };
        var tb = new TextBlock { DataContext = vm };
        tb.SetBinding(TextBlock.TextProperty, new Binding("TestProperty") { Mode = BindingMode.OneTime });

        Assert.Equal("initial", tb.Text);

        vm.TestProperty = "changed";
        Assert.Equal("initial", tb.Text);
    }

    [Fact]
    public void OneWayToSourceBinding_PushesTargetValueToSource()
    {
        var vm = new TestViewModel { TestProperty = "initial" };
        var tb = new TextBlock { Text = "target value", DataContext = vm };
        tb.SetBinding(TextBlock.TextProperty, new Binding("TestProperty") { Mode = BindingMode.OneWayToSource });

        // Attach pushes the target's current value into the source...
        Assert.Equal("target value", vm.TestProperty);
        // ...and never writes the target.
        Assert.Equal("target value", tb.Text);

        tb.Text = "updated";
        Assert.Equal("updated", vm.TestProperty);

        vm.TestProperty = "source only";
        Assert.Equal("updated", tb.Text);
    }

    [Fact]
    public void SetBinding_ReplacesExistingBinding_OldSourceIsDetached()
    {
        var vm1 = new TestViewModel { TestProperty = "one" };
        var vm2 = new TestViewModel { TestProperty = "two" };
        var tb = new TextBlock();

        tb.SetBinding(TextBlock.TextProperty, new Binding("TestProperty") { Source = vm1 });
        Assert.Equal("one", tb.Text);

        tb.SetBinding(TextBlock.TextProperty, new Binding("TestProperty") { Source = vm2 });
        Assert.Equal("two", tb.Text);

        // The replaced binding must be fully detached: changes to the old source
        // may not write the target anymore.
        vm1.TestProperty = "one updated";
        Assert.Equal("two", tb.Text);

        vm2.TestProperty = "two updated";
        Assert.Equal("two updated", tb.Text);
    }
}

public class ScrollCoordinateTests
{
    [Fact]
    public void PointToScreen_AccountsForScrollOffset()
    {
        var window = new TuiWindow();
        var stack = new StackPanel { Orientation = Orientation.Vertical };
        var blocks = new TextBlock[10];
        for (int i = 0; i < blocks.Length; i++)
        {
            blocks[i] = new TextBlock { Text = $"line {i}" };
            stack.AddChild(blocks[i]);
        }

        var sv = new ScrollViewer
        {
            Content = stack,
            Height = 4,
            VerticalScrollBarVisibility = ScrollBarVisibility.Visible
        };
        window.Content = sv;

        window.Measure(new Size(40, 4));
        window.Arrange(new Rect(0, 0, 40, 4));

        // Unscrolled: line 5 sits at absolute row 5.
        Assert.Equal(5, blocks[5].PointToScreen(new Point(0, 0)).Y);

        // Scrolled down by 3: the same line is drawn 3 rows higher, and hit-test
        // local coordinates (PointFromScreen) must agree with what is on screen.
        sv.ScrollToVerticalOffset(3);
        Assert.Equal(2, blocks[5].PointToScreen(new Point(0, 0)).Y);
    }
}

public class TablePaginationClampTests
{
    [Fact]
    public void RemovingRows_ClampsCurrentPageToLastValidPage()
    {
        var table = new Table { PageSize = 2 };
        for (int i = 0; i < 6; i++)
        {
            table.AddRow($"row {i}");
        }

        Assert.Equal(3, table.TotalPages);
        table.CurrentPage = 2;
        Assert.Equal(2, table.CurrentPage);

        // Drop to 2 rows -> only page 0 remains.
        while (table.Rows.Count > 2)
        {
            table.Rows.RemoveAt(table.Rows.Count - 1);
        }

        Assert.Equal(1, table.TotalPages);
        Assert.Equal(0, table.CurrentPage);
    }
}
