using System.Diagnostics;
using System.IO;

namespace Tedd.TUI.HumanTests.RenderTargets;

/// <summary>
/// Runs the Blazor rendering target by starting the <c>Tedd.TUI.Demo.Blazor</c>
/// WebAssembly dev server (browser Canvas/DOM renderers). Requires the repository
/// checkout next to this executable's project; the browser opens automatically via the
/// launch profile and Ctrl+C stops the server. Set <c>TEDD_TUI_NO_OPEN=1</c> to start
/// the server without opening a browser (automation/CI).
/// </summary>
public static class BlazorTarget
{
    public static void Run()
    {
        var project = FindProject();
        if (project == null)
        {
            System.Console.WriteLine("Could not locate Tedd.TUI.Demo.Blazor relative to this executable.");
            System.Console.WriteLine("Run it directly instead:  dotnet run --project src/Tedd.TUI.Demo.Blazor");
            return;
        }

        System.Console.WriteLine("Starting the Blazor WASM dev server (Ctrl+C stops it)...");
        var psi = new ProcessStartInfo { FileName = "dotnet", UseShellExecute = false };
        psi.ArgumentList.Add("run");
        psi.ArgumentList.Add("--project");
        psi.ArgumentList.Add(project);
        if (Environment.GetEnvironmentVariable("TEDD_TUI_NO_OPEN") is not (null or "" or "0"))
            psi.ArgumentList.Add("--no-launch-profile");

        using var process = Process.Start(psi);
        process?.WaitForExit();
    }

    private static string? FindProject()
    {
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir != null; dir = dir.Parent)
        {
            var candidate = Path.Combine(dir.FullName, "Tedd.TUI.Demo.Blazor", "Tedd.TUI.Demo.Blazor.csproj");
            if (File.Exists(candidate))
                return candidate;
        }
        return null;
    }
}
