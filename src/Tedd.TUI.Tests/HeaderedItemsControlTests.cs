using Xunit;

namespace Tedd.TUI.Tests;

public class HeaderedItemsControlTests
{
    private class TestHeaderedItemsControl : HeaderedItemsControl
    {
    }

    [Fact]
    public void HasHeader_IsInitiallyFalse()
    {
        var control = new TestHeaderedItemsControl();
        Assert.False(control.HasHeader);
    }

    [Fact]
    public void SettingHeader_UpdatesHasHeader()
    {
        var control = new TestHeaderedItemsControl();
        control.Header = "Test";

        Assert.Equal("Test", control.Header);
        Assert.True(control.HasHeader);
    }

    [Fact]
    public void ClearingHeader_UpdatesHasHeader()
    {
        var control = new TestHeaderedItemsControl();
        control.Header = "Test";
        Assert.True(control.HasHeader);

        control.Header = null;
        Assert.False(control.HasHeader);
    }

    [Fact]
    public void SettingHeaderTemplate_StoresValue()
    {
        var control = new TestHeaderedItemsControl();
        var template = new object();
        control.HeaderTemplate = template;

        Assert.Equal(template, control.HeaderTemplate);
    }
}
