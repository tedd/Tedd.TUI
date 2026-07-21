using System.Collections.ObjectModel;
using System.Linq;
using Xunit;
using Tedd.TUI;
using Tedd.TUI.Controls;

namespace Tedd.TUI.Tests;

/// <summary>Declaring and binding the selection from XAML.</summary>
public class ListBoxXamlSelectionTests
{
    private sealed class Model
    {
        public ObservableCollection<string> Options { get; } = ["A", "B", "C", "D"];
        public ObservableCollection<object?> Picked { get; } = [];
    }

    [Fact]
    public void SelectionMode_IsSettableFromXaml()
    {
        var list = (ListBox)XamlLoader.Load("<ListBox SelectionMode='Extended' />");

        Assert.Equal(SelectionMode.Extended, list.SelectionMode);
    }

    [Fact]
    public void SelectedItems_BindsToAViewModelCollection()
    {
        var model = new Model();
        var list = (ListBox)XamlLoader.Load(
            "<ListBox SelectionMode='Extended' ItemsSource='{Binding Options}' SelectedItems='{Binding Picked}' />");
        list.DataContext = model;

        list.SelectSingle(1);
        list.ExtendSelectionTo(2);

        Assert.Equal(new object?[] { "B", "C" }, model.Picked);
    }
}
