using Xunit;
using Tedd.TUI;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Tedd.TUI.Tests;

public class DataGridTests
{
    public class Person
    {
        public string Name { get; set; }
        public int Age { get; set; }
    }

    [Fact]
    public void AutoGenerateColumns_True_GeneratesColumns()
    {
        var grid = new DataGrid { AutoGenerateColumns = true };
        var list = new List<Person> { new Person { Name = "Test", Age = 10 } };
        grid.ItemsSource = list;

        Assert.Equal(2, grid.Columns.Count);
        Assert.Contains(grid.Columns, c => c.Header == "Name");
        Assert.Contains(grid.Columns, c => c.Header == "Age");
    }

    [Fact]
    public void ItemsSource_Updates_TableRows()
    {
        var grid = new DataGrid { AutoGenerateColumns = true };
        var list = new ObservableCollection<Person>();
        grid.ItemsSource = list;

        list.Add(new Person { Name = "A", Age = 1 });

        var table = (Table)grid.GetVisualChild(0);
        Assert.Single(table.Rows);

        var row = table.Rows[0];
        // Verify cell content?
        // Cell 0 should be Name, Cell 1 Age (or order depends on reflection)
        // Reflection order is not guaranteed, but usually definition order.

        // Let's check headers to find index
        int nameIdx = -1;
        for(int i=0; i<grid.Columns.Count; i++)
        {
            if (grid.Columns[i].Header == "Name") nameIdx = i;
        }

        var nameCell = (TextBlock)row.Cells[nameIdx]; // It's a string, so wrapped in TextBlock?
        // TableRow.AddCell(string) creates TextBlock.
        // But RefreshRows calls AddCell(string).
        // Wait, RefreshRows adds string. TableRow.AddCell(string) adds TextBlock.
        // So yes, it is TextBlock.

        Assert.Equal("A", nameCell.Text);
    }
}
