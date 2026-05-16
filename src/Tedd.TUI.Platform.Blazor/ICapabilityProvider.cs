using Tedd.TUI;

namespace Tedd.TUI.Platform.Blazor;

/// <summary>
/// Implemented by renderers that expose <see cref="SurfaceCapabilities"/> to the hosting
/// <see cref="BlazorTuiApp"/>. The app reads these capabilities once after <c>InitAsync</c>
/// and stores them on the <see cref="TuiWindow"/> so controls can branch on them.
/// </summary>
public interface ICapabilityProvider
{
    SurfaceCapabilities Capabilities { get; }
}
