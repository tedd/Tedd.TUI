using Tedd.TUI.Markdown;

namespace Tedd.TUI.Imaging;

/// <summary>
/// Convenience entry point for wiring image support into Tedd.TUI without each application
/// needing to plug the decoder and resolver manually. A single call to
/// <see cref="RegisterDefaults"/> at startup is usually all that is needed.
/// </summary>
public static class TuiImaging
{
    /// <summary>
    /// The shared <see cref="IImageDecoder"/> instance configured by <see cref="RegisterDefaults"/>.
    /// </summary>
    public static MagickNetDecoder Decoder { get; } = new MagickNetDecoder();

    /// <summary>
    /// The shared <see cref="FileImageResolver"/> instance configured by <see cref="RegisterDefaults"/>.
    /// </summary>
    public static FileImageResolver FileResolver { get; } = new FileImageResolver();

    /// <summary>
    /// The shared <see cref="HttpImageResolver"/> used to fetch <c>http</c>/<c>https</c> sources.
    /// </summary>
    public static HttpImageResolver HttpResolver { get; } = new HttpImageResolver();

    /// <summary>
    /// Installs <see cref="Decoder"/> and a composite of
    /// <see cref="FileResolver"/> + <see cref="HttpResolver"/> as the process-wide defaults
    /// on <see cref="Image"/>. When <paramref name="baseDirectory"/> is non-null it is used
    /// to resolve relative paths whenever the caller does not supply one (e.g. when no
    /// <see cref="MarkdownView.BaseDirectory"/> has been set).
    /// </summary>
    /// <param name="baseDirectory">Default base directory for relative file paths.</param>
    /// <param name="enableHttp">When true (default), <c>http(s)</c> URLs are fetched and
    /// rendered. Set to false to keep the resolver disk-only (e.g. in offline scenarios).</param>
    public static void RegisterDefaults(string? baseDirectory = null, bool enableHttp = true)
    {
        FileResolver.DefaultBaseDirectory = baseDirectory;
        Image.DefaultDecoder = Decoder;
        Image.DefaultResolver = enableHttp
            ? new CompositeImageResolver(FileResolver, HttpResolver)
            : FileResolver;
        if (baseDirectory != null)
        {
            Image.DefaultBaseDirectory = baseDirectory;
        }
    }
}
