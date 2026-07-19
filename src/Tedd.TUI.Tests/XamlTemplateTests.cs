using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Tedd.TUI.Tests.TestInfrastructure;
using Xunit;

namespace Tedd.TUI.Tests;

/// <summary>
/// XAML DataTemplate / ItemsPanelTemplate support and ItemsSource bindings, verified
/// against the rendered output the way an MVVM XAML user would exercise them.
/// </summary>
public class XamlTemplateTests
{
    private class Person : INotifyPropertyChanged
    {
        private string _name = "";
        public string Name { get => _name; set { if (_name != value) { _name = value; OnPropertyChanged(); } } }

        public override string ToString() => Name;

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    private class ViewModel : INotifyPropertyChanged
    {
        private Person? _selected;

        public ObservableCollection<Person> People { get; } = new();
        public Person? Selected { get => _selected; set { if (_selected != value) { _selected = value; OnPropertyChanged(); } } }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    private static ViewModel MakeVm(params string[] names)
    {
        var vm = new ViewModel();
        foreach (var name in names) vm.People.Add(new Person { Name = name });
        return vm;
    }

    private static void Relayout(ControlTestHost host, int width, int height)
    {
        host.Window.Measure(new Size(width, height));
        host.Window.Arrange(new Rect(0, 0, width, height));
    }

    // --- Parsing ---

    [Fact]
    public void DataTemplate_Element_ParsesToDataTemplateInstance()
    {
        var listBox = (ListBox)XamlLoader.Load(@"
<ListBox>
    <ListBox.ItemTemplate>
        <DataTemplate>
            <TextBlock Text='{Binding Name}' />
        </DataTemplate>
    </ListBox.ItemTemplate>
</ListBox>");

        Assert.NotNull(listBox.ItemTemplate);
        var content = listBox.ItemTemplate.LoadContent(listBox);
        Assert.IsType<TextBlock>(content);
    }

    [Fact]
    public void DataTemplate_LoadContent_CreatesFreshTreePerCall()
    {
        var listBox = (ListBox)XamlLoader.Load(@"
<ListBox>
    <ListBox.ItemTemplate>
        <DataTemplate>
            <TextBlock Text='fixed' />
        </DataTemplate>
    </ListBox.ItemTemplate>
</ListBox>");

        var first = listBox.ItemTemplate.LoadContent(listBox);
        var second = listBox.ItemTemplate.LoadContent(listBox);
        Assert.NotSame(first, second);
    }

    [Fact]
    public void DataTemplate_WithMultipleRoots_Throws()
    {
        Assert.Throws<InvalidOperationException>(() => XamlLoader.Load(@"
<ListBox>
    <ListBox.ItemTemplate>
        <DataTemplate>
            <TextBlock Text='one' />
            <TextBlock Text='two' />
        </DataTemplate>
    </ListBox.ItemTemplate>
</ListBox>"));
    }

    [Fact]
    public void ItemsPanelTemplate_Element_ParsesAndCreatesPanel()
    {
        var listBox = (ListBox)XamlLoader.Load(@"
<ListBox>
    <ListBox.ItemsPanel>
        <ItemsPanelTemplate>
            <WrapPanel />
        </ItemsPanelTemplate>
    </ListBox.ItemsPanel>
</ListBox>");

        Assert.NotNull(listBox.ItemsPanel);
        Assert.IsType<WrapPanel>(listBox.ItemsPanel.LoadContent(listBox));
    }

    // --- ItemsSource binding ---

    [Fact]
    public void ItemsSource_Binding_PopulatesItems()
    {
        var listBox = (ListBox)XamlLoader.Load("<ListBox ItemsSource='{Binding People}' />");
        var vm = MakeVm("Alice", "Bob");
        listBox.DataContext = vm;

        Assert.Equal(2, listBox.Items.Count);
        Assert.Same(vm.People[0], listBox.Items[0]);
    }

    [Fact]
    public void ItemsSource_ObservableAddRemove_SyncsItems()
    {
        var listBox = (ListBox)XamlLoader.Load("<ListBox ItemsSource='{Binding People}' />");
        var vm = MakeVm("Alice");
        listBox.DataContext = vm;

        vm.People.Add(new Person { Name = "Bob" });
        Assert.Equal(2, listBox.Items.Count);

        vm.People.RemoveAt(0);
        Assert.Single(listBox.Items);
        Assert.Equal("Bob", ((Person)listBox.Items[0]!).Name);
    }

    // --- Rendered output ---

    [Fact]
    public void ItemsSource_WithDataTemplate_RendersItemContent()
    {
        var root = XamlLoader.Load(@"
<ListBox ItemsSource='{Binding People}'>
    <ListBox.ItemTemplate>
        <DataTemplate>
            <TextBlock Text='{Binding Name}' />
        </DataTemplate>
    </ListBox.ItemTemplate>
</ListBox>");
        root.DataContext = MakeVm("Alice", "Bob");

        var host = new ControlTestHost(root, 20, 6);
        string text = VirtualBufferAssertions.GetText(host.Render());

        Assert.Contains("Alice", text);
        Assert.Contains("Bob", text);
    }

    [Fact]
    public void ItemsSource_CollectionChange_UpdatesRenderedOutput()
    {
        var root = XamlLoader.Load(@"
<ListBox ItemsSource='{Binding People}'>
    <ListBox.ItemTemplate>
        <DataTemplate>
            <TextBlock Text='{Binding Name}' />
        </DataTemplate>
    </ListBox.ItemTemplate>
</ListBox>");
        var vm = MakeVm("Alice");
        root.DataContext = vm;

        var host = new ControlTestHost(root, 20, 6);
        Assert.Contains("Alice", VirtualBufferAssertions.GetText(host.Render()));

        vm.People.Add(new Person { Name = "Carol" });
        Relayout(host, 20, 6);
        string text = VirtualBufferAssertions.GetText(host.Render());
        Assert.Contains("Alice", text);
        Assert.Contains("Carol", text);

        vm.People.RemoveAt(0);
        Relayout(host, 20, 6);
        text = VirtualBufferAssertions.GetText(host.Render());
        Assert.DoesNotContain("Alice", text);
        Assert.Contains("Carol", text);
    }

    [Fact]
    public void ItemPropertyChange_IsReflectedInRenderedOutput()
    {
        var root = XamlLoader.Load(@"
<ListBox ItemsSource='{Binding People}'>
    <ListBox.ItemTemplate>
        <DataTemplate>
            <TextBlock Text='{Binding Name}' />
        </DataTemplate>
    </ListBox.ItemTemplate>
</ListBox>");
        var vm = MakeVm("Old Name");
        root.DataContext = vm;

        var host = new ControlTestHost(root, 20, 6);
        Assert.Contains("Old Name", VirtualBufferAssertions.GetText(host.Render()));

        vm.People[0].Name = "New Name";
        string text = VirtualBufferAssertions.GetText(host.Render());
        Assert.Contains("New Name", text);
        Assert.DoesNotContain("Old Name", text);
    }

    [Fact]
    public void DisplayMemberPath_RendersMemberValue()
    {
        var root = XamlLoader.Load("<ListBox ItemsSource='{Binding People}' DisplayMemberPath='Name' />");
        root.DataContext = MakeVm("Display Me");

        var host = new ControlTestHost(root, 20, 6);
        Assert.Contains("Display Me", VirtualBufferAssertions.GetText(host.Render()));
    }

    // --- Selection with ItemsSource ---

    [Fact]
    public void SelectedItem_Binding_WithItemsSource_TwoWay()
    {
        var listBox = (ListBox)XamlLoader.Load(
            "<ListBox ItemsSource='{Binding People}' SelectedItem='{Binding Selected}' />");
        var vm = MakeVm("Alice", "Bob");
        listBox.DataContext = vm;

        listBox.SelectedIndex = 1;
        Assert.Same(vm.People[1], vm.Selected);

        vm.Selected = vm.People[0];
        Assert.Equal(0, listBox.SelectedIndex);
    }

    // --- ContentControl.ContentTemplate ---

    [Fact]
    public void ContentTemplate_AppliesToBoundContent()
    {
        var root = XamlLoader.Load(@"
<ContentControl Content='{Binding Selected}'>
    <ContentControl.ContentTemplate>
        <DataTemplate>
            <TextBlock Text='{Binding Name}' />
        </DataTemplate>
    </ContentControl.ContentTemplate>
</ContentControl>");

        var vm = new ViewModel { Selected = new Person { Name = "Templated" } };
        root.DataContext = vm;

        var host = new ControlTestHost(root, 20, 3);
        Assert.Contains("Templated", VirtualBufferAssertions.GetText(host.Render()));
    }
}
