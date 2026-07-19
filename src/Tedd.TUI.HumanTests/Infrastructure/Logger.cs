using System.IO;

namespace Tedd.TUI.HumanTests.Infrastructure;

public static class Logger
{
    private const string LogFileName = "test_results.log";

    public static void Log(TestResult result)
    {
        try
        {
            var line = result.ToString() + Environment.NewLine;
            File.AppendAllText(LogFileName, line);
        }
        catch (Exception ex)
        {
            // Fallback to console in case of file error (though in TUI this might be hidden)
            System.Diagnostics.Debug.WriteLine($"Failed to log result: {ex.Message}");
        }
    }

    public static void Clear()
    {
        if (File.Exists(LogFileName))
        {
            File.Delete(LogFileName);
        }
        File.WriteAllText(LogFileName, "Timestamp,Component,Status,Message" + Environment.NewLine);
    }
}
