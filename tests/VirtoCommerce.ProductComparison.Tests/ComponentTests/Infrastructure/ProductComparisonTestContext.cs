using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Security.Claims;
using System.Threading.Tasks;
using GraphQL;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OpenIddict.Abstractions;
using VirtoCommerce.CatalogModule.Core.Events;
using VirtoCommerce.CatalogModule.Core.Model;
using VirtoCommerce.Platform.Caching;
using VirtoCommerce.Platform.Core;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Core.Events;
using VirtoCommerce.Platform.Core.Security;
using VirtoCommerce.Platform.Core.Settings;
using VirtoCommerce.ProductComparison.Core.Models;
using VirtoCommerce.ProductComparison.Core.Services;
using VirtoCommerce.ProductComparison.Data.BackgroundJobs;
using VirtoCommerce.ProductComparison.Data.Handlers;
using VirtoCommerce.ProductComparison.Data.Models;
using VirtoCommerce.ProductComparison.Data.Repositories;
using VirtoCommerce.ProductComparison.Tests.Infrastructure;
using VirtoCommerce.ProductComparison.Xapi;
using VirtoCommerce.Xapi.Core.Infrastructure;
using CustomerSecurity = VirtoCommerce.CustomerModule.Core.ModuleConstants.Security;

namespace VirtoCommerce.ProductComparison.Tests.ComponentTests.Infrastructure;

/// <summary>
/// Isolated per-test harness: the module's real services, repository, job, event handler and GraphQL schema, over
/// an in-memory SQLite database created from the EF model with <c>EnsureCreated</c>, registered by the module's own
/// <c>Module.Initialize</c> (<see cref="TestServicesConfiguration"/>). Tests act through the services, the job, the
/// event bus or a GraphQL document executed in-process as a guest, a signed-in buyer or an administrator, and
/// assert on the results and on the rows.
/// </summary>
/// <remarks>
/// <list type="bullet">
/// <item>Two stores exist, <see cref="StoreId"/> and <see cref="OtherStoreId"/>, with the module's default
/// settings until a test changes one (<see cref="SetStoreSetting"/>).</item>
/// <item>Accounts, contacts and module settings are doubles that start empty: a registered buyer exists only once
/// a test adds it (<see cref="Users"/>).</item>
/// <item>The database lives on one SQLite connection, which isn't thread-safe: a test runs one call at a time, and
/// a GraphQL document selects one root field.</item>
/// </list>
/// </remarks>
internal sealed class ProductComparisonTestContext : IDisposable
{
    public const string StoreId = "B2B-store";
    public const string OtherStoreId = "Electronics";

    private readonly SqliteConnection _connection;
    private readonly IServiceProvider _provider;
    private readonly bool _ownsProvider;
    private readonly DbContextOptions<ProductComparisonDbContext> _dbContextOptions;

    private ProductComparisonTestContext(SqliteConnection connection, IServiceProvider provider, bool ownsProvider, DbContextOptions<ProductComparisonDbContext> dbContextOptions)
    {
        _connection = connection;
        _provider = provider;
        _ownsProvider = ownsProvider;
        _dbContextOptions = dbContextOptions;
    }

    /// <param name="configureOverrides">Last-wins registrations applied after the standard slices, for a test that
    /// needs a service of its own in place of the module's.</param>
    /// <param name="providerFactory">Builds the provider from the configured collection instead of a plain
    /// <c>BuildServiceProvider()</c>. The storefront E2E host passes one that copies the registrations into a Kestrel
    /// <c>WebApplication</c>, so the same services answer the real storefront; the caller then owns that provider.</param>
    public static ProductComparisonTestContext Create(Action<IServiceCollection> configureOverrides = null, Func<IServiceCollection, IServiceProvider> providerFactory = null)
    {
        // The platform reads the current user id from these claim types, configured at startup. "sub" first, as the
        // platform's tokens carry it (the storefront E2E signs buyers in with such a token); the component tests'
        // principals carry NameIdentifier.
        ClaimsPrincipalExtensions.UserIdClaimTypes = [OpenIddictConstants.Claims.Subject, ClaimTypes.NameIdentifier];

        var connection = SqliteTestDbContextFactory.CreateConnection();
        var dbContextOptions = SqliteTestDbContextFactory.CreateOptions<ProductComparisonDbContext>(connection);

        var services = new ServiceCollection()
            .AddPlatformSlice()
            .AddProductComparisonModule(dbContextOptions)
            .AddXapiSlice();

        configureOverrides?.Invoke(services);

        var ownsProvider = providerFactory == null;
        var provider = ownsProvider ? services.BuildServiceProvider() : providerFactory(services);

        // Module.PostInitialize: appBuilder.RegisterEventHandler<ProductChangedEvent, ProductChangedEventHandler>().
        provider.GetRequiredService<IEventHandlerRegistrar>()
            .RegisterEventHandler<ProductChangedEvent>(provider.GetRequiredService<ProductChangedEventHandler>());

        var context = new ProductComparisonTestContext(connection, provider, ownsProvider, dbContextOptions);
        context.Stores.Add(StoreId);
        context.Stores.Add(OtherStoreId);

        return context;
    }

    public T GetRequiredService<T>() where T : notnull => _provider.GetRequiredService<T>();

    public ICompareListService CompareListService => GetRequiredService<ICompareListService>();

    public ICompareListSearchService CompareListSearchService => GetRequiredService<ICompareListSearchService>();

    /// <summary>A fresh job, as the recurring job's handler resolves it on each run.</summary>
    public CleanupCompareListsJob CleanupJob => GetRequiredService<CleanupCompareListsJob>();

    public TestUsers Users => GetRequiredService<TestUsers>();

    public TestMemberService Members => GetRequiredService<TestMemberService>();

    public TestStoreService Stores => GetRequiredService<TestStoreService>();

    /// <summary>Overrides a module-level setting; left unset, a setting reports its descriptor default.</summary>
    public void SetSetting(SettingDescriptor descriptor, object value)
    {
        GetRequiredService<TestSettingsManager>().Values[descriptor.Name] = value;
    }

    /// <summary>Saves a store-level setting for a store, as an administrator does on the store's settings.</summary>
    public void SetStoreSetting(string storeId, SettingDescriptor descriptor, object value)
    {
        Stores.SetSetting(storeId, descriptor, value);
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

    /// <summary>Raises the catalog's product event, as the catalog's ItemService does on a save or a delete.</summary>
    public Task PublishProductChangedAsync(EntryState state, params string[] productIds)
    {
        var entries = productIds
            .Select(id => new GenericChangedEntry<CatalogProduct>(new CatalogProduct { Id = id }, state))
            .ToList();

        return GetRequiredService<IEventPublisher>().Publish(new ProductChangedEvent(entries));
    }

    /// <summary>Fresh DbContext on the module's database, for seeding and for asserting on rows.</summary>
    public ProductComparisonDbContext NewDbContext() => new(_dbContextOptions);

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

    // The module's own schema, built from its schema builders only, executed in a DI scope of its own as the
    // GraphQL middleware executes a request.
    private async Task<GraphQlResponse> ExecuteAsync(string query, ClaimsPrincipal principal)
    {
        using var scope = _provider.CreateScope();

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

    private static void ExpireCaches()
    {
        GenericCachingRegion<CompareList>.ExpireRegion();
        GenericSearchCachingRegion<CompareList>.ExpireRegion();
    }

    public void Dispose()
    {
        if (_ownsProvider)
        {
            (_provider as IDisposable)?.Dispose();
        }

        _connection.Dispose();
    }
}
