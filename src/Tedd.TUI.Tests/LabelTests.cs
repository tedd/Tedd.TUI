using System;
using Xunit;
using Tedd.TUI.Controls;
using Tedd.TUI;

namespace Tedd.TUI.Tests;

public class LabelTests
{
    [Fact]
    public void Label_InheritsFromContentControl()
    {
        var label = new Label();
        Assert.IsAssignableFrom<ContentControl>(label);
    }

    [Fact]
    public void Label_TargetProperty_DefaultsToNull()
    {
        var label = new Label();
        Assert.Null(label.Target);
    }

    [Fact]
    public void Label_TargetProperty_CanBeSet()
    {
        var label = new Label();
        var target = new TextBox();

        label.Target = target;

        Assert.Same(target, label.Target);
    }

    [Fact]
    public void Label_TargetProperty_SetViaDependencyProperty()
    {
        var label = new Label();
        var target = new TextBox();

        label.SetValue(Label.TargetProperty, target);

        Assert.Same(target, label.Target);
        Assert.Same(target, label.GetValue(Label.TargetProperty));
    }
}
