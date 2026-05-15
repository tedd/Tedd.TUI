using System;
using System.IO;
using Tedd.TUI.Markdown;

namespace Tedd.TUI.Imaging;

/// <summary>
/// Resolves image sources by reading files from disk. Absolute paths are loaded directly;
/// relative paths are resolved against the supplied <c>baseDirectory</c> (or
/// <see cref="DefaultBaseDirectory"/> when that is null).
/// </summary>
public sealed class FileImageResolver : IImageResolver
{
    /// <summary>
    /// Default base directory used when the caller does not supply one. Set by
    /// <see cref="TuiImaging.RegisterDefaults(string?, bool)"/> at app startup.
    /// </summary>
    public string? DefaultBaseDirectory { get; set; }

    /// <summary>
    /// When true (default) and the source is an <c>http(s)://</c> URL, attempt to load a
    /// local file whose name matches the URL's basename from the base directory before
    /// giving up. Useful for blog content where images are mirrored next to the post even
    /// though the markdown still references the original CDN URL.
    /// </summary>
    public bool UrlBasenameFallback { get; set; } = true;

    public bool TryResolve(string source, string? baseDirectory, out byte[] data, out string? mediaType)
    {
        data = Array.Empty<byte>();
        mediaType = null;

        if (string.IsNullOrEmpty(source))
            return false;

        if (source.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        bool isHttp = source.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
                      || source.StartsWith("https://", StringComparison.OrdinalIgnoreCase);

        string path;
        try
        {
            if (isHttp)
            {
                if (!UrlBasenameFallback) return false;

                var basePath = baseDirectory ?? DefaultBaseDirectory;
                if (string.IsNullOrEmpty(basePath)) return false;

                string? filename = ExtractUrlFilename(source);
                if (string.IsNullOrEmpty(filename)) return false;

                path = Path.Combine(basePath, filename);
            }
            else if (Path.IsPathRooted(source))
            {
                path = source;
            }
            else
            {
                var basePath = baseDirectory ?? DefaultBaseDirectory;
                if (string.IsNullOrEmpty(basePath))
                    return false;
                path = Path.Combine(basePath, source);
            }
        }
        catch
        {
            return false;
        }

        if (!File.Exists(path))
            return false;

        try
        {
            data = File.ReadAllBytes(path);
        }
        catch
        {
            return false;
        }

        mediaType = InferMediaType(path);
        return true;
    }

    /// <summary>
    /// Extracts the trailing path segment (filename) from an http(s) URL, stripping
    /// any query string or fragment. Returns null when the URL cannot be parsed or has no
    /// usable filename.
    /// </summary>
    private static string? ExtractUrlFilename(string url)
    {
        try
        {
            var uri = new Uri(url, UriKind.Absolute);
            // AbsolutePath has the query/fragment already removed and is URL-encoded. Decode
            // so a literal name like "Tedd.UnsafeMemoryBenchmark.Tests_.png" survives if the
            // server originally percent-encoded any of its characters.
            string segment = Uri.UnescapeDataString(Path.GetFileName(uri.AbsolutePath));
            return string.IsNullOrEmpty(segment) ? null : segment;
        }
        catch
        {
            return null;
        }
    }

    private static string InferMediaType(string path)
    {
        var ext = Path.GetExtension(path).ToLowerInvariant();
        return ext switch
        {
            ".png" => "image/png",
            ".jpg" => "image/jpeg",
            ".jpeg" => "image/jpeg",
            ".gif" => "image/gif",
            ".bmp" => "image/bmp",
            ".webp" => "image/webp",
            ".tga" => "image/x-tga",
            ".tiff" => "image/tiff",
            ".tif" => "image/tiff",
            ".svg" => "image/svg+xml",
            _ => "application/octet-stream"
        };
    }
}
