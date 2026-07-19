using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Tedd.TUI.Tests.TestInfrastructure;
using Xunit;

namespace Tedd.TUI.Tests;

/// <summary>
/// Input on XAML-loaded trees, routed through the same window pipeline the platform
/// hosts use: XAML-wired event handlers, tunnel/bubble propagation, and user input
/// flowing into view models through TwoWay bindings — verified down to rendered output.
/// </summary>
public class XamlInputBindingTests
{
    private class ViewModel : INotifyPropertyChanged
    {
        private string _text = "";
        private bool _flag;
        private int _number;
        private object? _selected;

        public string Text { get => _text; set { if (_text != value) { _text = value; OnPropertyChanged(); } } }
        public bool Flag { get => _flag; set { if (_flag != value) { _flag = value; OnPropertyChanged(); } } }
        public int Number { get => _number; set { if (_number != value) { _number = value; OnPropertyChanged(); } } }
        public object? Selected { get => _selected; set { if (_selected != value) { _selected = value; OnPropertyChanged(); } } }
        public ObservableCollection<string> Options { get; } = new() { "Red", "Green", "Blue" };

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    // --- XAML-wired event handlers ---

    private class ClickController
    {
        public int Clicks;
        public object? LastSender;

        private void OnButtonClick(object sender, RoutedEventArgs e)
        {
            Clicks++;
            LastSender = sender;
        }

        private void OnParameterless() => Clicks += 100;
    }

    [Fact]
    public void XamlClickHandler_FiresOnMouseClick()
    {
        var controller = new ClickController();
        var root = XamlLoader.Load(@"
<StackPanel xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <Button x:Name='Go' Content='Go' Click='OnButtonClick' />
</StackPanel>", controller);

        var button = (Button)root.FindName("Go");
        var host = new ControlTestHost(root, 20, 6);
        host.Click(button, button.RenderSize.Width / 2, button.RenderSize.Height / 2);

        Assert.Equal(1, controller.Clicks);
        Assert.Same(button, controller.LastSender);
    }

    [Fact]
    public void XamlClickHandler_FiresOnSpacebar()
    {
        var controller = new ClickController();
        var root = XamlLoader.Load(@"
<StackPanel xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <Button x:Name='Go' Content='Go' Click='OnButtonClick' />
</StackPanel>", controller);

        var button = (Button)root.FindName("Go");
        var host = new ControlTestHost(root, 20, 6);
        button.Focus();
        host.PressKey(ConsoleKey.Spacebar, ' ');

        Assert.Equal(1, controller.Clicks);
    }

    [Fact]
    public void XamlClickHandler_ParameterlessMethod_IsWrapped()
    {
        var controller = new ClickController();
        var root = XamlLoader.Load(@"
<StackPanel xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <Button x:Name='Go' Content='Go' Click='OnParameterless' />
</StackPanel>", controller);

        var button = (Button)root.FindName("Go");
        var host = new ControlTestHost(root, 20, 6);
        host.Click(button, button.RenderSize.Width / 2, button.RenderSize.Height / 2);

        Assert.Equal(100, controller.Clicks);
    }

    // --- Tunnel / bubble propagation ---

    [Fact]
    public void KeyDown_TunnelsThroughAncestors_ThenBubblesFromTarget()
    {
        var root = XamlLoader.Load(@"
<StackPanel xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <Border x:Name='Mid'>
        <TextBox x:Name='Input' />
    </Border>
</StackPanel>");

        var panel = (StackPanel)root;
        var border = (Border)root.FindName("Mid");
        var box = (TextBox)root.FindName("Input");

        var order = new List<string>();
        panel.AddHandler(UIElement.PreviewKeyDownEvent, (RoutedEventHandler)((s, e) => order.Add("panel-preview")));
        border.AddHandler(UIElement.PreviewKeyDownEvent, (RoutedEventHandler)((s, e) => order.Add("border-preview")));
        box.AddHandler(UIElement.PreviewKeyDownEvent, (RoutedEventHandler)((s, e) => order.Add("box-preview")));
        panel.AddHandler(UIElement.KeyDownEvent, (RoutedEventHandler)((s, e) => order.Add("panel-bubble")), handledEventsToo: true);
        border.AddHandler(UIElement.KeyDownEvent, (RoutedEventHandler)((s, e) => order.Add("border-bubble")), handledEventsToo: true);
        box.AddHandler(UIElement.KeyDownEvent, (RoutedEventHandler)((s, e) => order.Add("box-bubble")), handledEventsToo: true);

        var host = new ControlTestHost(root, 20, 6);
        box.Focus();
        host.KeyDown(ConsoleKey.A, 'a');

        Assert.Equal(
            new[] { "panel-preview", "border-preview", "box-preview", "box-bubble", "border-bubble", "panel-bubble" },
            order);
    }

    [Fact]
    public void PreviewHandled_SuppressesBubblePhase()
    {
        var root = XamlLoader.Load(@"
<StackPanel xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <TextBox x:Name='Input' />
</StackPanel>");

        var panel = (StackPanel)root;
        var box = (TextBox)root.FindName("Input");

        bool bubbleFired = false;
        panel.AddHandler(UIElement.PreviewKeyDownEvent, (RoutedEventHandler)((s, e) => e.Handled = true));
        box.AddHandler(UIElement.KeyDownEvent, (RoutedEventHandler)((s, e) => bubbleFired = true));

        var host = new ControlTestHost(root, 20, 6);
        box.Focus();
        var args = host.KeyDown(ConsoleKey.A, 'a');

        Assert.False(bubbleFired);
        Assert.True(args.Handled);
        // The handled key must not have reached the TextBox's editing logic.
        Assert.Equal(string.Empty, box.Text);
    }

    [Fact]
    public void MouseDown_OnPassiveElement_BubblesToAncestors()
    {
        var root = XamlLoader.Load(@"
<StackPanel xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <TextBlock x:Name='Label' Text='click me' />
</StackPanel>");

        var panel = (StackPanel)root;
        var label = (TextBlock)root.FindName("Label");

        var order = new List<string>();
        label.AddHandler(UIElement.MouseDownEvent, (RoutedEventHandler)((s, e) => order.Add("label")));
        panel.AddHandler(UIElement.MouseDownEvent, (RoutedEventHandler)((s, e) => order.Add("panel")));

        var host = new ControlTestHost(root, 20, 6);
        host.MouseDown(1, 0);

        Assert.Equal(new[] { "label", "panel" }, order);
    }

    // --- User input flowing into the view model through bindings ---

    [Fact]
    public void TypingInTextBox_UpdatesViewModel_ThroughDefaultTwoWayBinding()
    {
        var root = XamlLoader.Load(@"
<StackPanel xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <TextBox x:Name='Input' Text='{Binding Text}' />
</StackPanel>");
        var vm = new ViewModel();
        root.DataContext = vm;

        var box = (TextBox)root.FindName("Input");
        var host = new ControlTestHost(root, 30, 6);
        box.Focus();

        host.PressKey(ConsoleKey.H, 'h');
        host.PressKey(ConsoleKey.I, 'i');

        Assert.Equal("hi", box.Text);
        Assert.Equal("hi", vm.Text);
    }

    [Fact]
    public void CheckBoxSpace_TogglesViewModelFlag()
    {
        var root = XamlLoader.Load(@"
<StackPanel xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <CheckBox x:Name='Accept' Content='Accept' IsChecked='{Binding Flag}' />
</StackPanel>");
        var vm = new ViewModel { Flag = false };
        root.DataContext = vm;

        var checkBox = (CheckBox)root.FindName("Accept");
        var host = new ControlTestHost(root, 30, 6);
        checkBox.Focus();

        host.PressKey(ConsoleKey.Spacebar, ' ');
        Assert.True(vm.Flag);

        host.PressKey(ConsoleKey.Spacebar, ' ');
        Assert.False(vm.Flag);
    }

    [Fact]
    public void SliderArrowKeys_UpdateViewModelNumber()
    {
        var root = XamlLoader.Load(@"
<StackPanel xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <Slider x:Name='Level' Maximum='10' Value='{Binding Number}' />
</StackPanel>");
        var vm = new ViewModel { Number = 5 };
        root.DataContext = vm;

        var slider = (Slider)root.FindName("Level");
        Assert.Equal(5, slider.Value);

        var host = new ControlTestHost(root, 30, 6);
        slider.Focus();

        host.PressKey(ConsoleKey.RightArrow);
        Assert.Equal(6, vm.Number);

        host.PressKey(ConsoleKey.LeftArrow);
        host.PressKey(ConsoleKey.LeftArrow);
        Assert.Equal(4, vm.Number);
    }

    [Fact]
    public void ListBoxArrowKeys_UpdateBoundSelectedItem()
    {
        var root = XamlLoader.Load(@"
<StackPanel xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <ListBox x:Name='Choices' ItemsSource='{Binding Options}' SelectedItem='{Binding Selected}' />
</StackPanel>");
        var vm = new ViewModel();
        root.DataContext = vm;

        var listBox = (ListBox)root.FindName("Choices");
        var host = new ControlTestHost(root, 30, 8);
        listBox.Focus();

        host.PressKey(ConsoleKey.DownArrow);
        Assert.Equal("Red", vm.Selected);

        host.PressKey(ConsoleKey.DownArrow);
        Assert.Equal("Green", vm.Selected);
    }

    // --- Full MVVM loop with rendered verification ---

    private class CounterController
    {
        public readonly CounterVm Vm = new();

        private void OnIncrement(object sender, RoutedEventArgs e) => Vm.Count++;
    }

    private class CounterVm : INotifyPropertyChanged
    {
        private int _count;
        public int Count { get => _count; set { if (_count != value) { _count = value; OnPropertyChanged(); } } }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    [Fact]
    public void ButtonClick_UpdatesViewModel_AndBoundTextRerenders()
    {
        var controller = new CounterController();
        var root = XamlLoader.Load(@"
<StackPanel xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' Orientation='Vertical'>
    <Button x:Name='Plus' Content='+' Click='OnIncrement' />
    <TextBlock Text='{Binding Count, StringFormat=Count: {0}}' />
</StackPanel>", controller);
        root.DataContext = controller.Vm;

        var button = (Button)root.FindName("Plus");
        var host = new ControlTestHost(root, 30, 8);

        Assert.Contains("Count: 0", VirtualBufferAssertions.GetText(host.Render()));

        host.Click(button, button.RenderSize.Width / 2, button.RenderSize.Height / 2);
        host.Click(button, button.RenderSize.Width / 2, button.RenderSize.Height / 2);

        Assert.Equal(2, controller.Vm.Count);

        host.Window.Measure(new Size(30, 8));
        host.Window.Arrange(new Rect(0, 0, 30, 8));
        string text = VirtualBufferAssertions.GetText(host.Render());
        Assert.Contains("Count: 2", text);
        Assert.DoesNotContain("Count: 0", text);
    }

    [Fact]
    public void MouseClickOnTextBox_FocusesIt_AndTypingLandsThere()
    {
        var root = XamlLoader.Load(@"
<StackPanel xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' Orientation='Vertical'>
    <TextBox x:Name='First' Text='{Binding Text}' />
    <TextBox x:Name='Second' />
</StackPanel>");
        var vm = new ViewModel();
        root.DataContext = vm;

        var first = (TextBox)root.FindName("First");
        var host = new ControlTestHost(root, 30, 8);

        host.Click(first, 1, 0);
        Assert.True(first.IsFocused);

        host.PressKey(ConsoleKey.X, 'x');
        Assert.Equal("x", vm.Text);
    }
}
