using System;
using System.Runtime.InteropServices;

namespace Tedd.TUI.Platform.WindowsTerminal;

/// <summary>
/// Minimal kernel32 P/Invoke surface used by <see cref="WindowsTerminalPlatform"/> to
/// enable VT mode on the console handles. Mirrors the constants in
/// <c>Tedd.TUI.Platform.Console.NativeMethods</c> but kept here so this assembly is
/// self-contained for reflection-loading via <c>PlatformLoader</c>.
/// </summary>
internal static class Win32
{
    private const string Kernel32 = "kernel32.dll";

    public const int STD_INPUT_HANDLE = -10;
    public const int STD_OUTPUT_HANDLE = -11;

    public const uint ENABLE_QUICK_EDIT_MODE = 0x0040;
    public const uint ENABLE_EXTENDED_FLAGS = 0x0080;
    public const uint ENABLE_VIRTUAL_TERMINAL_INPUT = 0x0200;

    public const uint ENABLE_VIRTUAL_TERMINAL_PROCESSING = 0x0004;
    public const uint DISABLE_NEWLINE_AUTO_RETURN = 0x0008;

    [DllImport(Kernel32, SetLastError = true)]
    public static extern IntPtr GetStdHandle(int nStdHandle);

    [DllImport(Kernel32, SetLastError = true)]
    public static extern bool GetConsoleMode(IntPtr hConsoleHandle, out uint lpMode);

    [DllImport(Kernel32, SetLastError = true)]
    public static extern bool SetConsoleMode(IntPtr hConsoleHandle, uint dwMode);
}
