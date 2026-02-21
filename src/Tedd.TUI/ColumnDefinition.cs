namespace Tedd.TUI;

public class ColumnDefinition
{
    public GridLength Width { get; set; } = GridLength.Star;
    public int MinWidth { get; set; } = 0;
    public int MaxWidth { get; set; } = int.MaxValue;

    internal int ActualWidth { get; set; }
    internal int Offset { get; set; }
}
