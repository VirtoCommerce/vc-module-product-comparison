using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using GraphQL.Types;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using VirtoCommerce.ProductComparison.Tests.ComponentTests.Infrastructure;
using VirtoCommerce.ProductComparison.Tests.Infrastructure;

namespace VirtoCommerce.ProductComparison.Tests.StorefrontE2E.Infrastructure;

/// <summary>
/// The component harness hosted as a real HTTP backend inside the test process: the same
/// <see cref="ProductComparisonTestContext"/> (the module and the platform around it, as they start up, over in-memory
/// SQLite) behind Kestrel on a loopback port, so the real storefront talks to it while the tests seed and assert
/// through the very same services and databases. No second dotnet process, nothing read from <c>back/modules</c>.
/// </summary>
internal sealed class InProcessBackend : IAsyncDisposable
{
    public const string StoreId = ProductComparisonTestContext.StoreId;

    private readonly WebApplication _app;
    private readonly RequestGate _gate;

    private InProcessBackend(WebApplication app, RequestGate gate, ProductComparisonTestContext context, string url)
    {
        _app = app;
        _gate = gate;
        Context = context;
        Url = url;
    }

    /// <summary>The harness: seed and assert through it exactly as the component tests do.</summary>
    public ProductComparisonTestContext Context { get; }

    /// <summary>
    /// Loopback base URL of the Kestrel host (<c>http://127.0.0.1:port</c>): what a clone's Vite dev server proxies
    /// to, and where a built storefront is served.
    /// </summary>
    public string Url { get; }

    public TestTokenService Tokens => Context.GetRequiredService<TestTokenService>();

    public TestCatalog Catalog => Context.Catalog;

    /// <summary>
    /// Runs test-side work against the harness between requests. Its in-memory SQLite database lives on one
    /// connection, which is not thread-safe, so the host answers one request at a time and a test's reads and seeds take
    /// the same turn: the page may still be refetching after it shows a change.
    /// </summary>
    public Task<T> RunExclusiveAsync<T>(Func<ProductComparisonTestContext, Task<T>> work) => _gate.RunAsync(() => work(Context));

    /// <param name="storefrontBuild">A built storefront to serve from the backend's own origin; null when there is none to serve.</param>
    /// <param name="port">The loopback port to listen on; 0 takes any free one.</param>
    public static async Task<InProcessBackend> StartAsync(string storefrontBuild = null, int port = 0)
    {
        WebApplication app = null;
        var gate = new RequestGate();

        var context = await ProductComparisonTestContext.CreateAsync(
            configureServices: StorefrontServices.AddStorefrontShell,
            buildHost: services =>
            {
                var builder = WebApplication.CreateBuilder(new WebApplicationOptions { ApplicationName = "VirtoCommerce.ProductComparison.StorefrontE2E" });
                builder.Logging.SetMinimumLevel(LogLevel.Warning);
                builder.WebHost.UseUrls($"http://127.0.0.1:{port}");

                foreach (var descriptor in services)
                {
                    builder.Services.Add(descriptor);
                }

                StorefrontServices.AddWebServices(builder.Services);

                app = builder.Build();
                StorefrontServices.ConfigurePipeline(app, gate, storefrontBuild);

                // Not started yet: the platform's and the modules' startup still adds their endpoints to it.
                return app;
            });

        await StorefrontShellModules.PostInitializeAsync(app);
        await context.Catalog.InitializeAsync();

        // Eagerly, so a graph type whose dependency is missing fails here with the DI message, not as an opaque
        // "Error executing document" on the storefront's first request.
        app.Services.GetRequiredService<ISchema>().Initialize();

        await app.StartAsync();

        var url = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.First().TrimEnd('/');
        var backend = new InProcessBackend(app, gate, context, url);

        await backend.SelfCheckAsync();

        return backend;
    }

    /// <summary>
    /// The storefront's first request, made here first: a failure in the shell wiring then reads as a GraphQL error
    /// in the test output instead of a blank storefront page.
    /// </summary>
    private async Task SelfCheckAsync()
    {
        using var client = new HttpClient { BaseAddress = new Uri(Url) };
        var response = await GraphQlHttp.PostAsync(client, new
        {
            query = "{ store(domain: \"127.0.0.1\") { storeId settings { modules { moduleId settings { name value } } } plugins(appId: \"vc-frontend\") { id } } }",
        });
        var json = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode || json.Contains("\"errors\"") || !json.Contains($"\"storeId\":\"{StoreId}\"") || !json.Contains("\"id\":\"product-comparison\""))
        {
            throw new InvalidOperationException($"The in-process backend does not answer the storefront's store query with the compare plugin ({(int)response.StatusCode}): {json}");
        }
    }

    public async ValueTask DisposeAsync()
    {
        await _app.StopAsync();
        await _app.DisposeAsync();

        // The provider was the app's: its databases went with it.
        await Context.DisposeAsync();
        _gate.Dispose();
    }
}
