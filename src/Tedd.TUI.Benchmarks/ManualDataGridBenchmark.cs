using System;
using System.Diagnostics;
using System.Collections.ObjectModel;
using Tedd.TUI;

namespace Tedd.TUI.Benchmarks;

public class ManualDataGridBenchmark
{
    public class TestItem
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public DateTime Date { get; set; }
        public bool IsActive { get; set; }
    }

    public static void Run()
    {
        int itemCount = 1000; // Reduced from 10000 to avoid timeout
        var items = new ObservableCollection<TestItem>();
        for (int i = 0; i < itemCount; i++)
        {
            items.Add(new TestItem
            {
                Id = i,
                Name = $"Item {i}",
                Date = DateTime.Now,
                IsActive = i % 2 == 0
            });
        }

        // Warmup
        for (int i = 0; i < 5; i++)
        {
            var grid = new DataGrid();
            grid.AutoGenerateColumns = true;
            grid.ItemsSource = items;
        }

        // Measure
        var sw = Stopwatch.StartNew();
        for (int i = 0; i < 50; i++)
        {
            var grid = new DataGrid();
            grid.AutoGenerateColumns = true;
            grid.ItemsSource = items;
        }
        sw.Stop();

        Console.WriteLine($"Total time for 50 iterations with {itemCount} items: {sw.ElapsedMilliseconds} ms");
        Console.WriteLine($"Average time per iteration: {sw.ElapsedMilliseconds / 50.0} ms");
    }
}
