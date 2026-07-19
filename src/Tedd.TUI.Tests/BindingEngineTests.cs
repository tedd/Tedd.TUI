using System;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using Xunit;

namespace Tedd.TUI.Tests;

/// <summary>
/// Engine-level tests for the WPF-compatible binding features: ElementName sources,
/// nested property paths, StringFormat, TargetNullValue, automatic target-type
/// conversion, and BindsTwoWayByDefault mode resolution.
/// </summary>
public class BindingEngineTests
{
    private class ObservableObject : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool Set<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }
    }

    private class PersonVm : ObservableObject
    {
        private string _name = "";
        private int _age;
        private AddressVm? _address;

        public string Name { get => _name; set => Set(ref _name, value); }
        public int Age { get => _age; set => Set(ref _age, value); }
        public AddressVm? Address { get => _address; set => Set(ref _address, value); }
    }

    private class AddressVm : ObservableObject
    {
        private string _city = "";
        public string City { get => _city; set => Set(ref _city, value); }
    }

    // --- ElementName ---

    [Fact]
    public void ElementName_Binding_ReadsFromNamedElement()
    {
        var source = new TextBlock { Name = "SourceText", Text = "from source" };
        var target = new TextBlock();
        var panel = new StackPanel();
        panel.AddChild(source);
        panel.AddChild(target);

        target.SetBinding(TextBlock.TextProperty, new Binding("Text") { ElementName = "SourceText" });

        Assert.Equal("from source", target.Text);
    }

    [Fact]
    public void ElementName_Binding_TracksSourceElementChanges()
    {
        var slider = new Slider { Name = "MySlider", Maximum = 100, Value = 10 };
        var label = new TextBlock();
        var panel = new StackPanel();
        panel.AddChild(slider);
        panel.AddChild(label);

        label.SetBinding(TextBlock.TextProperty, new Binding("Value") { ElementName = "MySlider" });
        Assert.Equal("10", label.Text);

        slider.Value = 42;
        Assert.Equal("42", label.Text);
    }

    [Fact]
    public void ElementName_Binding_TwoWay_WritesBackToNamedElement()
    {
        var source = new TextBox { Name = "Input", Text = "initial" };
        var mirror = new TextBox();
        var panel = new StackPanel();
        panel.AddChild(source);
        panel.AddChild(mirror);

        mirror.SetBinding(TextBox.TextProperty, new Binding("Text") { ElementName = "Input", Mode = BindingMode.TwoWay });
        Assert.Equal("initial", mirror.Text);

        mirror.Text = "changed in mirror";
        Assert.Equal("changed in mirror", source.Text);
    }

    // --- Nested property paths ---

    [Fact]
    public void NestedPath_ResolvesInitialValue()
    {
        var vm = new PersonVm { Address = new AddressVm { City = "Oslo" } };
        var tb = new TextBlock { DataContext = vm };

        tb.SetBinding(TextBlock.TextProperty, new Binding("Address.City"));

        Assert.Equal("Oslo", tb.Text);
    }

    [Fact]
    public void NestedPath_LeafChange_UpdatesTarget()
    {
        var vm = new PersonVm { Address = new AddressVm { City = "Oslo" } };
        var tb = new TextBlock { DataContext = vm };
        tb.SetBinding(TextBlock.TextProperty, new Binding("Address.City"));

        vm.Address!.City = "Bergen";

        Assert.Equal("Bergen", tb.Text);
    }

    [Fact]
    public void NestedPath_IntermediateSwap_UpdatesTargetAndTracksNewLeaf()
    {
        var vm = new PersonVm { Address = new AddressVm { City = "Oslo" } };
        var tb = new TextBlock { DataContext = vm };
        tb.SetBinding(TextBlock.TextProperty, new Binding("Address.City"));

        var newAddress = new AddressVm { City = "Trondheim" };
        vm.Address = newAddress;
        Assert.Equal("Trondheim", tb.Text);

        // The binding must now be listening to the new leaf object.
        newAddress.City = "Stavanger";
        Assert.Equal("Stavanger", tb.Text);
    }

    [Fact]
    public void NestedPath_OldLeafChange_AfterSwap_IsIgnored()
    {
        var oldAddress = new AddressVm { City = "Oslo" };
        var vm = new PersonVm { Address = oldAddress };
        var tb = new TextBlock { DataContext = vm };
        tb.SetBinding(TextBlock.TextProperty, new Binding("Address.City"));

        vm.Address = new AddressVm { City = "Trondheim" };
        oldAddress.City = "Ghost town";

        Assert.Equal("Trondheim", tb.Text);
    }

    [Fact]
    public void NestedPath_NullIntermediate_UsesFallbackValue()
    {
        var vm = new PersonVm { Address = null };
        var tb = new TextBlock { DataContext = vm };

        tb.SetBinding(TextBlock.TextProperty, new Binding("Address.City") { FallbackValue = "(no address)" });

        Assert.Equal("(no address)", tb.Text);
    }

    [Fact]
    public void NestedPath_NullIntermediate_ThenPopulated_UpdatesTarget()
    {
        var vm = new PersonVm { Address = null };
        var tb = new TextBlock { DataContext = vm };
        tb.SetBinding(TextBlock.TextProperty, new Binding("Address.City") { FallbackValue = "(no address)" });

        vm.Address = new AddressVm { City = "Oslo" };

        Assert.Equal("Oslo", tb.Text);
    }

    [Fact]
    public void NestedPath_TwoWay_WritesLeafProperty()
    {
        var vm = new PersonVm { Address = new AddressVm { City = "Oslo" } };
        var box = new TextBox { DataContext = vm };
        box.SetBinding(TextBox.TextProperty, new Binding("Address.City") { Mode = BindingMode.TwoWay });

        Assert.Equal("Oslo", box.Text);

        box.Text = "Bergen";
        Assert.Equal("Bergen", vm.Address!.City);
    }

    // --- StringFormat / TargetNullValue ---

    [Fact]
    public void StringFormat_CompositeFormat_IsApplied()
    {
        var vm = new PersonVm { Age = 30 };
        var tb = new TextBlock { DataContext = vm };

        tb.SetBinding(TextBlock.TextProperty, new Binding("Age") { StringFormat = "Age: {0}" });

        Assert.Equal("Age: 30", tb.Text);
    }

    [Fact]
    public void StringFormat_BareSpecifier_FormatsValue()
    {
        var vm = new PersonVm { Age = 1234 };
        var tb = new TextBlock { DataContext = vm };

        tb.SetBinding(TextBlock.TextProperty, new Binding("Age") { StringFormat = "N0" });

        Assert.Equal(1234.ToString("N0", CultureInfo.CurrentCulture), tb.Text);
    }

    [Fact]
    public void StringFormat_UpdatesWithSource()
    {
        var vm = new PersonVm { Age = 1 };
        var tb = new TextBlock { DataContext = vm };
        tb.SetBinding(TextBlock.TextProperty, new Binding("Age") { StringFormat = "{0} year(s)" });

        vm.Age = 5;

        Assert.Equal("5 year(s)", tb.Text);
    }

    [Fact]
    public void TargetNullValue_UsedWhenPathResolvesToNull()
    {
        var vm = new PersonVm { Address = new AddressVm() };
        // City resolves; make a null-valued source instead.
        var holder = new NullableHolder { Value = null };
        var tb = new TextBlock { DataContext = holder };

        tb.SetBinding(TextBlock.TextProperty, new Binding("Value") { TargetNullValue = "(none)" });

        Assert.Equal("(none)", tb.Text);
    }

    private class NullableHolder : ObservableObject
    {
        private string? _value;
        public string? Value { get => _value; set => Set(ref _value, value); }
    }

    // --- BindsTwoWayByDefault ---

    [Fact]
    public void TextBoxText_DefaultMode_IsTwoWay()
    {
        var vm = new PersonVm { Name = "before" };
        var box = new TextBox { DataContext = vm };
        box.SetBinding(TextBox.TextProperty, new Binding("Name"));

        Assert.Equal("before", box.Text);

        // Simulates user input mutating the target property.
        box.Text = "after";
        Assert.Equal("after", vm.Name);
    }

    [Fact]
    public void TextBoxText_ExplicitOneWay_DoesNotWriteBack()
    {
        var vm = new PersonVm { Name = "before" };
        var box = new TextBox { DataContext = vm };
        box.SetBinding(TextBox.TextProperty, new Binding("Name") { Mode = BindingMode.OneWay });

        box.Text = "after";

        Assert.Equal("before", vm.Name);
    }

    [Fact]
    public void TextBlockText_DefaultMode_IsOneWay()
    {
        var vm = new PersonVm { Name = "before" };
        var tb = new TextBlock { DataContext = vm };
        tb.SetBinding(TextBlock.TextProperty, new Binding("Name"));

        tb.Text = "local change";

        Assert.Equal("before", vm.Name);
    }

    [Fact]
    public void ToggleButtonIsChecked_DefaultMode_IsTwoWay()
    {
        var vm = new FlagVm { Flag = false };
        var cb = new CheckBox { DataContext = vm };
        cb.SetBinding(ToggleButton.IsCheckedProperty, new Binding("Flag"));

        cb.IsChecked = true;

        Assert.True(vm.Flag);
    }

    private class FlagVm : ObservableObject
    {
        private bool _flag;
        public bool Flag { get => _flag; set => Set(ref _flag, value); }
    }

    [Fact]
    public void SliderValue_DefaultMode_IsTwoWay()
    {
        var vm = new PersonVm { Age = 10 };
        var slider = new Slider { Maximum = 100, DataContext = vm };
        slider.SetBinding(Slider.ValueProperty, new Binding("Age"));

        Assert.Equal(10, slider.Value);

        slider.Value = 55;
        Assert.Equal(55, vm.Age);

        vm.Age = 70;
        Assert.Equal(70, slider.Value);
    }

    // --- Selector selection binding ---

    [Fact]
    public void ListBox_SelectedIndex_Binding_IsTwoWayByDefault()
    {
        var vm = new SelectionVm { Index = 1 };
        var listBox = new ListBox { DataContext = vm };
        listBox.Items.Add("A");
        listBox.Items.Add("B");
        listBox.Items.Add("C");

        listBox.SetBinding(Selector.SelectedIndexProperty, new Binding("Index"));
        Assert.Equal(1, listBox.SelectedIndex);
        Assert.Equal("B", listBox.SelectedItem);

        // Control -> VM
        listBox.SelectedIndex = 2;
        Assert.Equal(2, vm.Index);

        // VM -> control
        vm.Index = 0;
        Assert.Equal(0, listBox.SelectedIndex);
        Assert.Equal("A", listBox.SelectedItem);
    }

    [Fact]
    public void ListBox_SelectedItem_Binding_IsTwoWayByDefault()
    {
        var vm = new SelectionVm();
        var listBox = new ListBox { DataContext = vm };
        listBox.Items.Add("A");
        listBox.Items.Add("B");

        listBox.SetBinding(Selector.SelectedItemProperty, new Binding("Item"));

        listBox.SelectedItem = "B";
        Assert.Equal("B", vm.Item);

        vm.Item = "A";
        Assert.Equal("A", listBox.SelectedItem);
        Assert.Equal(0, listBox.SelectedIndex);
    }

    [Fact]
    public void ListBox_SelectedIndex_ChangingSelection_KeepsItemInSync()
    {
        var listBox = new ListBox();
        listBox.Items.Add("A");
        listBox.Items.Add("B");

        var itemLabel = new TextBlock();
        var panel = new StackPanel();
        listBox.Name = "List";
        panel.AddChild(listBox);
        panel.AddChild(itemLabel);

        itemLabel.SetBinding(TextBlock.TextProperty, new Binding("SelectedItem") { ElementName = "List" });

        listBox.SelectedIndex = 1;
        Assert.Equal("B", itemLabel.Text);
    }

    private class SelectionVm : ObservableObject
    {
        private int _index = -1;
        private object? _item;
        public int Index { get => _index; set => Set(ref _index, value); }
        public object? Item { get => _item; set => Set(ref _item, value); }
    }

    // --- Mode semantics ---

    [Fact]
    public void OneTime_Binding_DoesNotTrackSourceChanges()
    {
        var vm = new PersonVm { Name = "initial" };
        var tb = new TextBlock { DataContext = vm };
        tb.SetBinding(TextBlock.TextProperty, new Binding("Name") { Mode = BindingMode.OneTime });

        Assert.Equal("initial", tb.Text);

        vm.Name = "changed";
        Assert.Equal("initial", tb.Text);
    }

    [Fact]
    public void OneWayToSource_PushesTargetValueToSource()
    {
        var vm = new PersonVm { Name = "vm value" };
        var box = new TextBox { DataContext = vm, Text = "target value" };
        box.SetBinding(TextBox.TextProperty, new Binding("Name") { Mode = BindingMode.OneWayToSource });

        // Initial transfer goes target -> source.
        Assert.Equal("target value", vm.Name);

        box.Text = "updated";
        Assert.Equal("updated", vm.Name);

        // Source changes never flow back to the target.
        vm.Name = "should not appear";
        Assert.Equal("updated", box.Text);
    }

    [Fact]
    public void TwoWay_Binding_NoInfiniteLoop_WhenConverterDoesNotRoundTrip()
    {
        var vm = new PersonVm { Age = 5 };
        var box = new TextBox { DataContext = vm };
        box.SetBinding(TextBox.TextProperty, new Binding("Age") { Mode = BindingMode.TwoWay });

        Assert.Equal("5", box.Text);

        box.Text = "42";
        Assert.Equal(42, vm.Age);

        vm.Age = 7;
        Assert.Equal("7", box.Text);
    }

    // --- Automatic conversion ---

    [Fact]
    public void AutoConvert_DoubleSource_ToIntTarget()
    {
        var vm = new DoubleVm { Number = 41.7 };
        var progressBar = new ProgressBar { DataContext = vm };
        progressBar.SetBinding(ProgressBar.ValueProperty, new Binding("Number"));

        Assert.Equal(42, progressBar.Value);
    }

    private class DoubleVm : ObservableObject
    {
        private double _number;
        public double Number { get => _number; set => Set(ref _number, value); }
    }

    [Fact]
    public void AutoConvert_NullToNonNullableValueType_UsesPropertyDefault()
    {
        var vm = new NullableIntVm { Number = 9 };
        var progressBar = new ProgressBar { DataContext = vm };
        progressBar.SetBinding(ProgressBar.ValueProperty, new Binding("Number"));
        Assert.Equal(9, progressBar.Value);

        vm.Number = null;
        Assert.Equal(0, progressBar.Value);
    }

    private class NullableIntVm : ObservableObject
    {
        private int? _number;
        public int? Number { get => _number; set => Set(ref _number, value); }
    }

    [Fact]
    public void AutoConvert_StringSource_ToEnumTarget()
    {
        var vm = new EnumSourceVm { OrientationName = "Horizontal" };
        var panel = new StackPanel { DataContext = vm };
        panel.SetBinding(StackPanel.OrientationProperty, new Binding("OrientationName"));

        Assert.Equal(Orientation.Horizontal, panel.Orientation);
    }

    private class EnumSourceVm : ObservableObject
    {
        private string _orientationName = "";
        public string OrientationName { get => _orientationName; set => Set(ref _orientationName, value); }
    }

    // --- Lifecycle ---

    [Fact]
    public void ReplacingBinding_DetachesOldSourceSubscription()
    {
        var vm1 = new PersonVm { Name = "one" };
        var vm2 = new PersonVm { Name = "two" };
        var tb = new TextBlock { DataContext = vm1 };

        tb.SetBinding(TextBlock.TextProperty, new Binding("Name"));
        Assert.Equal("one", tb.Text);

        tb.SetBinding(TextBlock.TextProperty, new Binding("Name") { Source = vm2 });
        Assert.Equal("two", tb.Text);

        // Old source must no longer drive the target.
        vm1.Name = "ghost";
        Assert.Equal("two", tb.Text);
    }

    [Fact]
    public void DataContextChange_RehooksNestedPathChain()
    {
        var vm1 = new PersonVm { Address = new AddressVm { City = "Oslo" } };
        var vm2 = new PersonVm { Address = new AddressVm { City = "Bergen" } };
        var tb = new TextBlock { DataContext = vm1 };
        tb.SetBinding(TextBlock.TextProperty, new Binding("Address.City"));

        tb.DataContext = vm2;
        Assert.Equal("Bergen", tb.Text);

        // Only the new chain is live.
        vm1.Address!.City = "Ghost";
        Assert.Equal("Bergen", tb.Text);

        vm2.Address!.City = "Tromsø";
        Assert.Equal("Tromsø", tb.Text);
    }
}
