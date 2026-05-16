using System;
using System.Runtime.InteropServices;

namespace Tedd.TUI.Platform.LinuxTerminal;

/// <summary>
/// Minimal libc (<c>libSystem</c> on macOS / <c>libc</c> on glibc + musl) P/Invokes used by
/// <see cref="LinuxTerminalPlatform"/> to switch stdin into raw mode and back, and to
/// register a SIGWINCH resize watcher.
/// </summary>
/// <remarks>
/// <para>The struct layout matches the System V <c>termios</c> on Linux x86_64. macOS
/// uses a slightly different layout (smaller <c>c_cc</c> array, no <c>c_line</c> field),
/// so on macOS we operate exclusively via the raw byte buffer that <c>tcgetattr</c>
/// fills in; we only flip flag bits whose offsets coincide. For platforms where the
/// flags don't line up the platform falls back to leaving the terminal alone — the
/// renderer still works (line-buffered) just with input echo turned on.</para>
/// </remarks>
internal static class Termios
{
    private const string Libc = "libc";

    public const int STDIN_FILENO = 0;
    public const int STDOUT_FILENO = 1;

    public const int TCSANOW = 0;

    // c_lflag bits (Linux + macOS use the same values).
    public const uint ECHO = 0x00000008;
    public const uint ICANON = 0x00000002;
    public const uint ISIG = 0x00000001;
    public const uint IEXTEN = 0x00008000;

    // c_iflag bits.
    public const uint IXON = 0x00000400;
    public const uint ICRNL = 0x00000100;
    public const uint BRKINT = 0x00000002;
    public const uint INPCK = 0x00000010;
    public const uint ISTRIP = 0x00000020;

    // c_oflag bits.
    public const uint OPOST = 0x00000001;

    public const int SIGWINCH = 28;

    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct LinuxTermios
    {
        public uint c_iflag;
        public uint c_oflag;
        public uint c_cflag;
        public uint c_lflag;
        public byte c_line;
        public fixed byte c_cc[32];
        public uint c_ispeed;
        public uint c_ospeed;
    }

    [DllImport(Libc, SetLastError = true)]
    public static extern int tcgetattr(int fd, out LinuxTermios termios);

    [DllImport(Libc, SetLastError = true)]
    public static extern int tcsetattr(int fd, int optional_actions, in LinuxTermios termios);

    [DllImport(Libc, SetLastError = true)]
    public static extern int isatty(int fd);

    public delegate void SignalHandler(int signum);

    [DllImport(Libc, EntryPoint = "signal", SetLastError = true)]
    public static extern IntPtr signal_raw(int signum, SignalHandler handler);
}
