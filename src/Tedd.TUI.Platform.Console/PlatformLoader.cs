using System;
using System.Reflection;
using Tedd.TUI;

namespace Tedd.TUI.Platform.Console;

/// <summary>
/// Auto-detects and instantiates the best available <see cref="ITuiPlatform"/> for the
/// current host. Looks for the optional truecolor backends
/// <c>Tedd.TUI.Platform.WindowsTerminal</c> and <c>Tedd.TUI.Platform.LinuxTerminal</c>
/// at runtime via reflection; falls back to <see cref="LegacyConsolePlatform"/> when
/// none are referenced or none accept the current terminal.
/// </summary>
/// <remarks>
/// <para>The reflection lookup is intentional: it lets a consumer opt in to truecolor
/// rendering simply by adding a project / NuGet reference, without forcing
/// <c>Tedd.TUI.Platform.Console</c> to take a hard dependency on the platform-specific
/// assemblies. When the assemblies are missing the loader silently returns the legacy
/// path, preserving the framework's "works out of the box on conhost" promise.</para>
/// </remarks>
public static class PlatformLoader
{
    /// <summary>
    /// Selects and constructs a platform. Honors <paramref name="explicitPlatform"/> when
    /// supplied; otherwise probes the environment and tries the modern backends in order.
    /// </summary>
    public static ITuiPlatform Load(ITuiPlatform? explicitPlatform = null)
    {
        if (explicitPlatform != null) return explicitPlatform;

        var profile = TerminalProbe.Detect();

        if (profile.IsWindowsTerminal || profile.IsLegacyWindowsConsole)
        {
            var win = TryLoad("Tedd.TUI.Platform.WindowsTerminal", "Tedd.TUI.Platform.WindowsTerminal.WindowsTerminalPlatform", profile);
            if (win != null) return win;
        }

        if (profile.IsUnixTerminal)
        {
            var lin = TryLoad("Tedd.TUI.Platform.LinuxTerminal", "Tedd.TUI.Platform.LinuxTerminal.LinuxTerminalPlatform", profile);
            if (lin != null) return lin;
        }

        return new LegacyConsolePlatform(profile);
    }

    private static ITuiPlatform? TryLoad(string assemblyName, string typeName, TerminalProfile profile)
    {
        try
        {
            var asm = Assembly.Load(new AssemblyName(assemblyName));
            var type = asm.GetType(typeName, throwOnError: false);
            if (type == null) return null;

            // Prefer a (TerminalProfile) ctor when present, otherwise the parameterless ctor.
            var profileCtor = type.GetConstructor(new[] { typeof(TerminalProfile) });
            if (profileCtor != null)
            {
                return (ITuiPlatform?)profileCtor.Invoke(new object[] { profile });
            }
            return (ITuiPlatform?)Activator.CreateInstance(type);
        }
        catch
        {
            // Reflection-loaded backends are optional by design; any failure (missing
            // assembly, missing type, missing ctor, exception in ctor) falls through to
            // the legacy renderer so the app still launches.
            return null;
        }
    }
}
