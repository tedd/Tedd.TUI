namespace Tedd.TUI;

public interface IRenderer
{
    void Render(VirtualBuffer buffer);
}

public class ConsoleRenderer : IRenderer
{
    public void Render(VirtualBuffer buffer)
    {
        // This would typically use Console.SetCursorPosition and Console.Write
        // For now we implement a basic version that clears and writes line by line
        Console.SetCursorPosition(0, 0);
        
        for (int y = 0; y < buffer.Height; y++)
        {
            for (int x = 0; x < buffer.Width; x++)
            {
                var cell = buffer.GetPixel(x, y);
                Console.ForegroundColor = cell.Foreground;
                Console.BackgroundColor = cell.Background;
                Console.Write(cell.Character);
            }
            Console.WriteLine();
        }
        Console.ResetColor();
    }
}
