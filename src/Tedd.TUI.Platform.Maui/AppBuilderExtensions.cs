using Microsoft.Maui.Hosting;
using SkiaSharp.Views.Maui.Controls.Hosting;

namespace Tedd.TUI.Platform.Maui;

public static class AppBuilderExtensions
{
    /// <summary>
    /// Registers the handlers <see cref="TuiHostView"/> needs (SkiaSharp views).
    /// Call on the <c>MauiAppBuilder</c> in <c>MauiProgram.CreateMauiApp</c>.
    /// </summary>
    public static MauiAppBuilder UseTeddTui(this MauiAppBuilder builder)
        => builder.UseSkiaSharp();
}
