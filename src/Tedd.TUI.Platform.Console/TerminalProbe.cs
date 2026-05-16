using System;
using System.Runtime.InteropServices;

namespace Tedd.TUI.Platform.Console;

/// <summary>
/// Inspects the environment to build a <see cref="TerminalProfile"/> describing the
/// host terminal's color, image, and platform capabilities. Cheap and side-effect-free
/// so it can be called eagerly at startup.
/// </summary>
public static class TerminalProbe
{
    private static TerminalProfile? _cached;

    /// <summary>
    /// Returns the cached <see cref="TerminalProfile"/> for the current process, probing
    /// the environment on the first call. Subsequent calls are O(1).
    /// </summary>
    public static TerminalProfile Detect()
    {
        return _cached ??= Probe();
    }

    /// <summary>Forces a fresh probe. Primarily useful for tests.</summary>
    public static TerminalProfile Refresh()
    {
        _cached = Probe();
        return _cached;
    }

    private static TerminalProfile Probe()
    {
        string? term = Environment.GetEnvironmentVariable("TERM");
        string? colorTerm = Environment.GetEnvironmentVariable("COLORTERM");
        string? wtSession = Environment.GetEnvironmentVariable("WT_SESSION");
        // Windows Terminal also sets WT_PROFILE_ID; some hosts only expose one of the two.
        string? wtProfileId = Environment.GetEnvironmentVariable("WT_PROFILE_ID");
        string? lcTerminal = Environment.GetEnvironmentVariable("LC_TERMINAL");
        string? termProgram = Environment.GetEnvironmentVariable("TERM_PROGRAM");
        string? kittyId = Environment.GetEnvironmentVariable("KITTY_WINDOW_ID");

        bool isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
        bool isUnix = !isWindows;
        bool isWindowsTerminal =
            !string.IsNullOrEmpty(wtSession) || !string.IsNullOrEmpty(wtProfileId);

        // COLORTERM=truecolor / 24bit is the only widely-honored signal.
        bool trueColor = false;
        if (!string.IsNullOrEmpty(colorTerm))
        {
            var ct = colorTerm.ToLowerInvariant();
            trueColor = ct.Contains("truecolor") || ct.Contains("24bit");
        }
        if (!trueColor && isWindowsTerminal) trueColor = true;
        if (!trueColor && !string.IsNullOrEmpty(term) && term.Contains("direct", StringComparison.OrdinalIgnoreCase)) trueColor = true;

        bool isITerm2 =
            string.Equals(lcTerminal, "iTerm2", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(termProgram, "iTerm.app", StringComparison.OrdinalIgnoreCase);

        bool isKitty = !string.IsNullOrEmpty(kittyId) ||
            (term?.Contains("kitty", StringComparison.OrdinalIgnoreCase) ?? false);

        var protocol = TerminalImageProtocol.None;
        if (isKitty) protocol = TerminalImageProtocol.Kitty;
        else if (isITerm2) protocol = TerminalImageProtocol.ITerm2;
        else if (isWindowsTerminal || (term != null && term.Contains("xterm", StringComparison.OrdinalIgnoreCase)))
        {
            // Sixel is supported on Windows Terminal 1.22+, xterm with -ti vt340, mlterm, contour, foot.
            // We optimistically pick Sixel for these hosts; the encoder is itself a no-op when missing.
            protocol = TerminalImageProtocol.Sixel;
        }

        // Conhost (legacy) generally lacks $WT_SESSION and stays on the legacy renderer path.
        bool isLegacyWindowsConsole = isWindows && !isWindowsTerminal;

        return new TerminalProfile
        {
            SupportsTrueColor = trueColor,
            IsWindowsTerminal = isWindowsTerminal,
            IsLegacyWindowsConsole = isLegacyWindowsConsole,
            IsUnixTerminal = isUnix,
            IsITerm2 = isITerm2,
            IsKitty = isKitty,
            ImageProtocol = protocol,
            RawTerm = term,
            RawColorTerm = colorTerm,
        };
    }
}
