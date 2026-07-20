namespace Tedd.TUI.Controls;

public class RowDefinition
{
    public GridLength Height { get; set; } = GridLength.Star;
    public int MinHeight { get; set; } = 0;
    public int MaxHeight { get; set; } = int.MaxValue;

    internal int ActualHeight { get; set; }
    internal int Offset { get; set; }
}
