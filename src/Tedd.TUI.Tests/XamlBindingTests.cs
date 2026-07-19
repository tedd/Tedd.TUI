using System;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using Tedd.TUI.Tests.TestInfrastructure;
using Xunit;

namespace Tedd.TUI.Tests;

/// <summary>
/// Tests for the {Binding} markup extension and friends in XamlLoader: anyone familiar
/// with WPF/XAML binding syntax should get the expected behavior, verified down to the
/// rendered output.
/// </summary>
public class XamlBindingTests
{
    private class ViewModel : INotifyPropertyChanged
    {
        private string _title = "Initial";
        private int _count;
        private string? _optional;
        private AddressVm _address = new();

        public string Title { get => _title; set { if (_title != value) { _title = value; OnPropertyChanged(); } } }
        public int Count { get => _count; set { if (_count != value) { _count = value; OnPropertyChanged(); } } }
        public string? Optional { get => _optional; set { if (_optional != value) { _optional = value; OnPropertyChanged(); } } }
        public AddressVm Address { get => _address; set { if (_address != value) { _address = value; OnPropertyChanged(); } } }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    private class AddressVm : INotifyPropertyChanged
    {
        private string _city = "Oslo";
        public string City { get => _city; set { if (_city != value) { _city = value; OnPropertyChanged(); } } }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    // --- Basic {Binding} ---

    [Fact]
    public void Binding_Positional_Path_BindsAndUpdates()
    {
        var element = XamlLoader.Load("<TextBlock Text='{Binding Title}' />");
        var tb = Assert.IsType<TextBlock>(element);

        var vm = new ViewModel { Title = "Hello" };
        tb.DataContext = vm;
        Assert.Equal("Hello", tb.Text);

        vm.Title = "Changed";
        Assert.Equal("Changed", tb.Text);
    }

    [Fact]
    public void Binding_PathKeyword_SameAsPositional()
    {
        var tb = (TextBlock)XamlLoader.Load("<TextBlock Text='{Binding Path=Title}' />");
        tb.DataContext = new ViewModel { Title = "Via Path=" };
        Assert.Equal("Via Path=", tb.Text);
    }

    [Fact]
    public void Binding_Empty_BindsToDataContextItself()
    {
        var tb = (TextBlock)XamlLoader.Load("<TextBlock Text='{Binding}' />");
        tb.DataContext = "raw string context";
        Assert.Equal("raw string context", tb.Text);
    }

    [Fact]
    public void Binding_NestedPath_ResolvesAndTracksLeaf()
    {
        var tb = (TextBlock)XamlLoader.Load("<TextBlock Text='{Binding Address.City}' />");
        var vm = new ViewModel();
        tb.DataContext = vm;
        Assert.Equal("Oslo", tb.Text);

        vm.Address.City = "Bergen";
        Assert.Equal("Bergen", tb.Text);
    }

    [Fact]
    public void Binding_IntSource_ConvertsToText()
    {
        var tb = (TextBlock)XamlLoader.Load("<TextBlock Text='{Binding Count}' />");
        tb.DataContext = new ViewModel { Count = 42 };
        Assert.Equal("42", tb.Text);
    }

    // --- Modes ---

    [Fact]
    public void Binding_OneTime_DoesNotTrackChanges()
    {
        var tb = (TextBlock)XamlLoader.Load("<TextBlock Text='{Binding Title, Mode=OneTime}' />");
        var vm = new ViewModel { Title = "Frozen" };
        tb.DataContext = vm;
        Assert.Equal("Frozen", tb.Text);

        vm.Title = "Thawed";
        Assert.Equal("Frozen", tb.Text);
    }

    [Fact]
    public void Binding_TextBox_DefaultsToTwoWay()
    {
        var box = (TextBox)XamlLoader.Load("<TextBox Text='{Binding Title}' />");
        var vm = new ViewModel { Title = "start" };
        box.DataContext = vm;
        Assert.Equal("start", box.Text);

        box.Text = "typed";
        Assert.Equal("typed", vm.Title);
    }

    [Fact]
    public void Binding_ExplicitTwoWay_OnSlider()
    {
        var slider = (Slider)XamlLoader.Load("<Slider Maximum='100' Value='{Binding Count, Mode=TwoWay}' />");
        var vm = new ViewModel { Count = 10 };
        slider.DataContext = vm;
        Assert.Equal(10, slider.Value);

        slider.Value = 60;
        Assert.Equal(60, vm.Count);

        vm.Count = 25;
        Assert.Equal(25, slider.Value);
    }

    [Fact]
    public void Binding_UpdateSourceTrigger_IsAcceptedForCompatibility()
    {
        var box = (TextBox)XamlLoader.Load("<TextBox Text='{Binding Title, UpdateSourceTrigger=PropertyChanged}' />");
        var vm = new ViewModel { Title = "x" };
        box.DataContext = vm;

        box.Text = "y";
        Assert.Equal("y", vm.Title);
    }

    // --- ElementName ---

    [Fact]
    public void Binding_ElementName_ForwardReference_ResolvesAfterLoad()
    {
        // The target references an element declared LATER in the document.
        var root = XamlLoader.Load(@"
<StackPanel xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <TextBlock x:Name='Mirror' Text='{Binding Text, ElementName=Source}' />
    <TextBlock x:Name='Source' Text='I came later' />
</StackPanel>");

        var mirror = (TextBlock)root.FindName("Mirror");
        Assert.Equal("I came later", mirror.Text);
    }

    [Fact]
    public void Binding_ElementName_TracksSourceChanges()
    {
        var root = XamlLoader.Load(@"
<StackPanel xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <Slider x:Name='Speed' Maximum='100' Value='30' />
    <TextBlock x:Name='Label' Text='{Binding Value, ElementName=Speed}' />
</StackPanel>");

        var label = (TextBlock)root.FindName("Label");
        var slider = (Slider)root.FindName("Speed");
        Assert.Equal("30", label.Text);

        slider.Value = 77;
        Assert.Equal("77", label.Text);
    }

    // --- RelativeSource ---

    [Fact]
    public void Binding_RelativeSourceSelf_BindsToOwnProperty()
    {
        var tb = (TextBlock)XamlLoader.Load(
            "<TextBlock Name='SelfNamed' Text='{Binding Name, RelativeSource={RelativeSource Self}}' />");
        Assert.Equal("SelfNamed", tb.Text);
    }

    [Fact]
    public void Binding_RelativeSourceFindAncestor_BindsToAncestorProperty()
    {
        var root = XamlLoader.Load(@"
<StackPanel Name='OuterPanel'>
    <Border>
        <TextBlock Name='Probe' Text='{Binding Name, RelativeSource={RelativeSource FindAncestor, AncestorType=StackPanel}}' />
    </Border>
</StackPanel>");

        var probe = (TextBlock)root.FindName("Probe");
        Assert.Equal("OuterPanel", probe.Text);
    }

    // --- FallbackValue / TargetNullValue / StringFormat ---

    [Fact]
    public void Binding_FallbackValue_UsedForBrokenPath()
    {
        var tb = (TextBlock)XamlLoader.Load("<TextBlock Text='{Binding Nonexistent, FallbackValue=n/a}' />");
        tb.DataContext = new ViewModel();
        Assert.Equal("n/a", tb.Text);
    }

    [Fact]
    public void Binding_FallbackValue_ConvertedToTargetType()
    {
        var slider = (Slider)XamlLoader.Load("<Slider Maximum='100' Value='{Binding Nonexistent, FallbackValue=15}' />");
        slider.DataContext = new ViewModel();
        Assert.Equal(15, slider.Value);
    }

    [Fact]
    public void Binding_TargetNullValue_UsedForNullValue()
    {
        var tb = (TextBlock)XamlLoader.Load("<TextBlock Text='{Binding Optional, TargetNullValue=(none)}' />");
        var vm = new ViewModel { Optional = null };
        tb.DataContext = vm;
        Assert.Equal("(none)", tb.Text);

        vm.Optional = "present";
        Assert.Equal("present", tb.Text);
    }

    [Fact]
    public void Binding_StringFormat_Quoted_CompositeFormat()
    {
        var tb = (TextBlock)XamlLoader.Load("<TextBlock Text=\"{Binding Count, StringFormat='{}{0} items'}\" />");
        tb.DataContext = new ViewModel { Count = 3 };
        Assert.Equal("3 items", tb.Text);
    }

    [Fact]
    public void Binding_StringFormat_BareSpecifier()
    {
        var tb = (TextBlock)XamlLoader.Load("<TextBlock Text='{Binding Count, StringFormat=D4}' />");
        tb.DataContext = new ViewModel { Count = 7 };
        Assert.Equal("0007", tb.Text);
    }

    // --- Converter / Source via StaticResource on the controller ---

    private class UpperCaseConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => value?.ToString()?.ToUpperInvariant() ?? "";
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => value?.ToString()?.ToLowerInvariant() ?? "";
    }

    private class Controller
    {
        public readonly UpperCaseConverter Shout = new();
        public ViewModel FixedSource { get; } = new() { Title = "from fixed source" };
    }

    [Fact]
    public void Binding_Converter_ResolvedFromController()
    {
        var controller = new Controller();
        var tb = (TextBlock)XamlLoader.Load(
            "<TextBlock Text='{Binding Title, Converter={StaticResource Shout}}' />", controller);
        tb.DataContext = new ViewModel { Title = "quiet" };
        Assert.Equal("QUIET", tb.Text);
    }

    [Fact]
    public void Binding_Source_ResolvedFromController()
    {
        var controller = new Controller();
        var tb = (TextBlock)XamlLoader.Load(
            "<TextBlock Text='{Binding Title, Source={StaticResource FixedSource}}' />", controller);
        // No DataContext needed; Source wins.
        Assert.Equal("from fixed source", tb.Text);
    }

    // --- Escapes and other extensions ---

    [Fact]
    public void EscapedBrace_IsLiteralText()
    {
        var tb = (TextBlock)XamlLoader.Load("<TextBlock Text='{}{Binding Title}' />");
        Assert.Equal("{Binding Title}", tb.Text);
    }

    [Fact]
    public void XNull_SetsPropertyToNull()
    {
        var tb = (TextBlock)XamlLoader.Load(
            "<TextBlock xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' Text='x' Background='{x:Null}' />");
        Assert.Null(tb.Background);
    }

    [Fact]
    public void Binding_OnNonDependencyProperty_ThrowsClearError()
    {
        // ScrollBar.Maximum is a plain CLR property with no MaximumProperty DP.
        var ex = Assert.Throws<InvalidOperationException>(() =>
            XamlLoader.Load("<ScrollBar Maximum='{Binding Count}' />"));
        Assert.Contains("Binding", ex.Message);
    }

    [Fact]
    public void UnknownMarkupExtension_Throws()
    {
        Assert.Throws<InvalidOperationException>(() =>
            XamlLoader.Load("<TextBlock Text='{DynamicResource Foo}' />"));
    }

    // --- DataContext flow and rendered output ---

    [Fact]
    public void RootDataContext_FlowsIntoWholeTree()
    {
        var root = XamlLoader.Load(@"
<StackPanel xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <TextBlock x:Name='First' Text='{Binding Title}' />
    <Border>
        <TextBlock x:Name='Deep' Text='{Binding Address.City}' />
    </Border>
</StackPanel>");

        var vm = new ViewModel { Title = "MVVM" };
        root.DataContext = vm;

        Assert.Equal("MVVM", ((TextBlock)root.FindName("First")).Text);
        Assert.Equal("Oslo", ((TextBlock)root.FindName("Deep")).Text);
    }

    [Fact]
    public void RenderedOutput_ReflectsBinding_AndUpdates()
    {
        var root = XamlLoader.Load(@"
<StackPanel Orientation='Vertical'>
    <TextBlock Text='{Binding Title}' />
    <TextBlock Text='{Binding Count, StringFormat=Count: {0}}' />
</StackPanel>");

        var vm = new ViewModel { Title = "First render", Count = 1 };
        root.DataContext = vm;

        var host = new ControlTestHost(root, 40, 5);
        string text = VirtualBufferAssertions.GetText(host.Render());
        Assert.Contains("First render", text);
        Assert.Contains("Count: 1", text);

        vm.Title = "Second render";
        vm.Count = 2;

        // Layout again in case content size changed, then re-render.
        host.Window.Measure(new Size(40, 5));
        host.Window.Arrange(new Rect(0, 0, 40, 5));
        text = VirtualBufferAssertions.GetText(host.Render());
        Assert.Contains("Second render", text);
        Assert.Contains("Count: 2", text);
        Assert.DoesNotContain("First render", text);
    }

    [Fact]
    public void RenderedOutput_ElementNameBinding_SliderToLabel()
    {
        var root = XamlLoader.Load(@"
<StackPanel xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' Orientation='Vertical'>
    <Slider x:Name='Volume' Maximum='100' Value='25' />
    <TextBlock Text='{Binding Value, ElementName=Volume, StringFormat=Volume: {0}}' />
</StackPanel>");

        var host = new ControlTestHost(root, 40, 5);
        string text = VirtualBufferAssertions.GetText(host.Render());
        Assert.Contains("Volume: 25", text);

        ((Slider)root.FindName("Volume")).Value = 80;
        host.Window.Measure(new Size(40, 5));
        host.Window.Arrange(new Rect(0, 0, 40, 5));
        text = VirtualBufferAssertions.GetText(host.Render());
        Assert.Contains("Volume: 80", text);
    }

    [Fact]
    public void DataContext_Attribute_CanBeBound()
    {
        // DataContext itself is bindable: scope a subtree to a sub-object.
        var root = XamlLoader.Load(@"
<StackPanel xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <Border DataContext='{Binding Address}'>
        <TextBlock x:Name='CityText' Text='{Binding City}' />
    </Border>
</StackPanel>");

        var vm = new ViewModel();
        root.DataContext = vm;

        Assert.Equal("Oslo", ((TextBlock)root.FindName("CityText")).Text);

        vm.Address.City = "Drammen";
        Assert.Equal("Drammen", ((TextBlock)root.FindName("CityText")).Text);
    }

    [Fact]
    public void AttachedProperty_WithBinding_ThrowsNotSupported()
    {
        Assert.Throws<NotSupportedException>(() =>
            XamlLoader.Load("<Grid><TextBlock Grid.Row='{Binding Count}' Text='x' /></Grid>"));
    }
}
