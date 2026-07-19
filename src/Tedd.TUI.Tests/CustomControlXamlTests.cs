using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Tedd.TUI.Tests.CustomControls;
using Tedd.TUI.Tests.TestInfrastructure;
using Xunit;

namespace Tedd.TUI.Tests;

/// <summary>
/// XAML compatibility for user-defined controls: resolving them through
/// clr-namespace/using xmlns mappings, binding to their custom dependency properties,
/// and verifying that overridden renderers drive the actual output while inherited
/// input behavior (clicks, keyboard) keeps working.
/// </summary>
public class CustomControlXamlTests
{
    private class ViewModel : INotifyPropertyChanged
    {
        private string _badge = "New";
        public string Badge { get => _badge; set { if (_badge != value) { _badge = value; OnPropertyChanged(); } } }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    // --- Type resolution ---

    [Fact]
    public void ClrNamespace_WithAssembly_ResolvesCustomControl()
    {
        var root = XamlLoader.Load(@"
<StackPanel xmlns:local='clr-namespace:Tedd.TUI.Tests.CustomControls;assembly=Tedd.TUI.Tests'>
    <local:BadgeControl Label='Hi' />
</StackPanel>");

        var panel = Assert.IsType<StackPanel>(root);
        var badge = Assert.IsType<BadgeControl>(panel.Children[0]);
        Assert.Equal("Hi", badge.Label);
    }

    [Fact]
    public void ClrNamespace_WithoutAssembly_ScansLoadedAssemblies()
    {
        var root = XamlLoader.Load(@"
<StackPanel xmlns:local='clr-namespace:Tedd.TUI.Tests.CustomControls'>
    <local:BadgeControl Label='NoAsm' />
</StackPanel>");

        var badge = Assert.IsType<BadgeControl>(((StackPanel)root).Children[0]);
        Assert.Equal("NoAsm", badge.Label);
    }

    [Fact]
    public void UsingSyntax_ResolvesCustomControl()
    {
        // WinUI-style xmlns
        var root = XamlLoader.Load(@"
<StackPanel xmlns:local='using:Tedd.TUI.Tests.CustomControls'>
    <local:BadgeControl Label='WinUI style' />
</StackPanel>");

        var badge = Assert.IsType<BadgeControl>(((StackPanel)root).Children[0]);
        Assert.Equal("WinUI style", badge.Label);
    }

    [Fact]
    public void UnknownClrNamespace_ThrowsTypeNotFound()
    {
        var ex = Assert.Throws<InvalidOperationException>(() => XamlLoader.Load(@"
<StackPanel xmlns:local='clr-namespace:Does.Not.Exist'>
    <local:BadgeControl />
</StackPanel>"));
        Assert.Contains("not found", ex.Message);
    }

    [Fact]
    public void CustomControl_XName_InjectsIntoController()
    {
        var controller = new BadgeController();
        XamlLoader.Load(@"
<StackPanel xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
            xmlns:local='clr-namespace:Tedd.TUI.Tests.CustomControls;assembly=Tedd.TUI.Tests'>
    <local:BadgeControl x:Name='TheBadge' Label='named' />
</StackPanel>", controller);

        Assert.NotNull(controller.TheBadge);
        Assert.Equal("named", controller.TheBadge!.Label);
    }

    private class BadgeController
    {
#pragma warning disable CS0649 // assigned via reflection by XamlLoader
        public BadgeControl? TheBadge;
#pragma warning restore CS0649
    }

    // --- Custom DP binding ---

    [Fact]
    public void CustomDependencyProperty_SupportsBinding()
    {
        var root = XamlLoader.Load(@"
<StackPanel xmlns:local='clr-namespace:Tedd.TUI.Tests.CustomControls;assembly=Tedd.TUI.Tests'>
    <local:BadgeControl Label='{Binding Badge}' />
</StackPanel>");

        var vm = new ViewModel { Badge = "42 unread" };
        root.DataContext = vm;

        var badge = (BadgeControl)((StackPanel)root).Children[0];
        Assert.Equal("42 unread", badge.Label);

        vm.Badge = "none";
        Assert.Equal("none", badge.Label);
    }

    // --- Overridden renderer drives output ---

    [Fact]
    public void CustomControl_RenderOverride_ProducesOutput()
    {
        var root = XamlLoader.Load(@"
<StackPanel xmlns:local='clr-namespace:Tedd.TUI.Tests.CustomControls;assembly=Tedd.TUI.Tests'>
    <local:BadgeControl Label='{Binding Badge}' />
</StackPanel>");
        root.DataContext = new ViewModel { Badge = "7" };

        var host = new ControlTestHost(root, 20, 3);
        string text = VirtualBufferAssertions.GetText(host.Render());

        Assert.Contains("[7]", text);
    }

    [Fact]
    public void CustomControl_RenderOverride_UpdatesWithBinding()
    {
        var root = XamlLoader.Load(@"
<StackPanel xmlns:local='clr-namespace:Tedd.TUI.Tests.CustomControls;assembly=Tedd.TUI.Tests'>
    <local:BadgeControl Label='{Binding Badge}' />
</StackPanel>");
        var vm = new ViewModel { Badge = "before" };
        root.DataContext = vm;

        var host = new ControlTestHost(root, 20, 3);
        Assert.Contains("[before]", VirtualBufferAssertions.GetText(host.Render()));

        vm.Badge = "after";
        host.Window.Measure(new Size(20, 3));
        host.Window.Arrange(new Rect(0, 0, 20, 3));
        string text = VirtualBufferAssertions.GetText(host.Render());
        Assert.Contains("[after]", text);
        Assert.DoesNotContain("[before]", text);
    }

    [Fact]
    public void InheritedButton_RenderOverride_ReplacesDefaultChrome()
    {
        var root = XamlLoader.Load(@"
<StackPanel xmlns:local='clr-namespace:Tedd.TUI.Tests.CustomControls;assembly=Tedd.TUI.Tests'>
    <local:FancyButton Content='Go' />
</StackPanel>");

        var button = Assert.IsType<FancyButton>(((StackPanel)root).Children[0]);
        var host = new ControlTestHost(root, 20, 3);
        string text = VirtualBufferAssertions.GetText(host.Render());

        Assert.Contains(">Go<", text);
        Assert.True(button.RenderCallCount > 0, "the overridden renderer must be invoked");
    }

    // --- Inherited input behavior still works on custom controls ---

    [Fact]
    public void InheritedButton_MouseClick_RaisesClickEvent()
    {
        var root = XamlLoader.Load(@"
<StackPanel xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
            xmlns:local='clr-namespace:Tedd.TUI.Tests.CustomControls;assembly=Tedd.TUI.Tests'>
    <local:FancyButton x:Name='Fancy' Content='Go' />
</StackPanel>");

        var button = (FancyButton)root.FindName("Fancy");
        int clicks = 0;
        button.Click += (s, e) => clicks++;

        var host = new ControlTestHost(root, 20, 3);
        host.Click(button, 1, 0);

        Assert.Equal(1, clicks);
    }

    [Fact]
    public void InheritedButton_KeyboardEnter_RaisesClickEvent()
    {
        var root = XamlLoader.Load(@"
<StackPanel xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
            xmlns:local='clr-namespace:Tedd.TUI.Tests.CustomControls;assembly=Tedd.TUI.Tests'>
    <local:FancyButton x:Name='Fancy' Content='Go' />
</StackPanel>");

        var button = (FancyButton)root.FindName("Fancy");
        int clicks = 0;
        button.Click += (s, e) => clicks++;

        var host = new ControlTestHost(root, 20, 3);
        button.Focus();
        host.PressKey(ConsoleKey.Enter);

        Assert.Equal(1, clicks);
    }

    [Fact]
    public void InheritedButton_ClickHandler_WiredFromXamlController()
    {
        var controller = new ClickController();
        var root = XamlLoader.Load(@"
<StackPanel xmlns:local='clr-namespace:Tedd.TUI.Tests.CustomControls;assembly=Tedd.TUI.Tests'>
    <local:FancyButton Content='Go' Click='OnGo' />
</StackPanel>", controller);

        var button = (FancyButton)((StackPanel)root).Children[0];
        var host = new ControlTestHost(root, 20, 3);
        host.Click(button, 1, 0);

        Assert.Equal(1, controller.GoCount);
    }

    private class ClickController
    {
        public int GoCount;

        private void OnGo(object sender, RoutedEventArgs e) => GoCount++;
    }
}
