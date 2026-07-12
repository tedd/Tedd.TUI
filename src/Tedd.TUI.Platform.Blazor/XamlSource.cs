using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace Tedd.TUI.Platform.Blazor;

/// <summary>
/// Resolves XAML markup for <c>TuiXamlView</c> from a source reference. A source can be a
/// physical file path (Blazor Server, MAUI Blazor hybrid), an absolute URL, or a path
/// relative to the application base (Blazor WebAssembly static assets under wwwroot).
/// </summary>
public static class XamlSource
{
    /// <summary>
    /// Loads the markup text behind <paramref name="source"/>. Resolution order:
    /// physical file (as-is, then relative to <see cref="AppContext.BaseDirectory"/>),
    /// then HTTP via the app's registered <see cref="HttpClient"/> (relative sources are
    /// combined with its <c>BaseAddress</c> or the <see cref="NavigationManager"/> base URI).
    /// </summary>
    public static async Task<string> FetchAsync(string source, IServiceProvider services)
    {
        if (string.IsNullOrWhiteSpace(source))
            throw new ArgumentException("Source must be a file path or URL.", nameof(source));

        // Physical files first: hosting models with a real file system serve XAML straight
        // from disk without requiring an HttpClient registration.
        if (File.Exists(source))
            return await File.ReadAllTextAsync(source);

        var baseDirCandidate = Path.Combine(AppContext.BaseDirectory, source);
        if (File.Exists(baseDirCandidate))
            return await File.ReadAllTextAsync(baseDirCandidate);

        var http = services.GetService<HttpClient>();
        var nav = services.GetService<NavigationManager>();

        bool ownsClient = false;
        if (http == null)
        {
            if (nav == null)
                throw new InvalidOperationException(
                    $"Cannot load XAML source '{source}': it is not a file on disk and no HttpClient is registered. " +
                    "Register an HttpClient (services.AddScoped(sp => new HttpClient { BaseAddress = ... })) or pass inline markup via the Xaml parameter.");
            http = new HttpClient { BaseAddress = new Uri(nav.BaseUri) };
            ownsClient = true;
        }

        try
        {
            Uri uri;
            if (Uri.TryCreate(source, UriKind.Absolute, out var absolute))
                uri = absolute;
            else if (http.BaseAddress != null)
                uri = new Uri(http.BaseAddress, source);
            else if (nav != null)
                uri = new Uri(new Uri(nav.BaseUri), source);
            else
                throw new InvalidOperationException(
                    $"Cannot resolve relative XAML source '{source}': the registered HttpClient has no BaseAddress and no NavigationManager is available.");

            return await http.GetStringAsync(uri);
        }
        finally
        {
            if (ownsClient)
                http.Dispose();
        }
    }
}
