namespace Tedd.TUI.HumanTests.RenderTargets;

/// <summary>
/// One rendering backend the human test app can run the test suite on. Targets are
/// listed in the startup picker; <see cref="Run"/> blocks until the session ends.
/// </summary>
public sealed class RenderTarget
{
    /// <summary>Stable key used for <c>--target &lt;id&gt;</c> command-line selection.</summary>
    public required string Id { get; init; }

    /// <summary>Menu line shown in the startup picker.</summary>
    public required string Label { get; init; }

    /// <summary>Launches the test session on this backend; returns when the session ends.</summary>
    public required Action Run { get; init; }
}
