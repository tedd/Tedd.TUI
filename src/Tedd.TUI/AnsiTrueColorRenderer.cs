using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace Tedd.TUI;

/// <summary>
/// Cross-platform ANSI/VT renderer for terminals that understand the standard SGR
/// 24-bit color sequences (<c>ESC [ 38;2;r;g;b m</c> for foreground,
/// <c>ESC [ 48;2;r;g;b m</c> for background) and absolute cursor positioning
/// (<c>ESC [ row;col H</c>). Diffs the supplied <see cref="VirtualBuffer"/> against a
/// retained back-buffer and emits only the changed cells.
/// </summary>
/// <remarks>
/// <para>This renderer bypasses <see cref="IConsole.ForegroundColor"/> entirely: it writes
/// raw escape sequences to the supplied output stream. That keeps the truecolor channel
/// alive end-to-end and avoids the 16-color quantization the legacy
/// <c>ConsoleRenderer</c> performs.</para>
/// <para>The <c>Tedd.TUI.Platform.WindowsTerminal</c> and
/// <c>Tedd.TUI.Platform.LinuxTerminal</c> backends both wrap this renderer; the only
/// platform-specific work they do beyond instantiating it is enabling VT mode (Windows)
/// and switching to the alt screen / raw input (Linux).</para>
/// </remarks>
public sealed class AnsiTrueColorRenderer : IRenderer
{
    private readonly TextWriter _output;
    private readonly StringBuilder _buffer = new(4096);

    private Cell[]? _backBuffer;
    private int _backBufferWidth;
    private int _backBufferHeight;

    private TuiColor _currentFg = TuiColor.FromArgb(0u);
    private TuiColor _currentBg = TuiColor.FromArgb(0u);
    private int _cursorX = -1;
    private int _cursorY = -1;

    /// <summary>True when the output stream and back-buffer have been flushed at least once.</summary>
    public bool HasRenderedFrame { get; private set; }

    /// <summary>
    /// Optional image protocol encoder (Sixel / Kitty / iTerm2). When set, the renderer
    /// emits every <see cref="GraphicPlacement"/> attached to a frame's buffer after the
    /// text cells have been flushed.
    /// </summary>
    public IImageProtocolEncoder? ImageEncoder { get; set; }

    public AnsiTrueColorRenderer() : this(System.Console.Out) { }

    public AnsiTrueColorRenderer(TextWriter output)
    {
        _output = output ?? throw new ArgumentNullException(nameof(output));
    }

    /// <inheritdoc />
    public void Render(VirtualBuffer buffer)
    {
        int w = buffer.Width;
        int h = buffer.Height;

        EnsureBackBuffer(w, h);

        _buffer.Clear();

        // We track the "pending run" position so we can emit cursor moves only when a
        // run breaks (color change or unchanged cell). Mirrors the legacy renderer's
        // chunking strategy but in pure ANSI.
        int runStartX = -1;
        int runStartY = -1;
        TuiColor runFg = _currentFg;
        TuiColor runBg = _currentBg;
        bool runActive = false;

        ref Cell src = ref MemoryMarshal.GetReference(buffer.Cells);
        ref Cell back = ref MemoryMarshal.GetArrayDataReference(_backBuffer!);

        for (int y = 0; y < h; y++)
        {
            int rowOffset = y * w;

            for (int x = 0; x < w; x++)
            {
                int idx = rowOffset + x;
                ref Cell newCell = ref Unsafe.Add(ref src, idx);
                ref Cell backCell = ref Unsafe.Add(ref back, idx);

                if (newCell.Character == backCell.Character &&
                    newCell.Foreground.Packed == backCell.Foreground.Packed &&
                    newCell.Background.Packed == backCell.Background.Packed)
                {
                    if (runActive)
                    {
                        runActive = false;
                    }
                    continue;
                }

                backCell = newCell;

                if (!runActive ||
                    runFg.Packed != newCell.Foreground.Packed ||
                    runBg.Packed != newCell.Background.Packed)
                {
                    // Start of a new run — emit cursor move + SGR.
                    EmitCursor(x, y);
                    EmitColors(newCell.Foreground, newCell.Background);

                    runStartX = x;
                    runStartY = y;
                    runFg = newCell.Foreground;
                    runBg = newCell.Background;
                    runActive = true;
                }

                AppendChar(newCell.Character);
                _cursorX = x + 1;
                _cursorY = y;
            }

            // Force a break at end of line so we don't depend on terminal autowrap behavior.
            runActive = false;
        }

        if (_buffer.Length > 0)
        {
            _output.Write(_buffer);
            _output.Flush();
        }

        EmitGraphics(buffer);

        HasRenderedFrame = true;
    }

    private void EmitGraphics(VirtualBuffer buffer)
    {
        var encoder = ImageEncoder;
        var graphics = buffer.Graphics;
        if (encoder == null || graphics == null || graphics.Count == 0) return;

        var sb = new StringBuilder(1024);
        for (int i = 0; i < graphics.Count; i++)
        {
            var placement = graphics[i];
            // Park the cursor at the placement's top-left cell so terminals that draw
            // images at the current cursor position (Sixel, iTerm2 inline, Kitty default)
            // land in the right spot.
            sb.Append('\x1b').Append('[').Append(placement.CharY + 1).Append(';').Append(placement.CharX + 1).Append('H');
            sb.Append(encoder.Encode(placement));
        }

        if (sb.Length > 0)
        {
            _output.Write(sb);
            _output.Flush();
        }

        // Image emission moves the cursor unpredictably and may overwrite cells our
        // back-buffer believes are still valid; invalidate so the next frame redraws
        // every affected region.
        Invalidate();
    }

    /// <summary>
    /// Forces the next <see cref="Render"/> call to issue every cell, regardless of the
    /// existing back-buffer contents. Useful after the terminal has been resized or after
    /// an image protocol payload has scribbled over the grid.
    /// </summary>
    public void Invalidate()
    {
        if (_backBuffer == null) return;
        var sentinel = TuiColor.FromArgb(0x00010203u);
        Array.Fill(_backBuffer, new Cell('\0', sentinel, sentinel));
        _cursorX = -1;
        _cursorY = -1;
    }

    private void EnsureBackBuffer(int w, int h)
    {
        if (_backBuffer == null || _backBufferWidth != w || _backBufferHeight != h)
        {
            _backBuffer = new Cell[w * h];
            _backBufferWidth = w;
            _backBufferHeight = h;
            Invalidate();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void AppendChar(char c)
    {
        // Replace control characters with spaces — emitting them raw would confuse the
        // terminal (and the diff loop already turned \0 into "needs redraw" sentinels).
        _buffer.Append(c < 0x20 ? ' ' : c);
    }

    private void EmitCursor(int x, int y)
    {
        // ANSI cursor positioning is 1-based.
        _buffer.Append('\x1b').Append('[').Append(y + 1).Append(';').Append(x + 1).Append('H');
        _cursorX = x;
        _cursorY = y;
    }

    private void EmitColors(TuiColor fg, TuiColor bg)
    {
        if (fg.Packed != _currentFg.Packed)
        {
            _buffer.Append("\x1b[38;2;").Append(fg.R).Append(';').Append(fg.G).Append(';').Append(fg.B).Append('m');
            _currentFg = fg;
        }
        if (bg.Packed != _currentBg.Packed)
        {
            _buffer.Append("\x1b[48;2;").Append(bg.R).Append(';').Append(bg.G).Append(';').Append(bg.B).Append('m');
            _currentBg = bg;
        }
    }
}
