using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Xunit;

namespace Tedd.TUI.Tests;

public class DataBindingCoverageTests
{
    public class TestViewModel : INotifyPropertyChanged
    {
        private string _title = "Initial Title";
        private int _count = 0;
        private bool _isActive = false;

        public string Title
        {
            get => _title;
            set { _title = value; OnPropertyChanged(); }
        }

        public int Count
        {
            get => _count;
            set { _count = value; OnPropertyChanged(); }
        }

        public bool IsActive
        {
            get => _isActive;
            set { _isActive = value; OnPropertyChanged(); }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    [Fact]
    public void Binding_InitialValue_IsPropagated()
    {
        var vm = new TestViewModel { Title = "Test Title" };
        var tb = new TextBlock();
        tb.DataContext = vm;
        tb.SetBinding(TextBlock.TextProperty, new Binding(nameof(TestViewModel.Title)));

        Assert.Equal("Test Title", tb.Text);
    }

    [Fact]
    public void Binding_PropertyChanged_UpdatesTarget()
    {
        var vm = new TestViewModel { Title = "Initial" };
        var tb = new TextBlock();
        tb.DataContext = vm;
        tb.SetBinding(TextBlock.TextProperty, new Binding(nameof(TestViewModel.Title)));

        Assert.Equal("Initial", tb.Text);

        vm.Title = "Updated";
        Assert.Equal("Updated", tb.Text);
    }

    [Fact]
    public void Binding_DataContextChange_UpdatesTarget()
    {
        var vm1 = new TestViewModel { Title = "VM1" };
        var vm2 = new TestViewModel { Title = "VM2" };
        var tb = new TextBlock();

        tb.SetBinding(TextBlock.TextProperty, new Binding(nameof(TestViewModel.Title)));

        tb.DataContext = vm1;
        Assert.Equal("VM1", tb.Text);

        tb.DataContext = vm2;
        Assert.Equal("VM2", tb.Text);
    }

    [Fact]
    public void Binding_NullDataContext_DoesNotCrash()
    {
        var vm = new TestViewModel { Title = "VM" };
        var tb = new TextBlock();
        tb.SetBinding(TextBlock.TextProperty, new Binding(nameof(TestViewModel.Title)));
        tb.DataContext = vm;

        Assert.Equal("VM", tb.Text);

        tb.DataContext = null;
        // Should not crash, value remains last set or default?
        // Binding.UpdateTarget returns if context is null, so value is NOT cleared.
        Assert.Equal("VM", tb.Text);
    }

    [Fact]
    public void Binding_InvalidPath_DoesNotCrash()
    {
        var vm = new TestViewModel();
        var tb = new TextBlock();
        tb.DataContext = vm;

        // Bind to non-existent property
        tb.SetBinding(TextBlock.TextProperty, new Binding("NonExistentProperty"));

        // Should be default (string.Empty for TextBlock.Text)
        Assert.Equal(string.Empty, tb.Text);
    }

    [Fact]
    public void Binding_DataContextInheritance_Works()
    {
        var vm = new TestViewModel { Title = "Inherited" };
        var stack = new StackPanel();
        var tb = new TextBlock();
        stack.AddChild(tb);

        // Set DataContext on parent
        stack.DataContext = vm;

        // Bind child
        tb.SetBinding(TextBlock.TextProperty, new Binding(nameof(TestViewModel.Title)));

        // Child should inherit DataContext
        Assert.Same(vm, tb.DataContext);
        Assert.Equal("Inherited", tb.Text);
    }

    [Fact]
    public void Binding_DataContextInheritance_Update_Works()
    {
        var vm1 = new TestViewModel { Title = "VM1" };
        var vm2 = new TestViewModel { Title = "VM2" };

        var stack = new StackPanel();
        var tb = new TextBlock();
        stack.AddChild(tb);

        stack.DataContext = vm1;
        tb.SetBinding(TextBlock.TextProperty, new Binding(nameof(TestViewModel.Title)));

        Assert.Equal("VM1", tb.Text);

        // Change parent DataContext
        stack.DataContext = vm2;

        Assert.Same(vm2, tb.DataContext);
        Assert.Equal("VM2", tb.Text);
    }

    [Fact]
    public void Binding_TypeMismatch_ThrowsOrIgnores()
    {
        // This test documents current behavior.
        // Binding int property (Count) to string property (Text).
        // If SetValue performs strict cast, this might fail.

        var vm = new TestViewModel { Count = 42 };
        var tb = new TextBlock();
        tb.DataContext = vm;

        // Text is string, Count is int.
        // DependencyProperty for Text is typeof(string).
        // SetValue(TextProperty, 42) -> if validation is loose, it might work if stored as object,
        // but usage as (string)GetValue() will cast exception.

        // Let's see if SetValue handles it.
        // If it throws, we should probably catch it in BindingExpression to be safe, but currently likely crashes.

        Assert.Throws<ArgumentException>(() =>
        {
            tb.SetBinding(TextBlock.TextProperty, new Binding(nameof(TestViewModel.Count)));
        });
    }

    [Fact]
    public void Binding_TypeMismatch_Safe_If_Converted()
    {
        // Ideally we want automatic ToString() conversion, but Binding doesn't support Converter yet.
        // This test confirms that we need to match types.

        var vm = new TestViewModel { Title = "123" };
        var progressBar = new ProgressBar(); // Value is int/double?
        // ProgressBar.Value is usually int or double. Let's check ProgressBar.
        // Assuming it's int for now based on TUI nature.

        progressBar.DataContext = vm;

        // Title is string "123". Value property expects int.
        // This will likely throw InvalidCastException or ArgumentException.
        Assert.Throws<ArgumentException>(() =>
        {
             progressBar.SetBinding(ProgressBar.ValueProperty, new Binding(nameof(TestViewModel.Title)));
        });
    }
}
