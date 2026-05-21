using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using Tedd.TUI.Markdown;

namespace Tedd.TUI.Imaging;

/// <summary>
/// Resolves <c>http://</c> and <c>https://</c> image sources by downloading the bytes
/// via <see cref="HttpClient"/>. Successful and failed lookups are cached in-memory so
/// repeated render passes don't re-download the same image. Downloads are time-bounded
/// to keep a single bad URL from stalling the UI thread.
/// </summary>
public sealed class HttpImageResolver : IImageResolver
{
    private readonly HttpClient _httpClient;
    private readonly Dictionary<string, CacheEntry> _cache = new(StringComparer.OrdinalIgnoreCase);
    private readonly System.Threading.Lock _gate = new();

    /// <summary>Maximum time to wait for a single download. Defaults to 5 seconds.</summary>
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(5);

    /// <summary>Maximum image size to accept (bytes). Larger responses are treated as a failure. Defaults to 16 MiB.</summary>
    public long MaxBytes { get; set; } = 16 * 1024 * 1024;

    public HttpImageResolver() : this(CreateDefaultClient()) { }

    public HttpImageResolver(HttpClient httpClient)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    }

    private static HttpClient CreateDefaultClient()
    {
        var handler = new SocketsHttpHandler
        {
            AutomaticDecompression = System.Net.DecompressionMethods.All,
            ConnectTimeout = TimeSpan.FromSeconds(5)
        };
        var client = new HttpClient(handler, disposeHandler: true);
        client.DefaultRequestHeaders.UserAgent.ParseAdd("Tedd.TUI/1.0");
        return client;
    }

    public bool TryResolve(string source, string? baseDirectory, out byte[] data, out string? mediaType)
    {
        data = Array.Empty<byte>();
        mediaType = null;

        if (string.IsNullOrEmpty(source)) return false;
        if (!source.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
            && !source.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        lock (_gate)
        {
            if (_cache.TryGetValue(source, out var cached))
            {
                if (!cached.Success) return false;
                data = cached.Data!;
                mediaType = cached.MediaType;
                return true;
            }
        }

        bool success = TryDownload(source, out var fetched, out var ct);

        lock (_gate)
        {
            _cache[source] = new CacheEntry
            {
                Success = success,
                Data = success ? fetched : null,
                MediaType = ct
            };
        }

        if (!success) return false;
        data = fetched!;
        mediaType = ct;
        return true;
    }

    private bool TryDownload(string url, out byte[]? data, out string? mediaType)
    {
        data = null;
        mediaType = null;
        try
        {
            using var cts = new CancellationTokenSource(Timeout);
            using var response = _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cts.Token)
                .GetAwaiter().GetResult();
            if (!response.IsSuccessStatusCode) return false;

            if (response.Content.Headers.ContentLength is long len && len > MaxBytes) return false;

            mediaType = response.Content.Headers.ContentType?.MediaType;

            data = response.Content.ReadAsByteArrayAsync(cts.Token).GetAwaiter().GetResult();
            if (data.LongLength > MaxBytes)
            {
                data = null;
                return false;
            }
            return true;
        }
        catch
        {
            data = null;
            return false;
        }
    }

    private struct CacheEntry
    {
        public bool Success;
        public byte[]? Data;
        public string? MediaType;
    }
}
