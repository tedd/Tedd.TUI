using Xunit;
using Tedd.TUI;
using Tedd.TUI.Platform.Console;

namespace Tedd.TUI.Tests;

public class TuiAppCapabilitiesSyncTests
{
    private sealed class FakeGraphicsPlatform : ITuiPlatform
    {
        public SurfaceCapabilities Capabilities { get; } = new SurfaceCapabilities
        {
            SupportsGraphics = true,
            CharPixelWidth = 11,
            CharPixelHeight = 22,
        };

        public IImageProtocolEncoder? ImageEncoder => null;

        public IRenderer CreateRenderer() => new NoOpRenderer();

        public ITuiInputManager? CreateInputManager(TuiWindow window) => null;

        public void Initialize() { }

        public void Shutdown() { }

        public void Dispose() => Shutdown();
    }

    private sealed class NoOpRenderer : IRenderer
    {
        public void Render(VirtualBuffer buffer) { }
    }

    [Fact]
    public void TuiApp_Constructor_CopiesPlatformCapabilitiesOntoWindow()
    {
        var window = new TuiWindow();
        Assert.False(window.Capabilities.SupportsGraphics);

        _ = new TuiApp(window, new FakeGraphicsPlatform());

        Assert.True(window.Capabilities.SupportsGraphics);
        Assert.Equal(11, window.Capabilities.CharPixelWidth);
        Assert.Equal(22, window.Capabilities.CharPixelHeight);
    }
}
