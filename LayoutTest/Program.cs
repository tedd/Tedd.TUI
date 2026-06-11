using System;
using Tedd.TUI;

class Program {
    static void Main() {
        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        var text = new TextBlock { Text = "Hello" };
        Grid.SetColumn(text, 0);
        grid.Children.Add(text);

        var sv = new ScrollViewer();
        ScrollViewer.SetHorizontalScrollBarVisibility(sv, ScrollBarVisibility.Auto);
        sv.Content = grid;

        sv.Measure(new Size(80, 24));
        Console.WriteLine($"Grid DesiredSize: {grid.DesiredSize.Width}x{grid.DesiredSize.Height}");
        Console.WriteLine($"Attached H-Scroll Visibility: {ScrollViewer.GetHorizontalScrollBarVisibility(sv)}");
    }
}
