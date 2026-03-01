using BenchmarkDotNet.Attributes;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Tedd.TUI;

namespace Tedd.TUI.Benchmarks;

[MemoryDiagnoser]
public class DataGridBenchmark
{
    private DataGrid _dataGrid;
    private ObservableCollection<TestItem> _items;

    public class TestItem
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public DateTime Date { get; set; }
        public bool IsActive { get; set; }
    }

    [Params(100, 1000)]
    public int ItemCount;

    [GlobalSetup]
    public void Setup()
    {
        _dataGrid = new DataGrid();
        _dataGrid.AutoGenerateColumns = true;

        _items = new ObservableCollection<TestItem>();
        for (int i = 0; i < ItemCount; i++)
        {
            _items.Add(new TestItem
            {
                Id = i,
                Name = $"Item {i}",
                Date = DateTime.Now,
                IsActive = i % 2 == 0
            });
        }
    }

    [Benchmark]
    public void PopulateDataGrid()
    {
        // Re-initialize to force re-evaluation of getters
        var grid = new DataGrid();
        grid.AutoGenerateColumns = true;
        grid.ItemsSource = _items;
    }
}
