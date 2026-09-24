using System;
using Xunit;
using Tedd.TUI;
using Tedd.TUI.Controls;

namespace Tedd.TUI.Tests;

public class HasContentPropertyTests
{
    [Fact]
    public void HasContent_UpdatesWhenContentChanges()
    {
        var control = new ContentControl();
        Assert.False(control.HasContent);

        control.Content = "Test";
        Assert.True(control.HasContent);

        control.Content = null;
        Assert.False(control.HasContent);
    }
}
