using Tedd.TUI.Controls;
using Xunit;

namespace Tedd.TUI.Tests;

public class ContentControlParityTests
{
    [Fact]
    public void HasContent_IsInitiallyFalse()
    {
        var control = new ContentControl();
        Assert.False(control.HasContent);
    }

    [Fact]
    public void SettingContent_UpdatesHasContent()
    {
        var control = new ContentControl();

        control.Content = "Some text";

        Assert.True(control.HasContent);
    }

    [Fact]
    public void ClearingContent_UpdatesHasContent()
    {
        var control = new ContentControl();
        control.Content = "Some text";
        Assert.True(control.HasContent);

        control.Content = null;

        Assert.False(control.HasContent);
    }
}