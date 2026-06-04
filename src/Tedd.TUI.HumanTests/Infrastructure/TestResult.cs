namespace Tedd.TUI.HumanTests.Infrastructure;

public enum TestStatus
{
    NotRun,
    Passed,
    Failed,
    Skipped,
    /// <summary>Session or environment metadata (not a component test outcome).</summary>
    Info,
}

public class TestResult
{
    public string ComponentName { get; set; } = string.Empty;
    public TestStatus Status { get; set; }
    public string Message { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.Now;

    public override string ToString()
    {
        return $"{Timestamp:O},{ComponentName},{Status},\"{Message.Replace("\"", "\"\"")}\"";
    }
}
