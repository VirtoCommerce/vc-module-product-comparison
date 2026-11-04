using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Playwright;
using VirtoCommerce.ProductComparison.Data.Models;
using VirtoCommerce.ProductComparison.Tests.ComponentTests.Infrastructure;
using Xunit;

namespace VirtoCommerce.ProductComparison.Tests.StorefrontE2E.Infrastructure;

/// <summary>
/// One virtual environment per test group (xunit collection): the in-process backend with its store settings and
/// catalog, the storefront (the run's Vite dev server for a clone, proxying to the backend, or a build the backend
/// serves itself), and one browser. Tests in the group share all of it; each test gets its own browser context and
/// its own guest or buyer, so what one test saves is nobody else's. Groups run one after another (their collections
/// disable parallelization), so only one environment is alive at a time.
/// </summary>
public abstract class StorefrontEnvironment : IAsyncLifetime
{
    private IPlaywright _playwright;
    private IBrowser _browser;
    private InProcessBackend _backend;
    private int _buyers;

    /// <summary>Where the browser opens the storefront: the run's Vite dev server, or the backend's own origin for a build.</summary>
    internal string StorefrontUrl { get; private set; }

    /// <summary>Timestamped phases of this environment's life, so a hang or a slow step is attributable.</summary>
    public string PhaseLog { get; } = Path.Combine(DiagnosticsDirectory, $"environment-{DateTime.Now:HHmmss}.log");

    public static string DiagnosticsDirectory => Path.Combine(AppContext.BaseDirectory, "StorefrontE2E-diagnostics");

    public async ValueTask InitializeAsync()
    {
        // Before anything starts: the storefront, and the plugin build every compare control comes from. Either one
        // missing fails the group with what to do.
        var storefront = StorefrontLocation.Current;
        StorefrontPlugin.EnsureBuilt();

        // A build is served by the backend itself, from any free port. A clone runs on the run's Vite dev server, whose
        // proxy target was fixed when it started: the group's backend takes the port it points at.
        var port = storefront.BuildDirectory == null ? SharedViteDevServer.BackendPort : 0;

        Log("backend starting");
        _backend = await InProcessBackend.StartAsync(storefront.BuildDirectory, port);
        Log($"backend ready at {_backend.Url}");

        ConfigureStore(_backend.Context);
        E2ECatalog.Seed(_backend.Catalog);
        Log("seeded");

        if (storefront.BuildDirectory != null)
        {
            StorefrontUrl = _backend.Url;
        }
        else
        {
            var vite = await SharedViteDevServer.GetAsync(storefront.SourceDirectory, DiagnosticsDirectory);
            StorefrontUrl = vite.Url;
        }

        Log($"storefront ready at {StorefrontUrl}");

        _playwright = await Playwright.CreateAsync();
        _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = Environment.GetEnvironmentVariable("E2E_HEADED") != "1",
        });
        Log("browser launched");
    }

    private void Log(string phase)
    {
        Directory.CreateDirectory(DiagnosticsDirectory);
        File.AppendAllText(PhaseLog, $"{DateTime.Now:HH:mm:ss.fff} [{GetType().Name}] {phase}{Environment.NewLine}");
    }

    /// <summary>The group's store settings, applied before the first page opens; the module's defaults otherwise.</summary>
    private protected virtual void ConfigureStore(ProductComparisonTestContext context)
    {
    }

    /// <summary>
    /// Test-side work against the harness, between requests: the page may still be fetching, and the in-memory database
    /// takes one caller at a time.
    /// </summary>
    internal Task<T> RunExclusiveAsync<T>(Func<ProductComparisonTestContext, Task<T>> work) => _backend.RunExclusiveAsync(work);

    /// <summary>
    /// A guest id of the test's own, as the storefront keeps one for a guest (the guest cart's): a GUID no account has.
    /// </summary>
    internal static string NewGuestId() => Guid.NewGuid().ToString();

    /// <summary>A registered buyer of the test's own (an account without a contact or an organization).</summary>
    internal string NewBuyer()
    {
        var n = (++_buyers).ToString(CultureInfo.InvariantCulture);
        var userId = $"e2e-buyer-{GetType().Name}-{n}".ToLowerInvariant();
        _backend.Context.Users.Add(userId);

        return userId;
    }

    /// <summary>A token for the buyer, as /connect/token issues one.</summary>
    internal TokenResponse IssueToken(string userId) => _backend.Tokens.Issue(userId);

    /// <summary>
    /// Saves products to a guest's compare list through the real mutation the storefront sends, as the guest, between
    /// requests: for a test that starts from a list. Each product is "productId@categoryId".
    /// </summary>
    internal Task<string> SaveAsGuestAsync(string guestId, params string[] products)
        => RunExclusiveAsync(async context =>
        {
            var list = (await context.ExecuteAsGuestAsync(CompareListDocuments.SaveProducts(guestId, products))).Data("saveProductsToCompareList");
            return list.GetProperty("id").GetString();
        });

    /// <summary>The user's compare list rows in the store, with their items, read between requests; empty when there is none.</summary>
    internal Task<IList<CompareListEntity>> GetListsAsync(string userId)
        => RunExclusiveAsync<IList<CompareListEntity>>(async context =>
        {
            await using var db = context.NewDbContext();
            return await db.Set<CompareListEntity>()
                .AsNoTracking()
                .Include(x => x.Items)
                .Where(x => x.UserId == userId && x.StoreId == InProcessBackend.StoreId)
                .ToListAsync();
        });

    /// <summary>
    /// A fresh browser context (fresh localStorage, fresh Apollo cache) as a guest the storefront knows by
    /// <paramref name="guestId"/>, landed on the path. A test signs the guest in later, in the same context, when it
    /// needs a buyer.
    /// </summary>
    internal async Task<StorefrontSession> OpenAsGuestAsync(string guestId, string path)
    {
        Log($"opening {path}");

        var context = await _browser.NewContextAsync(new BrowserNewContextOptions
        {
            BaseURL = StorefrontUrl,
            IgnoreHTTPSErrors = true,
            ViewportSize = new ViewportSize { Width = 1440, Height = 900 },
            Locale = StorefrontServices.DefaultCulture,
        });

        // The id the storefront keeps in localStorage user-id and sends with the page context; set only while it has
        // none, as the storefront keeps it, so a sign-in in the same context replaces it.
        await context.AddInitScriptAsync($"if (!localStorage.getItem('user-id')) localStorage.setItem('user-id', {JsonSerializer.Serialize(guestId)});");

        // A guest whose list was seeded is one whose browser saved it: the plugin remembers the list's id, and loads a
        // guest's list only then. Set only while there is none, as the plugin forgets it after a sign-in's merge.
        var guestList = (await GetListsAsync(guestId)).FirstOrDefault();
        if (guestList != null)
        {
            await context.AddInitScriptAsync(
                $"if (!localStorage.getItem({JsonSerializer.Serialize(CompareControls.GuestListIdKey)})) " +
                $"localStorage.setItem({JsonSerializer.Serialize(CompareControls.GuestListIdKey)}, {JsonSerializer.Serialize(guestList.Id)});");
        }

        var session = new StorefrontSession(context, await context.NewPageAsync());
        await session.GotoAsync(path);
        Log($"loaded {session.Page.Url}");

        return session;
    }

    public async ValueTask DisposeAsync()
    {
        if (_backend == null)
        {
            return;
        }

        Log("disposing");

        if (_browser != null)
        {
            await _browser.CloseAsync();
        }

        _playwright?.Dispose();
        Log("browser closed");

        // A clone's dev server stays up for the next group: SharedViteDevServer stops it when the run ends.
        await _backend.DisposeAsync();
        Log("backend stopped");
    }
}
