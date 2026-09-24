using System;
using Xunit;
using Tedd.TUI;
using Tedd.TUI.Controls;

namespace Tedd.TUI.Tests;

public class HeaderedContentControlTests
{
    // Need a concrete implementation since it might not be instantiable directly if it's abstract
    // Actually, looking at the source, HeaderedContentControl is not abstract.

    [Fact]
    public void HasHeader_UpdatesWhenHeaderChanges()
    {
        var control = new HeaderedContentControl();
        Assert.False(control.HasHeader);

        control.Header = "Test Header";
        Assert.True(control.HasHeader);

        control.Header = null;
        Assert.False(control.HasHeader);
    }
}
