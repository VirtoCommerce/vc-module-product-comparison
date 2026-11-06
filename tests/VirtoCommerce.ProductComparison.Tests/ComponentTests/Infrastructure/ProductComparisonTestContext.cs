using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Security.Claims;
using System.Threading.Tasks;
using GraphQL;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using VirtoCommerce.CoreModule.Core.Currency;
using VirtoCommerce.CustomerModule.Core.Model;
using VirtoCommerce.CustomerModule.Core.Services;
using VirtoCommerce.Platform.Caching;
using VirtoCommerce.Platform.Core;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Core.Jobs;
using VirtoCommerce.Platform.Core.Security;
using VirtoCommerce.Platform.Core.Settings;
using VirtoCommerce.ProductComparison.Core.Models;
using VirtoCommerce.ProductComparison.Core.Services;
using VirtoCommerce.ProductComparison.Data.BackgroundJobs;
using VirtoCommerce.ProductComparison.Data.Models;
using VirtoCommerce.ProductComparison.Data.Repositories;
using VirtoCommerce.ProductComparison.Tests.Infrastructure;
using VirtoCommerce.ProductComparison.Xapi;
using VirtoCommerce.StoreModule.Core.Model;
using VirtoCommerce.StoreModule.Core.Services;
using VirtoCommerce.Xapi.Core.Infrastructure;
using CustomerSecurity = VirtoCommerce.CustomerModule.Core.ModuleConstants.Security;

namespace VirtoCommerce.ProductComparison.Tests.ComponentTests.Infrastructure;

/// <summary>
/// Isolated per-test harness: the module as the platform runs it, its own <c>Module.Initialize</c> and
/// <c>Module.PostInitialize</c> included, among the real platform and modules around it (<see cref="PlatformModules"/>:
/// settings, accounts, stores, currencies, contacts, the catalog, X-API, X-Catalog, X-Cart), every database an in-memory
/// SQLite one. Tests seed through those modules' services as an administrator would, act through the module's
/// services, its job, the event bus or a GraphQL document executed in-process as a guest, a signed-in buyer or an
/// administrator, and assert on the results, the rows and the SQL the modules ran.
/// </summary>
/// <remarks>
/// <list type="bullet">
/// <item>Two stores exist, <see cref="StoreId"/> and <see cref="OtherStoreId"/>, in USD and en-US, with every
/// store-level setting at its default until a test saves one (<see cref="SetStoreSettingAsync"/>).</item>
/// <item>No account, contact or catalog exists until a test adds it (<see cref="AddUserAsync"/>,
/// <see cref="AddContactAsync"/>, <see cref="Catalog"/>).</item>
/// <item>Each database lives on one SQLite connection, which isn't thread-safe: a test runs one call at a time, and a
/// GraphQL document selects one root field.</item>
/// </list>
/// </remarks>
internal sealed class ProductComparisonTestContext : IAsyncDisposable
{
    public const string StoreId = "B2B-store";
    public const string OtherStoreId = "Electronics";
    public const string CultureName = "en-US";
    public const string CurrencyCode = "USD";

    private readonly IServiceProvider _provider;
    private readonly bool _ownsProvider;

    private ProductComparisonTestContext(IServiceProvider provider, bool ownsProvider)
    {
        _provider = provider;
        _ownsProvider = ownsProvider;
    }

    /// <param name="configureServices">Registrations added after the platform's and the module's: the storefront
    /// E2E's shell, or a test's observer of the module's outgoing requests.</param>
    /// <param name="buildHost">Builds the host from the configured collection instead of a bare application builder over
    /// it. The storefront E2E passes one that copies the registrations into a Kestrel <c>WebApplication</c> with its
    /// request pipeline so far, so the startup below adds the GraphQL endpoints to it and the same services answer the
    /// real storefront; the caller then owns that host.</param>
    public static async Task<ProductComparisonTestContext> CreateAsync(Action<IServiceCollection> configureServices = null, Func<IServiceCollection, IApplicationBuilder> buildHost = null)
    {
        var configuration = CreateConfiguration();
        var module = TestedModule.Create(configuration);

        var services = new ServiceCollection().AddPlatformModules(configuration);
        module.Initialize(services);
        services.RemoveAll<DbContextOptions<ProductComparisonDbContext>>();
        services.AddModuleDatabase<ProductComparisonDbContext>();
        services.AddSingleton<TestCatalog>();
        configureServices?.Invoke(services);

        var ownsProvider = buildHost == null;
        var app = ownsProvider
            ? new ApplicationBuilder(services.AddSingleton(configuration).BuildServiceProvider())
            : buildHost(services);

        await PlatformModules.PostInitializeAsync(app);
        module.PostInitialize(app);

        var context = new ProductComparisonTestContext(app.ApplicationServices, ownsProvider);
        await context.SeedAsync();

        return context;
    }

    /// <summary>
    /// What the platform's shipped appsettings.json sets that the hosted code reads; every other option keeps its
    /// default: the store X-API's store query falls back to for a domain no store claims, the GraphQL playground (and
    /// with it schema introspection), and the authentication types GraphQL ignores. The platform's country lists, which
    /// it ships in its own content root, are the copies in <c>Platform/localization</c>.
    /// </summary>
    private static IConfiguration CreateConfiguration() => new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string>
        {
            ["VirtoCommerce:Stores:DefaultStore"] = StoreId,
            ["VirtoCommerce:GraphQLPlayground:Enable"] = "true",
            ["VirtoCommerce:GraphQL:ForbiddenAuthenticationTypes:0"] = "Identity.Application",
            ["VirtoCommerce:CountriesFilePath"] = Path.Combine(AppContext.BaseDirectory, "Platform", "localization", "common", "countries.json"),
            ["VirtoCommerce:CountryRegionsFilePath"] = Path.Combine(AppContext.BaseDirectory, "Platform", "localization", "common", "countriesRegions.json"),
        })
        .Build();

    public T GetRequiredService<T>() where T : notnull => _provider.GetRequiredService<T>();

    public ICompareListService CompareListService => GetRequiredService<ICompareListService>();

    public ICompareListSearchService CompareListSearchService => GetRequiredService<ICompareListSearchService>();

    /// <summary>
    /// One occurrence of the recurring cleanup, as the background-job engine runs it: the handler the module registers
    /// for the job's payload, resolved in a scope of its own, given a fresh payload from the type factory. The handler
    /// takes the job's lock and runs the job. It reads nothing of the run's context (the engine's job id, progress,
    /// headers), and the engine's own context isn't public, so none is passed.
    /// </summary>
    public async Task RunCleanupJobAsync()
    {
        using var scope = _provider.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<IBackgroundJobHandler<CleanupCompareListsJobPayload>>();
        await handler.Execute(AbstractTypeFactory<CleanupCompareListsJobPayload>.TryCreateInstance(), context: null);
    }

    /// <summary>The catalog, set up through the catalog and pricing modules: <see cref="TestCatalog.InitializeAsync"/> first.</summary>
    public TestCatalog Catalog => GetRequiredService<TestCatalog>();

    /// <summary>The SQL the modules' databases ran; clear it before the action a test measures.</summary>
    public SqlCommandLog Sql => GetRequiredService<SqlCommandLog>();

    /// <summary>
    /// A customer's account in <see cref="StoreId"/>, created through the platform's UserManager into the security
    /// database, with what the storefront's registration sets: the store, the customer type and the contact
    /// (<paramref name="memberId"/>, if any).
    /// </summary>
    public async Task<ApplicationUser> AddUserAsync(string userId, string memberId = null)
    {
        var user = new ApplicationUser
        {
            Id = userId,
            UserName = $"{userId}@test.com",
            Email = $"{userId}@test.com",
            StoreId = StoreId,
            MemberId = memberId,
            UserType = "Customer",
        };

        await RunScopedAsync<UserManager<ApplicationUser>>(async users => EnsureSucceeded(await users.CreateAsync(user)));

        return user;
    }

    /// <summary>
    /// A buyer as the storefront's registration creates one: a contact, saved through the customer module, and its
    /// account. The storefront's profile queries (the address book) answer only an account that has a contact.
    /// </summary>
    public async Task<ApplicationUser> AddBuyerAsync(string userId)
    {
        var contactId = $"{userId}-contact";
        await AddContactAsync(contactId);

        return await AddUserAsync(userId, contactId);
    }

    /// <summary>Locked as the platform's "Lock" action locks an account.</summary>
    public Task LockOutUserAsync(string userId)
        => RunScopedAsync<UserManager<ApplicationUser>>(async users =>
            EnsureSucceeded(await users.SetLockoutEndDateAsync(await users.FindByIdAsync(userId), DateTime.MaxValue.ToUniversalTime())));

    /// <summary>
    /// Stamps the account's last sign-in, as the platform's token endpoint does on each sign-in: an account change, so
    /// the platform drops its cached copies of the account.
    /// </summary>
    public Task RecordSignInAsync(string userId)
        => RunScopedAsync<UserManager<ApplicationUser>>(async users =>
        {
            var user = await users.FindByIdAsync(userId);
            user.LastLoginDate = DateTime.UtcNow;
            EnsureSucceeded(await users.UpdateAsync(user));
        });

    /// <summary>A contact belonging to the organizations, saved through the customer module with the ones it doesn't have yet.</summary>
    public Task AddContactAsync(string memberId, params string[] organizationIds)
        => AddMemberAsync(new Contact { Id = memberId, Name = memberId, FirstName = memberId, LastName = memberId, Organizations = organizationIds.ToList() }, organizationIds);

    /// <summary>An employee belonging to the organizations, as <see cref="AddContactAsync"/>.</summary>
    public Task AddEmployeeAsync(string memberId, params string[] organizationIds)
        => AddMemberAsync(new Employee { Id = memberId, Name = memberId, FirstName = memberId, LastName = memberId, Organizations = organizationIds.ToList() }, organizationIds);

    private async Task AddMemberAsync(Member member, string[] organizationIds)
    {
        var members = GetRequiredService<IMemberService>();
        var existing = (await members.GetByIdsAsync(organizationIds)).Select(x => x.Id);
        var organizations = organizationIds.Except(existing).Select(id => new Organization { Id = id, Name = id });

        await members.SaveChangesAsync([.. organizations, member]);
    }

    /// <summary>Saves a module-level setting, as an administrator does in the platform's settings.</summary>
    public Task SetSettingAsync(SettingDescriptor descriptor, object value)
        => GetRequiredService<ISettingsManager>().SetValueAsync(descriptor.Name, value);

    /// <summary>Saves a store-level setting, as an administrator does on the store's settings: the store is saved with it.</summary>
    public async Task SetStoreSettingAsync(string storeId, SettingDescriptor descriptor, object value)
    {
        var stores = GetRequiredService<IStoreService>();
        var store = await stores.GetByIdAsync(storeId);
        store.Settings.Single(x => x.Name == descriptor.Name).Value = value;

        await stores.SaveChangesAsync([store]);
    }

    /// <summary>Saves new or changed lists through the real CRUD service (events, cache, audit stamps).</summary>
    public async Task<CompareList> SaveAsync(CompareList list)
    {
        await CompareListService.SaveChangesAsync([list]);

        return list;
    }

    public async Task SaveAsync(IList<CompareList> lists)
    {
        await CompareListService.SaveChangesAsync(lists);
    }

    /// <summary>A list as the module reads it now, or null: past the CRUD cache, from the rows.</summary>
    public async Task<CompareList> ReloadAsync(string listId)
    {
        ExpireCaches();

        return await CompareListService.GetNoCloneAsync(listId);
    }

    /// <summary>
    /// Backdates a list's audit dates, as time passing would: a direct UPDATE (no trigger re-stamps it), then the
    /// CRUD and search caches the raw write bypasses are expired.
    /// </summary>
    public Task SetDatesAsync(string listId, DateTime? createdDate = null, DateTime? modifiedDate = null)
    {
        return SetDatesAsync(x => x.Id == listId, createdDate, modifiedDate);
    }

    /// <summary>As <see cref="SetDatesAsync(string, DateTime?, DateTime?)"/>, for every list the filter matches.</summary>
    public async Task SetDatesAsync(Expression<Func<CompareListEntity, bool>> lists, DateTime? createdDate = null, DateTime? modifiedDate = null)
    {
        await using var dbContext = NewDbContext();

        await dbContext.Set<CompareListEntity>()
            .Where(lists)
            .ExecuteUpdateAsync(setters =>
            {
                if (createdDate != null)
                {
                    setters.SetProperty(x => x.CreatedDate, createdDate.Value);
                }

                if (modifiedDate != null)
                {
                    setters.SetProperty(x => x.ModifiedDate, modifiedDate.Value);
                }
            });

        ExpireCaches();
    }

    /// <summary>Fresh DbContext on the module's database, for seeding and for asserting on rows.</summary>
    public ProductComparisonDbContext NewDbContext() => new(GetRequiredService<DbContextOptions<ProductComparisonDbContext>>());

    /// <summary>Executes a GraphQL document as an anonymous caller, a guest of the storefront.</summary>
    public Task<GraphQlResponse> ExecuteAsGuestAsync(string query)
    {
        return ExecuteAsync(query, new ClaimsPrincipal(new ClaimsIdentity()));
    }

    /// <summary>
    /// Executes a GraphQL document as a signed-in buyer. <paramref name="organizationId"/> is the organization the
    /// token carries, the one the buyer picked at sign-in.
    /// </summary>
    public Task<GraphQlResponse> ExecuteAsBuyerAsync(string userId, string query, string organizationId = null)
    {
        var identity = new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, userId)], authenticationType: "Test");

        if (organizationId != null)
        {
            identity.AddClaim(new Claim(CustomerSecurity.Claims.OrganizationId, organizationId));
        }

        return ExecuteAsync(query, new ClaimsPrincipal(identity));
    }

    /// <summary>Executes a GraphQL document as a platform administrator (the Administrator role claim).</summary>
    public Task<GraphQlResponse> ExecuteAsAdministratorAsync(string query)
    {
        var identity = new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, "administrator")], authenticationType: "Test");
        identity.AddClaim(new Claim(identity.RoleClaimType, PlatformConstants.Security.SystemRoles.Administrator));

        return ExecuteAsync(query, new ClaimsPrincipal(identity));
    }

    // The module's own schema (/graphql/product-comparison), built from its schema builders only, executed as the
    // GraphQL middleware executes a request: in a DI scope of its own, within the HTTP request it came with (a JSON
    // POST by the caller), which X-API's content type rule, the request cache and the audit stamps read.
    private async Task<GraphQlResponse> ExecuteAsync(string query, ClaimsPrincipal principal)
    {
        await using var scope = _provider.CreateAsyncScope();

        var httpContextAccessor = _provider.GetRequiredService<IHttpContextAccessor>();
        var httpContext = new DefaultHttpContext { RequestServices = scope.ServiceProvider, User = principal };
        httpContext.Request.Method = HttpMethods.Post;
        httpContext.Request.ContentType = "application/json";
        httpContextAccessor.HttpContext = httpContext;

        try
        {
            var executer = scope.ServiceProvider.GetRequiredService<IDocumentExecuter>();
            var serializer = scope.ServiceProvider.GetRequiredService<IGraphQLTextSerializer>();
            var schema = _provider.GetRequiredService<ScopedSchemaFactory<XapiAssemblyMarker>>();

            var result = await executer.ExecuteAsync(options =>
            {
                options.Schema = schema;
                options.Query = query;
                options.RequestServices = scope.ServiceProvider;
                options.UserContext = new GraphQLUserContext(principal);
            });

            return new GraphQlResponse(serializer.Serialize(result));
        }
        finally
        {
            httpContextAccessor.HttpContext = null;
        }
    }

    /// <summary>The currency and the two stores every test starts with, saved through the core and store modules.</summary>
    private async Task SeedAsync()
    {
        await GetRequiredService<ICurrencyService>().SaveChangesAsync(
        [
            new Currency { Code = CurrencyCode, Name = "US Dollar", ExchangeRate = 1m, IsPrimary = true },
        ]);

        await GetRequiredService<IStoreService>().SaveChangesAsync([NewStore(StoreId), NewStore(OtherStoreId)]);
    }

    private static Store NewStore(string storeId) => new()
    {
        Id = storeId,
        Name = storeId,
        Catalog = TestCatalog.CatalogId,
        DefaultLanguage = CultureName,
        Languages = [CultureName],
        DefaultCurrency = CurrencyCode,
        Currencies = [CurrencyCode],
    };

    // As a request runs it: the scoped services (UserManager over its DbContext) live for the call.
    private async Task RunScopedAsync<TService>(Func<TService, Task> work) where TService : notnull
    {
        await using var scope = _provider.CreateAsyncScope();
        await work(scope.ServiceProvider.GetRequiredService<TService>());
    }

    private static void EnsureSucceeded(IdentityResult result)
    {
        if (!result.Succeeded)
        {
            throw new InvalidOperationException(string.Join("; ", result.Errors.Select(x => x.Description)));
        }
    }

    private static void ExpireCaches()
    {
        GenericCachingRegion<CompareList>.ExpireRegion();
        GenericSearchCachingRegion<CompareList>.ExpireRegion();
    }

    public async ValueTask DisposeAsync()
    {
        if (_ownsProvider && _provider is IAsyncDisposable provider)
        {
            await provider.DisposeAsync();
        }
    }
}
