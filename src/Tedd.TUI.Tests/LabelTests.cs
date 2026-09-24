using System;
using Tedd.TUI;
using Tedd.TUI.Controls;
using Xunit;

namespace Tedd.TUI.Tests;

public class LabelTests
{
    [Fact]
    public void Label_HasTargetProperty()
    {
        var label = new Label();
        var targetElement = new TextBox();

        label.Target = targetElement;

        Assert.Equal(targetElement, label.Target);
    }

    [Fact]
    public void Label_InheritsContentControl()
    {
        var label = new Label();
        label.Content = "Label Text";

        Assert.Equal("Label Text", label.Content);
        Assert.True(label.HasContent);
    }
}
