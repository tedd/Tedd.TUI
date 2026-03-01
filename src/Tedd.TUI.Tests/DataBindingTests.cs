using Xunit;
using Tedd.TUI;
using System.ComponentModel;

namespace Tedd.TUI.Tests;

public class DataBindingTests
{
    public class ViewModel
    {
        public string Title { get; set; } = "Initial Title";
    }

    [Fact]
    public void TestOneWayBinding()
    {
        var vm = new ViewModel();
        var tb = new TextBlock();
        tb.DataContext = vm;

        // Bind TextBlock.Text to ViewModel.Title
        tb.SetBinding(TextBlock.TextProperty, new Binding("Title"));

        Assert.Equal("Initial Title", tb.Text);

        // Simulate change (if we had INotifyPropertyChanged support fully implemented)
        // Since our Binding system is very basic and currently only updates on SetBinding or initial DataContext set,
        // we might not see updates unless we manually trigger or re-bind.

        // But let's check if setting DataContext later updates it.
        var vm2 = new ViewModel { Title = "New Title" };
        tb.DataContext = vm2;
        // OnPropertyChanged for DataContext calls UpdateTarget on bindings

        Assert.Equal("New Title", tb.Text);
    }
}
