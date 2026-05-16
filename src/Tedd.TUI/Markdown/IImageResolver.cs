namespace Tedd.TUI.Markdown;

/// <summary>
/// Resolves an image <c>Source</c> string (URL or relative path) into raw bytes that
/// an <see cref="IImageDecoder"/> can consume. The resolver is also responsible for
/// reporting the MIME type so a surface that can render bitmaps natively (e.g. Blazor
/// DOM via &lt;img&gt;) can avoid re-encoding.
/// </summary>
public interface IImageResolver
{
    /// <summary>
    /// Attempts to load the image identified by <paramref name="source"/>. When the source
    /// is a relative path, <paramref name="baseDirectory"/> provides the anchor (typically
    /// the directory containing the markdown document).
    /// </summary>
    /// <param name="source">Source string from the markdown (URL, absolute or relative path).</param>
    /// <param name="baseDirectory">Optional directory used to resolve relative paths.</param>
    /// <param name="data">On success, the raw image bytes.</param>
    /// <param name="mediaType">On success, the MIME type (e.g. "image/png"). May be null when unknown.</param>
    /// <returns>True when the source was resolved.</returns>
    bool TryResolve(string source, string? baseDirectory, out byte[] data, out string? mediaType);
}
