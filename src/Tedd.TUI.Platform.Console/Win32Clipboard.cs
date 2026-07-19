using System;
using System.Runtime.InteropServices;
using System.Threading;
using Tedd.TUI;

namespace Tedd.TUI.Platform.Console;

/// <summary>
/// <see cref="IClipboard"/> bridge to the Win32 clipboard (<c>CF_UNICODETEXT</c>).
/// Registered by the Windows console platforms (conhost, Windows Terminal) so copy /
/// paste interoperates with the rest of the desktop. All failures throw or return
/// <c>null</c>; the static <see cref="Clipboard"/> service falls back to its in-process
/// buffer in that case.
/// </summary>
public sealed class Win32Clipboard : IClipboard
{
    private const string User32 = "user32.dll";
    private const string Kernel32 = "kernel32.dll";

    private const uint CF_UNICODETEXT = 13;
    private const uint GMEM_MOVEABLE = 0x0002;

    public string? GetText()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return null;
        if (!TryOpenClipboard()) return null;

        try
        {
            IntPtr handle = GetClipboardData(CF_UNICODETEXT);
            // The clipboard was readable but holds no text (empty, or e.g. an image).
            // Empty string is authoritative here — returning null would paste stale
            // text from the in-process fallback buffer instead of nothing.
            if (handle == IntPtr.Zero) return string.Empty;

            IntPtr ptr = GlobalLock(handle);
            if (ptr == IntPtr.Zero) return null;
            try
            {
                return Marshal.PtrToStringUni(ptr) ?? string.Empty;
            }
            finally
            {
                GlobalUnlock(handle);
            }
        }
        finally
        {
            CloseClipboard();
        }
    }

    public void SetText(string text)
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return;
        text ??= string.Empty;

        int bytes = (text.Length + 1) * sizeof(char);
        IntPtr hGlobal = GlobalAlloc(GMEM_MOVEABLE, (UIntPtr)bytes);
        if (hGlobal == IntPtr.Zero) throw new OutOfMemoryException("GlobalAlloc failed for clipboard text.");

        try
        {
            IntPtr target = GlobalLock(hGlobal);
            if (target == IntPtr.Zero) throw new InvalidOperationException("GlobalLock failed for clipboard text.");
            try
            {
                Marshal.Copy(text.ToCharArray(), 0, target, text.Length);
                Marshal.WriteInt16(target, text.Length * sizeof(char), 0);
            }
            finally
            {
                GlobalUnlock(hGlobal);
            }

            if (!TryOpenClipboard()) throw new InvalidOperationException("OpenClipboard failed.");
            try
            {
                EmptyClipboard();
                if (SetClipboardData(CF_UNICODETEXT, hGlobal) == IntPtr.Zero)
                {
                    throw new InvalidOperationException("SetClipboardData failed.");
                }
                // Ownership of the memory transferred to the OS.
                hGlobal = IntPtr.Zero;
            }
            finally
            {
                CloseClipboard();
            }
        }
        finally
        {
            if (hGlobal != IntPtr.Zero) GlobalFree(hGlobal);
        }
    }

    // Another process may hold the clipboard open for a moment; retry briefly instead
    // of failing the copy outright.
    private static bool TryOpenClipboard()
    {
        for (int attempt = 0; attempt < 5; attempt++)
        {
            if (OpenClipboard(IntPtr.Zero)) return true;
            Thread.Sleep(10);
        }
        return false;
    }

    [DllImport(User32, SetLastError = true)]
    private static extern bool OpenClipboard(IntPtr hWndNewOwner);

    [DllImport(User32, SetLastError = true)]
    private static extern bool CloseClipboard();

    [DllImport(User32, SetLastError = true)]
    private static extern bool EmptyClipboard();

    [DllImport(User32, SetLastError = true)]
    private static extern IntPtr GetClipboardData(uint uFormat);

    [DllImport(User32, SetLastError = true)]
    private static extern IntPtr SetClipboardData(uint uFormat, IntPtr hMem);

    [DllImport(Kernel32, SetLastError = true)]
    private static extern IntPtr GlobalAlloc(uint uFlags, UIntPtr dwBytes);

    [DllImport(Kernel32, SetLastError = true)]
    private static extern IntPtr GlobalFree(IntPtr hMem);

    [DllImport(Kernel32, SetLastError = true)]
    private static extern IntPtr GlobalLock(IntPtr hMem);

    [DllImport(Kernel32, SetLastError = true)]
    private static extern bool GlobalUnlock(IntPtr hMem);
}
