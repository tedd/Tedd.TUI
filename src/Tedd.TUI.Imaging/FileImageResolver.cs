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
    /// <see cref="TuiImaging.RegisterDefaults(string?)"/> at app startup.
    /// </summary>
    public string? DefaultBaseDirectory { get; set; }

    public bool TryResolve(string source, string? baseDirectory, out byte[] data, out string? mediaType)
    {
        data = Array.Empty<byte>();
        mediaType = null;

        if (string.IsNullOrEmpty(source))
            return false;

        // We deliberately ignore http:// / https:// here — those should be handled by an
        // HttpImageResolver. The Blazor surface can still render absolute URLs directly
        // (see TuiDomGrid.GetImageSrc) without decoded bytes.
        if (source.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
            || source.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
            || source.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        string path;
        try
        {
            if (Path.IsPathRooted(source))
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
