using System;
using System.IO;

public class Program
{
    public static void Main()
    {
        var file = "src/Tedd.TUI/Slider.cs";
        var lines = File.ReadAllLines(file);
        for (int i=0; i<lines.Length; i++) {
            if (lines[i].Contains("protected override void OnPropertyChanged(DependencyProperty dp, object oldValue, object newValue)")) {
                lines[i] = "    protected override void OnPropertyChanged(DependencyProperty dp)";
            }
        }
        File.WriteAllLines(file, lines);
    }
}
