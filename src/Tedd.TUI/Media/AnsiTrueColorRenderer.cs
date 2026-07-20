using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace Tedd.TUI.Media;

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

        // Bounding box of the cells emitted this frame; EmitGraphics uses it to decide
        // which image placements were overdrawn by text and need re-emission.
        _dirtyMinX = int.MaxValue;
        _dirtyMinY = int.MaxValue;
        _dirtyMaxX = int.MinValue;
        _dirtyMaxY = int.MinValue;

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

                if (x < _dirtyMinX) _dirtyMinX = x;
                if (x > _dirtyMaxX) _dirtyMaxX = x;
                if (y < _dirtyMinY) _dirtyMinY = y;
                if (y > _dirtyMaxY) _dirtyMaxY = y;

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

    // Placements emitted on the previous frame, used to skip redundant re-encodes and
    // to damage the cells of images that were removed or moved.
    private GraphicPlacement[] _lastGraphics = Array.Empty<GraphicPlacement>();

    private int _dirtyMinX, _dirtyMinY, _dirtyMaxX, _dirtyMaxY;

    private void EmitGraphics(VirtualBuffer buffer)
    {
        var encoder = ImageEncoder;
        var graphics = buffer.Graphics;
        int count = (encoder != null && graphics != null) ? graphics.Count : 0;

        bool placementsChanged = !PlacementsEqualLastFrame(graphics, count);

        if (placementsChanged)
        {
            // Cells under previous placements may still show image pixels that no new
            // placement will overwrite (image removed or moved); damage them so the
            // next frame re-emits the underlying text.
            for (int i = 0; i < _lastGraphics.Length; i++)
            {
                InvalidateRect(_lastGraphics[i].CharX, _lastGraphics[i].CharY, _lastGraphics[i].CharWidth, _lastGraphics[i].CharHeight);
            }

            _lastGraphics = count == 0 ? Array.Empty<GraphicPlacement>() : graphics!.ToArray();
        }

        if (count == 0) return;

        StringBuilder? sb = null;
        for (int i = 0; i < count; i++)
        {
            var placement = graphics![i];

            // Re-encode and re-emit only when needed: the set of placements changed,
            // or this frame's text diff painted into the placement's rectangle
            // (which overwrites the image cells with text). Previously every frame
            // re-encoded every image and then invalidated the whole back-buffer, so
            // one blinking cell elsewhere forced full-screen redraws + Sixel encodes.
            bool touchedByText = RectIntersectsDirty(placement.CharX, placement.CharY, placement.CharWidth, placement.CharHeight);
            if (!placementsChanged && !touchedByText) continue;

            sb ??= new StringBuilder(1024);
            // Park the cursor at the placement's top-left cell so terminals that draw
            // images at the current cursor position (Sixel, iTerm2 inline, Kitty default)
            // land in the right spot.
            sb.Append('\x1b').Append('[').Append(placement.CharY + 1).Append(';').Append(placement.CharX + 1).Append('H');
            sb.Append(encoder!.Encode(placement));

            // Note: the rect is deliberately NOT damaged here. The back-buffer's text
            // state for these cells is still what the diff should compare against (the
            // image merely sits on top of it); damaging it would re-emit the text next
            // frame, erase the image, re-trigger this path, and so on every frame.
        }

        if (sb != null && sb.Length > 0)
        {
            _output.Write(sb);
            _output.Flush();

            // Image emission moves the cursor unpredictably; force an absolute move
            // before the next text run.
            _cursorX = -1;
            _cursorY = -1;
        }
    }

    private bool PlacementsEqualLastFrame(System.Collections.Generic.IList<GraphicPlacement>? graphics, int count)
    {
        if (count != _lastGraphics.Length) return false;

        for (int i = 0; i < count; i++)
        {
            var a = graphics![i];
            var b = _lastGraphics[i];
            // Payload arrays are compared by reference: image controls cache their
            // decoded buffers, so a different reference means different content.
            if (a.CharX != b.CharX || a.CharY != b.CharY ||
                a.CharWidth != b.CharWidth || a.CharHeight != b.CharHeight ||
                !ReferenceEquals(a.ImageData, b.ImageData) ||
                !ReferenceEquals(a.Pixels, b.Pixels) ||
                a.PixelWidth != b.PixelWidth || a.PixelHeight != b.PixelHeight ||
                a.Source != b.Source)
            {
                return false;
            }
        }
        return true;
    }

    private bool RectIntersectsDirty(int cellX, int cellY, int cellW, int cellH)
    {
        if (_dirtyMaxX < _dirtyMinX) return false; // nothing emitted this frame
        return cellX <= _dirtyMaxX && cellX + cellW > _dirtyMinX &&
               cellY <= _dirtyMaxY && cellY + cellH > _dirtyMinY;
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

    /// <summary>
    /// Marks a cell rectangle as needing re-emission on the next <see cref="Render"/>,
    /// leaving the rest of the back-buffer's diff state intact.
    /// </summary>
    private void InvalidateRect(int cellX, int cellY, int cellW, int cellH)
    {
        if (_backBuffer == null) return;
        var sentinel = TuiColor.FromArgb(0x00010203u);
        var damaged = new Cell('\0', sentinel, sentinel);

        int x0 = Math.Max(0, cellX);
        int y0 = Math.Max(0, cellY);
        int x1 = Math.Min(_backBufferWidth, cellX + cellW);
        int y1 = Math.Min(_backBufferHeight, cellY + cellH);

        for (int y = y0; y < y1; y++)
        {
            int row = y * _backBufferWidth;
            for (int x = x0; x < x1; x++)
            {
                _backBuffer[row + x] = damaged;
            }
        }
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
