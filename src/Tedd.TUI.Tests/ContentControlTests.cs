using System;
using Tedd.TUI;
using Tedd.TUI.Controls;
using Xunit;

namespace Tedd.TUI.Tests;

public class ContentControlHasContentTests
{
    [Fact]
    public void HasContent_UpdatesAutomatically_WhenContentChanges()
    {
        var control = new ContentControl();

        Assert.False(control.HasContent);

        control.Content = "Test";
        Assert.True(control.HasContent);

        control.Content = null;
        Assert.False(control.HasContent);
    }

    [Fact]
    public void HasHeader_UpdatesAutomatically_WhenHeaderChanges()
    {
        var control = new HeaderedContentControl();

        Assert.False(control.HasHeader);

        control.Header = "Header Test";
        Assert.True(control.HasHeader);

        control.Header = null;
        Assert.False(control.HasHeader);
    }
}
