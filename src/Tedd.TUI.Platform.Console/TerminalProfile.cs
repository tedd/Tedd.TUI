using System;
using Tedd.TUI;

namespace Tedd.TUI.Platform.Console;

/// <summary>
/// Snapshot of the host terminal's advertised capabilities, populated once at startup
/// by <see cref="TerminalProbe"/>. The auto-detecting <see cref="PlatformLoader"/>
/// uses this to choose between the truecolor backends and the legacy 16-color
/// <see cref="ConsoleRenderer"/> fallback.
/// </summary>
/// <remarks>
/// <para>Probing happens once per process and is deliberately cheap: it inspects a
/// handful of environment variables (<c>$TERM</c>, <c>$COLORTERM</c>, <c>$WT_SESSION</c>,
/// <c>$WT_PROFILE_ID</c>, <c>$LC_TERMINAL</c>, <c>$KITTY_WINDOW_ID</c>, <c>$TERM_PROGRAM</c>) plus the host
/// OS. It deliberately avoids the more elaborate DA1 / DA2 / XTSMGRAPHICS round-trips,
/// because those require taking over the input stream, which doesn't compose well
/// with the existing input manager. Callers that want the heavy probing path will be
/// able to opt in once the dedicated terminal backends land in Phase 5/6.</para>
/// </remarks>
public sealed class TerminalProfile
{
    /// <summary>True when the host announces 24-bit color support (<c>COLORTERM=truecolor</c> etc.).</summary>
    public bool SupportsTrueColor { get; init; }

    /// <summary>True when the host is Windows Terminal (sets <c>WT_SESSION</c> and/or <c>WT_PROFILE_ID</c>).</summary>
    public bool IsWindowsTerminal { get; init; }

    /// <summary>True when the host is the legacy Windows console (conhost).</summary>
    public bool IsLegacyWindowsConsole { get; init; }

    /// <summary>True when the host is a unix-style terminal (Linux/macOS).</summary>
    public bool IsUnixTerminal { get; init; }

    /// <summary>True when the host is iTerm2 (<c>$LC_TERMINAL=iTerm2</c>).</summary>
    public bool IsITerm2 { get; init; }

    /// <summary>True when the host is the Kitty terminal (<c>$KITTY_WINDOW_ID</c>).</summary>
    public bool IsKitty { get; init; }

    /// <summary>Best guess at the image protocol the host can display, if any.</summary>
    public TerminalImageProtocol ImageProtocol { get; init; } = TerminalImageProtocol.None;

    /// <summary>The raw <c>$TERM</c> value the probe observed, for diagnostics.</summary>
    public string? RawTerm { get; init; }

    /// <summary>The raw <c>$COLORTERM</c> value the probe observed, for diagnostics.</summary>
    public string? RawColorTerm { get; init; }

    /// <summary>Convenience: legacy 16-color fallback when neither truecolor nor a known modern host applies.</summary>
    public bool IsLegacy16Color => !SupportsTrueColor && !IsWindowsTerminal && !IsITerm2 && !IsKitty;
}

/// <summary>Image transport protocol families known to <see cref="TerminalProfile"/>.</summary>
public enum TerminalImageProtocol
{
    /// <summary>Host can't render bitmaps; image controls fall back to ASCII art.</summary>
    None,
    /// <summary>DEC Sixel (Windows Terminal 1.22+, xterm, mlterm, contour, …).</summary>
    Sixel,
    /// <summary>Kitty graphics protocol.</summary>
    Kitty,
    /// <summary>iTerm2 inline image protocol.</summary>
    ITerm2,
}
