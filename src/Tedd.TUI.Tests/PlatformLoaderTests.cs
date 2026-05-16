using Xunit;
using Tedd.TUI;
using Tedd.TUI.Platform.Console;

namespace Tedd.TUI.Tests;

public class PlatformLoaderTests
{
    [Fact]
    public void TerminalProbe_Refresh_ReturnsNonNullProfile()
    {
        var profile = TerminalProbe.Refresh();
        Assert.NotNull(profile);
    }

    [Fact]
    public void PlatformLoader_ResolvesPlatform()
    {
        // We deliberately don't assert a specific concrete type because the loader
        // picks the best backend available to the test runner (Windows / Linux /
        // legacy console). What matters is that we always get a usable platform.
        using var platform = PlatformLoader.Load();
        Assert.NotNull(platform);
        Assert.NotNull(platform.Capabilities);
    }

    [Fact]
    public void PlatformLoader_ExplicitPlatform_IsHonored()
    {
        var explicitPlatform = new LegacyConsolePlatform();
        using var loaded = PlatformLoader.Load(explicitPlatform);
        Assert.Same(explicitPlatform, loaded);
    }

    [Fact]
    public void LegacyConsolePlatform_AdvertisesProfile()
    {
        var profile = new TerminalProfile { SupportsTrueColor = false };
        using var platform = new LegacyConsolePlatform(profile);
        Assert.Same(profile, platform.Profile);
    }
}
