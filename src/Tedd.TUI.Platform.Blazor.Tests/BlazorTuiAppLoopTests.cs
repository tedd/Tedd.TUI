using System.Collections.Generic;
using Tedd.TUI;
using Tedd.TUI.Platform.Blazor;

namespace Tedd.TUI.Platform.Blazor.Tests;

/// <summary>
/// Guards the liveness of <see cref="BlazorTuiApp"/>'s render loop.
///
/// The loop waits on a semaphore that every invalidation signals, so when a frame's own
/// rendering invalidates the window the wait is already satisfied on the next pass. On
/// WebAssembly the loop shares the single UI thread with the browser and `await` on an
/// already-completed task resumes synchronously, so without an explicit yield the loop
/// never returns to the event loop and the tab wedges — no input, no timers, no repaint.
///
/// These tests cannot reproduce that starvation: the test host is multi-threaded, so the
/// loop makes progress on a pool thread regardless. What they do pin down is the behaviour
/// a regression here would break — that a continuously invalidating window keeps producing
/// frames rather than deadlocking or stopping after one, and that the loop shuts down when
/// asked. Removing the yield, or moving the try/catch back outside the while, shows up as
/// a hang or a stalled frame count rather than passing silently.
/// </summary>
public class BlazorTuiAppLoopTests
{
    /// <summary>
    /// Renderer that invalidates the window from inside rendering, reproducing the
    /// "every frame re-arms the loop" condition that used to starve the browser.
    /// </summary>
    private sealed class ReinvalidatingRenderer : IRendererAsync, ILayeredRenderer
    {
        private readonly TuiWindow _window;
        private readonly TaskCompletionSource _framesReached = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly int _targetFrames;
        private int _frames;

        public ReinvalidatingRenderer(TuiWindow window, int targetFrames)
        {
            _window = window;
            _targetFrames = targetFrames;
        }

        public int Frames => Volatile.Read(ref _frames);
        public Task FramesReached => _framesReached.Task;

        public Task<(int CharWidth, int CharHeight)> InitAsync(int width, int height) =>
            Task.FromResult((1, 1));

        public Task RenderAsync(VirtualBuffer buffer) => Count();

        public Task RenderLayersAsync(List<RenderLayer> layers) => Count();

        private Task Count()
        {
            int n = Interlocked.Increment(ref _frames);
            if (n >= _targetFrames)
                _framesReached.TrySetResult();

            // The behaviour under test: rendering itself dirties the window, so the loop
            // is immediately eligible to run again.
            _window.Invalidate();
            return Task.CompletedTask;
        }
    }

    [Fact]
    public async Task LoopKeepsProducingFramesWhenEveryFrameInvalidates()
    {
        var window = new TuiWindow { Content = new TextBlock { Text = "hello" } };
        var renderer = new ReinvalidatingRenderer(window, targetFrames: 5);
        using var app = new BlazorTuiApp(window, renderer);

        await app.StartAsync(20, 5);

        // Bounded wait: a loop that deadlocks or renders once then stalls fails here
        // instead of hanging the suite.
        var completed = await Task.WhenAny(renderer.FramesReached, Task.Delay(TimeSpan.FromSeconds(5)));

        app.Stop();

        Assert.Same(renderer.FramesReached, completed);
        Assert.True(renderer.Frames >= 5, $"expected at least 5 frames, saw {renderer.Frames}");
    }

    [Fact]
    public async Task StopEndsTheLoopEvenWhileFramesKeepArriving()
    {
        var window = new TuiWindow { Content = new TextBlock { Text = "hello" } };
        var renderer = new ReinvalidatingRenderer(window, targetFrames: 3);
        var app = new BlazorTuiApp(window, renderer);

        await app.StartAsync(20, 5);
        await Task.WhenAny(renderer.FramesReached, Task.Delay(TimeSpan.FromSeconds(5)));

        app.Stop();

        // Give the loop a moment to observe the stop, then confirm it really stopped
        // rather than merely slowed: the frame count must settle.
        await Task.Delay(150);
        int settled = renderer.Frames;
        await Task.Delay(150);

        Assert.Equal(settled, renderer.Frames);

        app.Dispose();
    }

    [Fact]
    public async Task DisposeAfterStopDoesNotThrowFromTheLoop()
    {
        var window = new TuiWindow { Content = new TextBlock { Text = "hello" } };
        var renderer = new ReinvalidatingRenderer(window, targetFrames: 2);
        var app = new BlazorTuiApp(window, renderer);

        await app.StartAsync(20, 5);
        await Task.WhenAny(renderer.FramesReached, Task.Delay(TimeSpan.FromSeconds(5)));

        // Disposing tears down the semaphore the loop is waiting on; the loop is expected
        // to treat that as shutdown rather than surfacing an ObjectDisposedException.
        app.Dispose();
        await Task.Delay(150);
    }
}
