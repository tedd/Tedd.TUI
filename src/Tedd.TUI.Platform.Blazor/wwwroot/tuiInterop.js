
window.tuiInterop = {
    canvasContexts: {},
    charWidth: 10,
    charHeight: 18,
    font: '16px Consolas, monospace',
    colors: [
        '#000000', // Black
        '#00008B', // DarkBlue
        '#006400', // DarkGreen
        '#008B8B', // DarkCyan
        '#8B0000', // DarkRed
        '#8B008B', // DarkMagenta
        '#BDB76B', // DarkYellow (using a dimmer yellow)
        '#C0C0C0', // Gray
        '#808080', // DarkGray
        '#0000FF', // Blue
        '#00FF00', // Green
        '#00FFFF', // Cyan
        '#FF0000', // Red
        '#FF00FF', // Magenta
        '#FFFF00', // Yellow
        '#FFFFFF'  // White
    ],

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
        const colors = this.colors;

        let ptr = 0;
        for (let y = 0; y < height; y++) {
            for (let x = 0; x < width; x++) {
                const charCode = data[ptr++];
                const fg = data[ptr++];
                const bg = data[ptr++];

                // Draw background
                ctx.fillStyle = colors[bg];
                ctx.fillRect(x * cw, y * ch, cw, ch);

                // Draw foreground char
                if (charCode !== 32) { // Skip space
                    ctx.fillStyle = colors[fg];
                    ctx.fillText(String.fromCharCode(charCode), x * cw, y * ch);
                }
            }
        }
    },

    listenForResize: function (dotnetHelper, canvasId) {
        const resizeHandler = () => {
            // We default to full window for now, as TUI is usually full screen.
            // Ideally we'd use the container size, but that requires the container to have explicit size.
            const w = window.innerWidth;
            const h = window.innerHeight;

            const cols = Math.floor(w / this.charWidth);
            const rows = Math.floor(h / this.charHeight);

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
    }
};
