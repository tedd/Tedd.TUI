using System.Net;
using System.Text;
using Tedd.TUI;
using Tedd.TUI.Platform.Blazor;

namespace Tedd.TUI.Platform.Blazor.Tests;

public class XamlSourceTests
{
    private const string SampleXaml = "<TextBlock Text='Hello' />";

    [Fact]
    public async Task FetchAsync_PhysicalFile_ReadsFromDisk()
    {
        var path = Path.Combine(Path.GetTempPath(), $"tui_xamlsource_{Guid.NewGuid():N}.xaml");
        await File.WriteAllTextAsync(path, SampleXaml);
        try
        {
            var result = await XamlSource.FetchAsync(path, new MapServiceProvider());
            Assert.Equal(SampleXaml, result);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task FetchAsync_RelativeUrl_UsesHttpClientBaseAddress()
    {
        var handler = new FakeHandler(req =>
        {
            Assert.Equal("https://app.example/tui/app.xaml", req.RequestUri!.ToString());
            return SampleXaml;
        });
        var services = new MapServiceProvider();
        services.Add(new HttpClient(handler) { BaseAddress = new Uri("https://app.example/") });

        var result = await XamlSource.FetchAsync("tui/app.xaml", services);

        Assert.Equal(SampleXaml, result);
    }

    [Fact]
    public async Task FetchAsync_AbsoluteUrl_IgnoresBaseAddress()
    {
        var handler = new FakeHandler(req =>
        {
            Assert.Equal("https://other.example/x.xaml", req.RequestUri!.ToString());
            return SampleXaml;
        });
        var services = new MapServiceProvider();
        services.Add(new HttpClient(handler) { BaseAddress = new Uri("https://app.example/") });

        var result = await XamlSource.FetchAsync("https://other.example/x.xaml", services);

        Assert.Equal(SampleXaml, result);
    }

    [Fact]
    public async Task FetchAsync_NoFileNoHttpClient_ThrowsWithGuidance()
    {
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => XamlSource.FetchAsync("does/not/exist.xaml", new MapServiceProvider()));

        Assert.Contains("HttpClient", ex.Message);
    }

    [Fact]
    public async Task FetchAsync_FetchedMarkup_LoadsThroughXamlLoader()
    {
        var services = new MapServiceProvider();
        services.Add(new HttpClient(new FakeHandler(_ => "<StackPanel><TextBlock Text='A' /></StackPanel>"))
        {
            BaseAddress = new Uri("https://app.example/")
        });

        var markup = await XamlSource.FetchAsync("app.xaml", services);
        var element = XamlLoader.Load(markup);

        var stack = Assert.IsType<StackPanel>(element);
        Assert.Single(stack.Children);
    }

    /// <summary>Minimal IServiceProvider backed by a type map — avoids pulling in the full DI container.</summary>
    private sealed class MapServiceProvider : IServiceProvider
    {
        private readonly Dictionary<Type, object> _services = new();
        public void Add<T>(T service) where T : class => _services[typeof(T)] = service;
        public object? GetService(Type serviceType) => _services.GetValueOrDefault(serviceType);
    }

    private sealed class FakeHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, string> _respond;
        public FakeHandler(Func<HttpRequestMessage, string> respond) => _respond = respond;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(_respond(request), Encoding.UTF8, "application/xml")
            });
    }
}
