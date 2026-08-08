using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Xunit;
using Tedd.TUI;
using Tedd.TUI.Tests.TestInfrastructure;

namespace Tedd.TUI.Tests;

public class DataGridTests
{
    public class Person
    {
        public string Name { get; set; } = "";
        public int Age { get; set; }
    }

    [Theory]
    [InlineData(true, 2)]
    [InlineData(false, 0)]
    public void AutoGenerateColumns_GeneratesColumns(bool autoGenerate, int expectedColumnCount)
    {
        var grid = new DataGrid { AutoGenerateColumns = autoGenerate };
        var list = new List<Person> { new Person { Name = "Test", Age = 10 } };
        grid.ItemsSource = list;

        Assert.Equal(expectedColumnCount, grid.Columns.Count);
    }

    [Theory]
    [InlineData("Name", "A")]
    [InlineData("Age", "1")]
    public void ItemsSource_Updates_TableRows(string targetColumnHeader, string expectedCellValue)
    {
        var grid = new DataGrid { AutoGenerateColumns = true };
        var list = new ObservableCollection<Person>();
        grid.ItemsSource = list;

        list.Add(new Person { Name = "A", Age = 1 });

        var table = (Table)grid.GetVisualChild(0);
        Assert.Single(table.Rows);

        var row = table.Rows[0];

        int nameIdx = -1;
        for (int i = 0; i < grid.Columns.Count; i++)
        {
            if (grid.Columns[i].Header == targetColumnHeader) nameIdx = i;
        }

        var cell = (TextBlock)row.Cells[nameIdx];
        Assert.Equal(expectedCellValue, cell.Text);
    }

    [Theory]
    [InlineData("My Header", "MyPath", 50)]
    [InlineData("EmptyHeader", "", 10)]
    [InlineData(null, null, 100)]
    public void DataGridColumn_Properties(string? header, string? bindingPath, double widthPixels)
    {
        var col = new DataGridColumn();
        col.Header = header;
        col.BindingPath = bindingPath;
        col.Width = new GridLength(widthPixels, GridUnitType.Pixel);

        Assert.Equal(header, col.Header);
        Assert.Equal(bindingPath, col.BindingPath);
        Assert.Equal(widthPixels, col.Width.Value);
    }

    [Fact]
    public void ItemsSource_NullValues_AreHandled()
    {
        var grid = new DataGrid { AutoGenerateColumns = true };
        var list = new List<Person?> { null, new Person { Name = "Valid", Age = 20 }, null };
        grid.ItemsSource = list;

        var table = (Table)grid.GetVisualChild(0);
        Assert.Single(table.Rows); // Nulls are skipped in AddRowForItem
    }

    [Fact]
    public void Columns_CollectionChanged_RebuildsColumns()
    {
        var grid = new DataGrid { AutoGenerateColumns = false };
        var list = new List<Person> { new Person { Name = "Test", Age = 10 } };
        grid.ItemsSource = list;

        grid.Columns.Add(new DataGridColumn { Header = "Name", BindingPath = "Name" });

        var table = (Table)grid.GetVisualChild(0);
        Assert.Single(table.Columns);
        Assert.Equal("Name", table.Columns[0].Header);
        Assert.Single(table.Rows); // Refreshes rows as well
    }

    [Fact]
    public void CollectionChanged_Remove_RemovesRow()
    {
        var grid = new DataGrid { AutoGenerateColumns = true };
        var p1 = new Person { Name = "A", Age = 1 };
        var p2 = new Person { Name = "B", Age = 2 };
        var list = new ObservableCollection<Person> { p1, p2 };
        grid.ItemsSource = list;

        var table = (Table)grid.GetVisualChild(0);
        Assert.Equal(2, table.Rows.Count);

        list.Remove(p1);

        Assert.Single(table.Rows);
        int nameIdx = grid.Columns.ToList().FindIndex(c => c.Header == "Name");
        Assert.Equal("B", ((TextBlock)table.Rows[0].Cells[nameIdx]).Text);
    }

    [Fact]
    public void CollectionChanged_Replace_RefreshesRows()
    {
        var grid = new DataGrid { AutoGenerateColumns = true };
        var p1 = new Person { Name = "A", Age = 1 };
        var p2 = new Person { Name = "B", Age = 2 };
        var list = new ObservableCollection<Person> { p1 };
        grid.ItemsSource = list;

        var table = (Table)grid.GetVisualChild(0);
        Assert.Single(table.Rows);

        list[0] = p2; // triggers Replace action

        Assert.Single(table.Rows);
        int nameIdx = grid.Columns.ToList().FindIndex(c => c.Header == "Name");
        Assert.Equal("B", ((TextBlock)table.Rows[0].Cells[nameIdx]).Text);
    }

    [Fact]
    public void CollectionChanged_Reset_RefreshesRows()
    {
        var grid = new DataGrid { AutoGenerateColumns = true };
        var p1 = new Person { Name = "A", Age = 1 };
        var list = new ObservableCollection<Person> { p1 };
        grid.ItemsSource = list;

        var table = (Table)grid.GetVisualChild(0);
        Assert.Single(table.Rows);

        list.Clear(); // triggers Reset action

        Assert.Empty(table.Rows);
    }

    [Theory]
    [InlineData("Invalid", "NonExistent")]
    [InlineData("EmptyPath", "")]
    [InlineData("NullPath", null)]
    public void EnsureGetters_InvalidPropertyPath_ReturnsEmptyString(string header, string? bindingPath)
    {
        var grid = new DataGrid { AutoGenerateColumns = false };
        grid.Columns.Add(new DataGridColumn { Header = header, BindingPath = bindingPath });
        var list = new List<Person> { new Person { Name = "Test" } };
        grid.ItemsSource = list;

        var table = (Table)grid.GetVisualChild(0);
        var row = table.Rows[0];
        Assert.Equal("", ((TextBlock)row.Cells[0]).Text);
    }

    [Theory]
    [InlineData(100, 100)]
    [InlineData(0, 0)]
    [InlineData(10, 50)]
    public void RenderAndLayout_PassedToInternalTable(int width, int height)
    {
        var grid = new DataGrid { AutoGenerateColumns = false };
        grid.Columns.Add(new DataGridColumn { Header = "Name", BindingPath = "Name" });
        grid.ItemsSource = new List<Person> { new Person { Name = "Test" } };

        grid.Measure(new Size(width, height));

        // DataGrid passes measurement directly down
        if (width > 0 && height > 0)
        {
            Assert.True(grid.DesiredSize.Width > 0);
            Assert.True(grid.DesiredSize.Height > 0);
        }

        grid.Arrange(new Rect(0, 0, width, height));

        if (width > 0 && height > 0)
        {
            var buffer = new VirtualBuffer(width, height);
            grid.Render(buffer, 0, 0);

            // Spot check that something was drawn (table borders or text)
            bool hasContent = false;
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    if (buffer.GetPixel(x, y).Character != ' ' && buffer.GetPixel(x, y).Character != '\0')
                    {
                        hasContent = true;
                        break;
                    }
                }
            }
            Assert.True(hasContent);
        }
    }

    [Theory]
    [InlineData(ConsoleKey.DownArrow)]
    [InlineData(ConsoleKey.UpArrow)]
    public void InputRouting_KeyEvents_PassedToInternalTable(ConsoleKey key)
    {
        var grid = new DataGrid { AutoGenerateColumns = false };
        grid.Columns.Add(new DataGridColumn { Header = "Name", BindingPath = "Name" });
        grid.ItemsSource = new List<Person> { new Person { Name = "Test" } };

        var keyArgs = new KeyEventArgs { Key = key };
        grid.OnKeyDown(keyArgs);
        // Table handles arrow keys for selection
        Assert.True(keyArgs.Handled);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void MouseClick_SelectsEachRow_WithEveryHeaderAndBorderCombination(
        bool showHeader,
        bool showBorder)
    {
        var people = CreatePeople(5);
        var grid = CreateGrid(people, showHeader, showBorder);
        var host = new ControlTestHost(grid, 20, 10);
        int bodyY = (showBorder ? 1 : 0) + (showHeader ? 2 : 0);
        int selectionChanges = 0;
        grid.SelectionChanged += (_, _) => selectionChanges++;

        for (int rowIndex = 0; rowIndex < people.Count; rowIndex++)
        {
            host.Click(grid, showBorder ? 1 : 0, bodyY + rowIndex);

            Assert.True(((Table)grid.GetVisualChild(0)).IsFocused);
            Assert.Equal(rowIndex, grid.SelectedIndex);
            Assert.Same(people[rowIndex], grid.SelectedItem);
        }

        Assert.Equal(people.Count, selectionChanges);
    }

    [Fact]
    public void MouseClick_NestedInPanelsAndBorder_SelectsScreenRowNotParentRelativeRow()
    {
        var people = CreatePeople(6);
        var grid = CreateGrid(people, showHeader: true, showBorder: true);
        // 12 = the border's two lines plus the grid's own 10 (border, header, 6 rows).
        // A shorter frame is legitimate but makes the grid scroll its body, which moves
        // the rows this test is about.
        var borderedGrid = new Border
        {
            Child = grid,
            BoxStyle = BoxStyle.Double,
            Width = 22,
            Height = 12,
            Padding = new Thickness(0)
        };
        var nestedPanel = new StackPanel();
        nestedPanel.AddChild(new TextBlock { Text = "toolbar" });
        nestedPanel.AddChild(new TextBlock { Text = "status" });
        nestedPanel.AddChild(borderedGrid);
        var host = new ControlTestHost(nestedPanel, 22, 14);

        // Grid local row 4 is screen row 2 (panel offset) + 1 (Border) + 3 + 4.
        host.Click(grid, 1, 3 + 4);

        Assert.Equal(4, grid.SelectedIndex);
        Assert.Same(people[4], grid.SelectedItem);
    }

    [Fact]
    public void MouseClick_InsideScrolledNestedContent_SelectsVisibleScreenRow()
    {
        var people = CreatePeople(6);
        var grid = CreateGrid(people, showHeader: true, showBorder: false);
        grid.Height = 8;

        var scrolledContent = new StackPanel();
        scrolledContent.AddChild(new TextBlock { Text = "one" });
        scrolledContent.AddChild(new TextBlock { Text = "two" });
        scrolledContent.AddChild(new TextBlock { Text = "three" });
        scrolledContent.AddChild(grid);

        var outerScrollViewer = new ScrollViewer
        {
            Content = scrolledContent,
            VerticalScrollBarVisibility = ScrollBarVisibility.Visible,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled
        };
        var host = new ControlTestHost(outerScrollViewer, 20, 6);
        outerScrollViewer.ScrollToVerticalOffset(5);

        // After the outer offset, DataGrid row 3 is visibly rendered at screen Y=3.
        Assert.Equal(new Point(0, -2), grid.PointToScreen(new Point(0, 0)));
        host.Click(grid, 0, 2 + 3);

        Assert.Equal(3, grid.SelectedIndex);
        Assert.Same(people[3], grid.SelectedItem);
    }

    [Fact]
    public void MouseClick_AfterDataGridBodyScroll_SelectsScrolledRow()
    {
        var people = CreatePeople(10);
        var grid = CreateGrid(people, showHeader: true, showBorder: true);
        var host = new ControlTestHost(grid, 20, 8);
        var table = (Table)grid.GetVisualChild(0);
        var bodyScrollViewer = (ScrollViewer)table.GetVisualChild(0);
        Assert.True(bodyScrollViewer.IsVerticalScrollBarShown);

        bodyScrollViewer.ScrollToVerticalOffset(4);
        host.Click(grid, 1, 3 + 1);

        Assert.Equal(5, grid.SelectedIndex);
        Assert.Same(people[5], grid.SelectedItem);
    }

    [Fact]
    public void MouseClick_HeaderBorderScrollbarAndSibling_DoNotChangeSelection()
    {
        var people = CreatePeople(10);
        var grid = CreateGrid(people, showHeader: true, showBorder: true);
        grid.Width = 20;
        var root = new StackPanel { Orientation = Orientation.Horizontal };
        root.AddChild(grid);
        root.AddChild(new Button { Content = "Other", Width = 7 });
        var host = new ControlTestHost(root, 27, 8);

        grid.SelectedIndex = 4;

        host.Click(grid, 1, 0);
        Assert.Equal(4, grid.SelectedIndex);

        host.Click(grid, 1, 1);
        Assert.Equal(4, grid.SelectedIndex);

        host.Click(grid, 18, 4);
        Assert.Equal(4, grid.SelectedIndex);

        host.Click(23, 1);
        Assert.Equal(4, grid.SelectedIndex);
        Assert.Same(people[4], grid.SelectedItem);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(-1)]
    [InlineData(10)]
    public void GetVisualChild_InvalidIndex_Throws(int index)
    {
        var grid = new DataGrid();
        Assert.Throws<ArgumentOutOfRangeException>(() => grid.GetVisualChild(index));
    }

    public class PersonWithException
    {
        public string Name { get => throw new Exception("Error"); }
    }

    [Fact]
    public void EnsureGetters_PropertyThrowsException_ReturnsEmptyString()
    {
        var grid = new DataGrid { AutoGenerateColumns = false };
        grid.Columns.Add(new DataGridColumn { Header = "Name", BindingPath = "Name" });
        grid.ItemsSource = new List<PersonWithException> { new PersonWithException() };

        var table = (Table)grid.GetVisualChild(0);
        var row = table.Rows[0];
        Assert.Equal("", ((TextBlock)row.Cells[0]).Text);
    }

    [Fact]
    public void AddRowForItem_NoColumns_UsesToString()
    {
        var grid = new DataGrid { AutoGenerateColumns = false };
        grid.ItemsSource = new List<Person> { new Person { Name = "Test", Age = 10 } };

        var table = (Table)grid.GetVisualChild(0);
        var row = table.Rows[0];
        Assert.Equal(typeof(Person).FullName, ((TextBlock)row.Cells[0]).Text);
    }

    private static List<Person> CreatePeople(int count) =>
        Enumerable.Range(0, count)
            .Select(index => new Person { Name = $"Person {index}", Age = 20 + index })
            .ToList();

    private static DataGrid CreateGrid(
        List<Person> people,
        bool showHeader,
        bool showBorder)
    {
        var grid = new DataGrid
        {
            AutoGenerateColumns = false,
            ShowHeader = showHeader,
            ShowBorder = showBorder
        };
        grid.Columns.Add(new DataGridColumn
        {
            Header = "Name",
            BindingPath = "Name",
            Width = new GridLength(18, GridUnitType.Pixel)
        });
        grid.ItemsSource = people;
        return grid;
    }
}
