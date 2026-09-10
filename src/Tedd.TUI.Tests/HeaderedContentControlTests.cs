using System;
using Xunit;
using Tedd.TUI.Controls;

namespace Tedd.TUI.Tests;

public class HeaderedContentControlTests
{
    private class TestHeaderedContentControl : HeaderedContentControl { }

    [Fact]
    public void HasHeader_IsInitiallyFalse()
    {
        var control = new TestHeaderedContentControl();
        Assert.False(control.HasHeader);
    }

    [Fact]
    public void SettingHeader_UpdatesHasHeader()
    {
        var control = new TestHeaderedContentControl();
        control.Header = "Some Header";
        Assert.True(control.HasHeader);
    }

    [Fact]
    public void ClearingHeader_UpdatesHasHeader()
    {
        var control = new TestHeaderedContentControl();
        control.Header = "Some Header";
        Assert.True(control.HasHeader);

        control.Header = null!;
        Assert.False(control.HasHeader);
    }
}
