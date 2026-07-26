
window.tuiInterop = {
    canvasContexts: {},
    charWidth: 10,
    charHeight: 18,
    font: '16px Consolas, monospace',

    // Convert a packed 0xAARRGGBB integer (as sent from BlazorRenderer) to a CSS rgba() string.
    // Bit layout matches Tedd.TUI.TuiColor.Packed exactly.
    packedToRgba: function (packed) {
        // JS bitwise ops are signed 32-bit; use unsigned shift for the alpha byte.
        const a = (packed >>> 24) & 0xff;
        const r = (packed >>> 16) & 0xff;
        const g = (packed >>> 8) & 0xff;
        const b = packed & 0xff;
        return 'rgba(' + r + ',' + g + ',' + b + ',' + (a / 255) + ')';
    },

    init: function (canvasId, width, height) {
        const canvas = document.getElementById(canvasId);
        if (!canvas) return;

        // Set high DPI support if needed, but for now simple 1:1
        // We might want to scale the canvas based on char size.
        // Let's measure 'M' to get char size?
        // Or we enforce a size.
        const ctx = canvas.getContext('2d');
        ctx.font = this.font;

        // Measure char size
        const metrics = ctx.measureText('M');
        // width is explicit, height is approximate from font size
        this.charWidth = Math.ceil(metrics.width);
        // Heuristic for height or strict line height
        this.charHeight = 18; // 16px font + 2px buffer

        // Resize canvas to fit the grid
        canvas.width = width * this.charWidth;
        canvas.height = height * this.charHeight;

        // Reset font after resize (canvas clears state on resize)
        ctx.font = this.font;
        ctx.textBaseline = 'top';

        this.canvasContexts[canvasId] = ctx;

        return { charWidth: this.charWidth, charHeight: this.charHeight };
    },

    measureDom: function () {
        // Measure pixel size of a character in the DOM environment to ensure alignment
        const div = document.createElement('div');
        div.style.fontFamily = "'Consolas', monospace";
        div.style.fontSize = '16px';
        div.style.lineHeight = '18px';
        div.style.position = 'absolute';
        div.style.whiteSpace = 'pre';
        div.style.visibility = 'hidden';
        div.innerText = 'M';
        document.body.appendChild(div);

        const rect = div.getBoundingClientRect();
        const w = rect.width;
        const h = rect.height;

        document.body.removeChild(div);

        // Keep our own copy in sync so any caller that doesn't pass explicit metrics
        // (e.g. listenForResize's fallback) uses the measured size, not the defaults.
        this.charWidth = w;
        this.charHeight = h;

        return { charWidth: w, charHeight: h };
    },

    render: function (canvasId, width, height, data) {
        const ctx = this.canvasContexts[canvasId];
        if (!ctx) return;

        // Check size and resize if needed
        const canvas = ctx.canvas;
        const requiredWidth = width * this.charWidth;
        const requiredHeight = height * this.charHeight;

        if (canvas.width !== requiredWidth || canvas.height !== requiredHeight) {
            canvas.width = requiredWidth;
            canvas.height = requiredHeight;
            // Restore context state after resize
            ctx.font = this.font;
            ctx.textBaseline = 'top';
        }

        // Clear? Or overwrite. Overwrite is faster usually but TUI clears often.
        // We will overwrite everything because we draw background rectangles.

        // data is a flat Int32Array passed from Blazor
        // Format: [char, fg, bg, char, fg, bg, ...]

        const cw = this.charWidth;
        const ch = this.charHeight;

        let ptr = 0;
        for (let y = 0; y < height; y++) {
            for (let x = 0; x < width; x++) {
                const charCode = data[ptr++];
                const fg = data[ptr++];
                const bg = data[ptr++];

                // Draw background
                ctx.fillStyle = this.packedToRgba(bg);
                ctx.fillRect(x * cw, y * ch, cw, ch);

                // Draw foreground char
                if (charCode !== 32) { // Skip space
                    ctx.fillStyle = this.packedToRgba(fg);
                    ctx.fillText(String.fromCharCode(charCode), x * cw, y * ch);
                }
            }
        }
    },

    // Browser-side cache of decoded HTMLImageElements keyed by stable identity (the
    // .NET side hands us `Key`, typically the original Source URL or a hash code).
    // Drawing is otherwise allocation-free per frame: we just look up + drawImage.
    imageCache: {},

    renderGraphics: function (canvasId, cw, ch, placements) {
        const ctx = this.canvasContexts[canvasId];
        if (!ctx) return;

        const cache = this.imageCache;
        for (let i = 0; i < placements.length; i++) {
            const p = placements[i];
            const key = p.key || p.src;
            if (!key || !p.src) continue;

            let entry = cache[key];
            if (!entry || entry.src !== p.src) {
                const img = new Image();
                entry = { img: img, src: p.src, loaded: false };
                cache[key] = entry;
                img.onload = (function (entryRef, canvasId, ctxRef, px, py, pw, ph, cww, chh) {
                    return function () {
                        entryRef.loaded = true;
                        // Once the image decodes we draw it once at the placement we
                        // captured at request time. Subsequent frames that re-request
                        // the same image hit the cache and draw synchronously below.
                        try {
                            ctxRef.drawImage(entryRef.img, px * cww, py * chh, pw * cww, ph * chh);
                        } catch (e) { /* canvas may have been resized */ }
                    };
                })(entry, canvasId, ctx, p.x, p.y, p.w, p.h, cw, ch);
                img.src = p.src;
            }

            if (entry.loaded) {
                try {
                    ctx.drawImage(entry.img, p.x * cw, p.y * ch, p.w * cw, p.h * ch);
                } catch (e) { /* ignore intermittent canvas state errors */ }
            }
        }
    },

    renderDiff: function (canvasId, data) {
        const ctx = this.canvasContexts[canvasId];
        if (!ctx) return;

        const cw = this.charWidth;
        const ch = this.charHeight;

        let ptr = 0;
        const len = data.length;

        while (ptr < len) {
            const x = data[ptr++];
            const y = data[ptr++];
            const charCode = data[ptr++];
            const fg = data[ptr++];
            const bg = data[ptr++];

            // Draw background
            ctx.fillStyle = this.packedToRgba(bg);
            ctx.fillRect(x * cw, y * ch, cw, ch);

            // Draw foreground char
            if (charCode !== 32) { // Skip space
                ctx.fillStyle = this.packedToRgba(fg);
                ctx.fillText(String.fromCharCode(charCode), x * cw, y * ch);
            }
        }
    },

    listenForResize: function (dotnetHelper, canvasId, cellWidth, cellHeight) {
        // The caller passes the cell metrics the renderer actually draws with; only fall
        // back to our own measurements when they are absent (older callers). Deriving the
        // grid from a different cell size than the renderer uses makes the drawn surface
        // disagree with the requested column/row count.
        const cw = (cellWidth > 0) ? cellWidth : this.charWidth;
        const chh = (cellHeight > 0) ? cellHeight : this.charHeight;

        const resizeHandler = () => {
            // We default to full window for now, as TUI is usually full screen.
            // Ideally we'd use the container size, but that requires the container to have explicit size.
            const w = window.innerWidth;
            const h = window.innerHeight;

            const cols = Math.floor(w / cw);
            const rows = Math.floor(h / chh);

            // Only notify if valid
            if (cols > 0 && rows > 0) {
                dotnetHelper.invokeMethodAsync('OnBrowserResize', cols, rows);
            }
        };

        // Debounce?
        let timeout;
        const debouncedHandler = () => {
            clearTimeout(timeout);
            timeout = setTimeout(resizeHandler, 100);
        };

        window.addEventListener('resize', debouncedHandler);

        // Initial check
        resizeHandler();

        this.resizeHandlers = this.resizeHandlers || {};
        this.resizeHandlers[canvasId] = debouncedHandler;
    },

    disposeResizeListener: function (canvasId) {
        if (this.resizeHandlers && this.resizeHandlers[canvasId]) {
            window.removeEventListener('resize', this.resizeHandlers[canvasId]);
            delete this.resizeHandlers[canvasId];
        }
    },

    // Global Mouse Handling for Dragging
    globalMouseState: null,

    startGlobalDrag: function (dotnetHelper, containerId) {
        const container = document.getElementById(containerId);
        if (!container) return;

        // The surface suppresses the mousedown default action so the browser cannot start a
        // native text selection or drag over the grid -- that gesture belongs to the TUI, and
        // once the browser owns it, it stops delivering the moves a scrollbar drag needs.
        // Suppressing the default also suppresses the focus it would have given us, so take
        // focus explicitly or the surface stops receiving keys after the first click.
        container.focus({ preventScroll: true });

        if (this.globalMouseState) return; // Already tracking

        // Coalesce mousemove to at most one .NET call per animation frame. Raw mouse
        // events arrive at 60-120Hz; forwarding each one triggers a TUI re-render and
        // the queue outruns the renderer, freezing the page during drags/selection.
        // requestAnimationFrame also gives natural backpressure: while WASM hogs the
        // main thread no frames are produced, so no further moves pile up.
        let pendingMove = null;
        let moveScheduled = false;

        const onMove = (e) => {
            pendingMove = { clientX: e.clientX, clientY: e.clientY,
                            ctrlKey: e.ctrlKey, shiftKey: e.shiftKey, altKey: e.altKey };
            if (moveScheduled) return;
            moveScheduled = true;
            requestAnimationFrame(() => {
                moveScheduled = false;
                if (!pendingMove || !this.globalMouseState) return;
                const rect = container.getBoundingClientRect();
                const x = pendingMove.clientX - rect.left;
                const y = pendingMove.clientY - rect.top;
                const m = pendingMove;
                pendingMove = null;
                dotnetHelper.invokeMethodAsync('OnGlobalMouse', 'mousemove', x, y,
                    0, m.ctrlKey, m.shiftKey, m.altKey);
            });
        };

        const onUp = (e) => {
            const rect = container.getBoundingClientRect();
            const x = e.clientX - rect.left;
            const y = e.clientY - rect.top;

            dotnetHelper.invokeMethodAsync('OnGlobalMouse', 'mouseup', x, y,
                e.button, e.ctrlKey, e.shiftKey, e.altKey);
            this.stopGlobalDrag();
        };

        this.globalMouseState = { move: onMove, up: onUp };
        document.addEventListener('mousemove', onMove);
        document.addEventListener('mouseup', onUp);
    },

    stopGlobalDrag: function () {
        if (this.globalMouseState) {
            document.removeEventListener('mousemove', this.globalMouseState.move);
            document.removeEventListener('mouseup', this.globalMouseState.up);
            this.globalMouseState = null;
        }
    },

    // Hover tracking. Without this the TUI only ever sees a mouse move while a button is held
    // (startGlobalDrag is the only other source), so IsMouseOver never updates on a bare move:
    // no hover highlights, no hover-revealed affordances, no link hover.
    //
    // Coalesced to one call per animation frame for the same reason drags are: raw moves arrive
    // at 60-120Hz, and forwarding each one queues a TUI pass faster than they complete.
    hoverStates: {},

    startHoverTracking: function (dotnetHelper, containerId) {
        const container = document.getElementById(containerId);
        if (!container) return;

        // Keyed per container, like the resize listeners: two surfaces on one page must not
        // evict each other's tracking.
        this.stopHoverTracking(containerId);

        let pending = null;
        let scheduled = false;

        const onMove = (e) => {
            // A drag owns the pointer and already forwards its moves; delivering them twice
            // would make hover fight whatever captured the mouse.
            if (this.globalMouseState) return;

            pending = { clientX: e.clientX, clientY: e.clientY,
                        ctrlKey: e.ctrlKey, shiftKey: e.shiftKey, altKey: e.altKey };
            if (scheduled) return;
            scheduled = true;
            requestAnimationFrame(() => {
                scheduled = false;
                if (!pending || !this.hoverStates[containerId]) return;
                const rect = container.getBoundingClientRect();
                const x = pending.clientX - rect.left;
                const y = pending.clientY - rect.top;
                const m = pending;
                pending = null;
                dotnetHelper.invokeMethodAsync('OnGlobalMouse', 'mousemove', x, y,
                    0, m.ctrlKey, m.shiftKey, m.altKey);
            });
        };

        const onLeave = () => {
            if (this.globalMouseState) return;
            pending = null;
            // Park the pointer far outside the grid so the hit test misses and whatever was
            // hovered gets its MouseLeave; otherwise a highlight stays lit after the pointer
            // has left the surface entirely.
            dotnetHelper.invokeMethodAsync('OnGlobalMouse', 'mousemove', -1e6, -1e6,
                0, false, false, false);
        };

        this.hoverStates[containerId] = { container: container, move: onMove, leave: onLeave };
        container.addEventListener('mousemove', onMove);
        container.addEventListener('mouseleave', onLeave);
    },

    stopHoverTracking: function (containerId) {
        const state = this.hoverStates[containerId];
        if (state) {
            state.container.removeEventListener('mousemove', state.move);
            state.container.removeEventListener('mouseleave', state.leave);
            delete this.hoverStates[containerId];
        }
    }
};
