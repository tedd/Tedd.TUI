using Tedd.TUI.HumanTests.Infrastructure;
using Tedd.TUI.Platform.Sdl2;

namespace Tedd.TUI.HumanTests.RenderTargets;

/// <summary>
/// Runs the test session in a native SDL2 window (<see cref="TuiSdl2Host"/>: SkiaSharp
/// cell surface blitted into an SDL streaming texture; Windows/Linux/macOS).
/// </summary>
public static class Sdl2Target
{
    public static void Run()
    {
        using var host = new TuiSdl2Host();
        host.SetContent(TestSession.CreateWindow());
        host.Run("Tedd.TUI HumanTests - SDL2 host", columns: 110, rows: 40);
    }
}
