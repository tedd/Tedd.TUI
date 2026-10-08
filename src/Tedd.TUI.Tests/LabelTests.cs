using System;
using Xunit;
using Tedd.TUI.Controls;

namespace Tedd.TUI.Tests;

public class LabelTests
{
    [Fact]
    public void Label_InheritsContentControl()
    {
        var label = new Label();
        Assert.IsAssignableFrom<ContentControl>(label);
    }

    [Fact]
    public void Label_TargetProperty_SetAndGet()
    {
        var label = new Label();
        var target = new TextBox();

        label.Target = target;

        Assert.Equal(target, label.Target);
    }

    [Fact]
    public void Label_Focus_RedirectsToTarget()
    {
        var window = new TuiWindow();
        var panel = new StackPanel();
        window.Content = panel;

        var label = new Label { Content = "Targeted Label" };
        var textBox = new TextBox { Width = 10, Focusable = true };

        label.Target = textBox;

        panel.Children.Add(label);
        panel.Children.Add(textBox);

        // Process measure/arrange so elements have dimensions and are considered focusable/visible
        window.Measure(new Size(80, 24));
        window.Arrange(new Rect(0, 0, 80, 24));

        // Assert initial state
        Assert.False(label.IsFocused);
        Assert.False(textBox.IsFocused);

        // Attempt to focus label using TuiWindow method
        bool focusSuccess = window.SetFocus(label);

        // Focus should be true, but actual focus should land on the TextBox
        Assert.True(focusSuccess);

        // Let's verify our routing strategy: SetFocus on window delegates to the element.
        // Label's OnGotFocus is called, which calls Target.Focus(), which calls window.SetFocus(Target).
        Assert.False(label.IsFocused);
        Assert.True(textBox.IsFocused);
    }
}
