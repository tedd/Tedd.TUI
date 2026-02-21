using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Xunit;
using Tedd.TUI;

namespace Tedd.TUI.Tests;

public class TestViewModel : INotifyPropertyChanged
{
    private string _testProperty;
    public string TestProperty
    {
        get => _testProperty;
        set
        {
            if (_testProperty != value)
            {
                _testProperty = value;
                OnPropertyChanged();
            }
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

public class BindingTests
{
    [Fact]
    public void PropertyChange_ShouldUpdateTarget()
    {
        // Arrange
        var vm = new TestViewModel { TestProperty = "Initial Value" };
        var textBlock = new TextBlock();
        textBlock.DataContext = vm;

        // Bind TextBlock.Text to TestProperty
        var binding = new Binding("TestProperty");
        textBlock.SetBinding(TextBlock.TextProperty, binding);

        // Assert Initial Value
        Assert.Equal("Initial Value", textBlock.Text);

        // Act
        vm.TestProperty = "New Value";

        // Assert Update
        Assert.Equal("New Value", textBlock.Text);
    }
}
