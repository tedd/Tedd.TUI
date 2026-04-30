using System;
using Tedd.TUI;
using Tedd.TUI.Markdown;

namespace LayoutTest;

class Program
{
    static void Main(string[] args)
    {
        var mdScrollViewer = new ScrollViewer { Width = 70, Height = 15, VerticalScrollBarVisibility = true };
        var mdView = new MarkdownView();
        
        string mdText = @"# Table

| ID | Name |
|---|---|
| 1 | Alice |
| 2 | Bob |";
        
        mdView.Text = mdText;
        mdScrollViewer.Content = mdView;

        // Measure Phase
        mdScrollViewer.Measure(new Size(70, 15));

        Console.WriteLine($""ScrollViewer DesiredSize: {mdScrollViewer.DesiredSize.Width}x{mdScrollViewer.DesiredSize.Height}"");
        Console.WriteLine($""MarkdownView DesiredSize: {mdView.DesiredSize.Width}x{mdView.DesiredSize.Height}"");

        // Arrange Phase
        mdScrollViewer.Arrange(new Rect(0, 0, 70, 15));

        // Get inner FlowDocument
        var fd = (UIElement)mdView.GetVisualChild(0);
        Console.WriteLine($""FlowDocument RenderSize: {fd.RenderSize.Width}x{fd.RenderSize.Height}"");

        for (int i = 0; i < fd.VisualChildrenCount; i++)
        {
             var block = fd.GetVisualChild(i);
             Console.WriteLine($"" Block {i} {block.GetType().Name} RenderSize: {block.RenderSize.Width}x{block.RenderSize.Height}"");
             if (block is Table t)
             {
                  Console.WriteLine($""   Table RenderSize: {t.RenderSize}"");
                  var tsv = (ScrollViewer)t.GetVisualChild(0);
                  Console.WriteLine($""   Table.ScrollViewer RenderSize: {tsv.RenderSize}"");
                  Console.WriteLine($""   Table.ScrollViewer ViewportH: {tsv.RenderSize.Height - 1}  MaxOffset: {tsv.VerticalOffset}"");
                  
                  var stack = (StackPanel)tsv.Content;
                  Console.WriteLine($""   Table.RowStack RenderSize: {stack.RenderSize}"");

                  for (int j = 0; j < stack.VisualChildrenCount; j++)
                  {
                      var row = stack.GetVisualChild(j);
                      Console.WriteLine($""      Row {j}: {row.GetType().Name} RenderSize: {row.RenderSize}"");
                  }
             }
        }
    }
}
