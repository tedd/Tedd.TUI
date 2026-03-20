using System;
using Tedd.TUI;
using Xunit;

namespace Tedd.TUI.Tests;

public class UserControlTests
{
    [Fact]
    public void UserControl_InheritsFromContentControl()
    {
        var uc = new UserControl();
        Assert.IsAssignableFrom<ContentControl>(uc);
        Assert.False(uc.Focusable); // By default UIElement is Focusable = false
    }

    [Fact]
    public void UserControl_MeasureArrange_RespectsContentSize()
    {
        var uc = new UserControl();
        var content = new Canvas { Width = 20, Height = 10 };
        uc.Content = content;

        uc.Measure(new Size(100, 100));
        Assert.Equal(20, uc.DesiredSize.Width);
        Assert.Equal(10, uc.DesiredSize.Height);

        uc.Arrange(new Rect(0, 0, 50, 50));
        Assert.Equal(50, uc.RenderSize.Width);
        Assert.Equal(50, uc.RenderSize.Height);

        // Wait, ContentPresenter inside UserControl handles the measure/arrange
        // ContentPresenter gets 50x50. The inner Canvas gets arranged at 50x50.
        uc.ApplyTemplate(); // Make sure template is applied
        uc.Measure(new Size(100, 100));
        uc.Arrange(new Rect(0, 0, 50, 50));

        Assert.Equal(1, uc.VisualChildrenCount);
        var cp = uc.GetVisualChild(0) as ContentPresenter;
        Assert.NotNull(cp);
        Assert.Equal(1, cp.VisualChildrenCount);
        var child = cp.GetVisualChild(0);
        Assert.Same(content, child);
    }

    [Fact]
    public void UserControl_DataContext_PropagatesToContent()
    {
        var uc = new UserControl();
        var content = new TextBlock();
        uc.Content = content;

        uc.ApplyTemplate();
        var dataContext = new object();
        uc.DataContext = dataContext;

        var cp = uc.GetVisualChild(0) as ContentPresenter;
        Assert.NotNull(cp);
        var child = cp.GetVisualChild(0) as TextBlock;
        Assert.NotNull(child);

        // ContentPresenter propagates DataContext or Content inherits it.
        // Let's verify DataContext inheritance.
        Assert.Same(dataContext, child.DataContext);
    }
}
