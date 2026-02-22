using Tedd.TUI;
using Tedd.TUI.HumanTests.Infrastructure;
using System.Collections.ObjectModel;

namespace Tedd.TUI.HumanTests.Tests;

public class TableTest : TestPage
{
    public override string Name => "Table";
    public override string Description => "Standard Table Control";

    protected override void AddScenarios()
    {
        // 1. Functionality Test
        var funcPanel = new StackPanel { Orientation = Orientation.Vertical };
        var table = new Table { Width = 60, Height = 10, ShowHeader = true, PageSize = 5 };

        table.Columns.Add(new TableColumn { Header = "ID", Width = GridLength.Pixel(5) });
        table.Columns.Add(new TableColumn { Header = "Name", Width = GridLength.Star });
        table.Columns.Add(new TableColumn { Header = "Age", Width = GridLength.Auto });

        var rows = new ObservableCollection<TableRow>();

        // Add Rows
        for (int i = 1; i <= 20; i++)
        {
            table.AddRow(i.ToString(), $"Person {i}", (20 + i).ToString());
        }

        var output = new TextBlock { Text = "Selected: None" };

        // Table supports selection?
        // Checking Table.cs... usually yes.
        // If not, we test sorting.
        // Assuming no selection event for now unless confirmed.

        funcPanel.AddChild(table);
        funcPanel.AddChild(new TextBlock { Text = " " });

        // Pagination Controls?
        // Table has built-in pagination if PageSize > 0?
        // Usually yes, but we might need external buttons if Table doesn't render pagination controls itself.
        // Usually Table renders pagination info.

        AddScenario("Functionality", funcPanel);

        // 2. Sorting
        var sortTable = new Table { Width = 40, Height = 10 };
        var col1 = new TableColumn { Header = "Num", Width = GridLength.Pixel(10) };
        col1.SortComparer = (a, b) =>
        {
             if (int.TryParse(a.ToString(), out int i1) && int.TryParse(b.ToString(), out int i2))
                 return i1.CompareTo(i2);
             return string.Compare(a.ToString(), b.ToString());
        };
        sortTable.Columns.Add(col1);
        sortTable.Columns.Add(new TableColumn { Header = "Text", Width = GridLength.Star });

        sortTable.AddRow("10", "Ten");
        sortTable.AddRow("2", "Two");
        sortTable.AddRow("1", "One");

        AddScenario("Sorting", sortTable);

        // 3. Custom Cells
        var customTable = new Table { Width = 50, Height = 10 };
        customTable.Columns.Add(new TableColumn { Header = "Item", Width = GridLength.Star });
        customTable.Columns.Add(new TableColumn { Header = "Action", Width = GridLength.Pixel(15) });

        customTable.AddRow("Item A", new Button { Content = "Edit A" });
        customTable.AddRow("Item B", new CheckBox { Content = "Active" });

        AddScenario("Custom Cells", customTable);
    }
}
