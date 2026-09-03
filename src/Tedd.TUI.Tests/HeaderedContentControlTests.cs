using System;
using Xunit;
using Tedd.TUI.Controls;

namespace Tedd.TUI.Tests;

public class HeaderedContentControlTests
{
    [Fact]
    public void HeaderedContentControl_HasHeader_UpdatesWhenHeaderChanges()
    {
        var control = new HeaderedContentControl();

        Assert.False(control.HasHeader);

        control.Header = "A Header";
        Assert.True(control.HasHeader);

        control.Header = new TextBlock { Text = "Complex Header" };
        Assert.True(control.HasHeader);

        control.Header = null;
        Assert.False(control.HasHeader);
    }
}
