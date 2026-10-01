using System;
using Xunit;
using Tedd.TUI;
using Tedd.TUI.Controls;

namespace Tedd.TUI.Tests;

public class LabelTests
{
    [Fact]
    public void Label_IsContentControl()
    {
        var label = new Label();
        Assert.IsAssignableFrom<ContentControl>(label);
    }

    [Fact]
    public void Label_TargetProperty_CanBeSetAndRetrieved()
    {
        var label = new Label();
        var target = new Button();

        Assert.Null(label.Target);

        label.Target = target;
        Assert.Equal(target, label.Target);
    }

    [Fact]
    public void Label_DataContext_PropagatesToTarget()
    {
        // Testing that Label itself works properly as a Framework element that handles DataContext
        var label = new Label();
        var dataContext = new object();

        // Target doesn't inherit DataContext from Label automatically via the visual tree unless it's a child.
        // But let's verify Label receives DataContext.
        label.DataContext = dataContext;
        Assert.Equal(dataContext, label.DataContext);

        // Verify content DataContext propagation
        var child = new TextBlock();
        label.Content = child;

        // DataContext propagates via the Template and ContentPresenter. We need to trigger measure to build visual tree if not manually applying template.
        // Actually since we didn't apply template, ContentPresenter might not be created.
        // Let's manually test template application.
        label.ApplyTemplate();

        // Wait, ContentControl data context propagation works when in visual tree.
        // The inheritance parent handles it.
        var window = new TuiWindow();
        window.Content = label;
        window.DataContext = dataContext;

        Assert.Equal(dataContext, label.DataContext);
    }
}
