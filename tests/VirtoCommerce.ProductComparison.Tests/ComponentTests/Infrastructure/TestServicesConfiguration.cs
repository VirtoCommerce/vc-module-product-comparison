using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GraphQL;
using GraphQL.Introspection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using VirtoCommerce.CatalogModule.Core.Model;
using VirtoCommerce.CatalogModule.Core.Services;
using VirtoCommerce.CoreModule.Core.Currency;
using VirtoCommerce.CustomerModule.Core.Services;
using VirtoCommerce.Platform.Caching;
using VirtoCommerce.Platform.Core.Bus;
using VirtoCommerce.Platform.Core.Caching;
using VirtoCommerce.Platform.Core.Events;
using VirtoCommerce.Platform.Core.GenericCrud;
using VirtoCommerce.Platform.Core.Modularity;
using VirtoCommerce.Platform.Core.Security;
using VirtoCommerce.Platform.Core.Security.Search;
using VirtoCommerce.Platform.Core.Settings;
using VirtoCommerce.ProductComparison.Data.Repositories;
using VirtoCommerce.ProductComparison.Web;
using VirtoCommerce.StoreModule.Core.Services;
using VirtoCommerce.Xapi.Core.Schemas;
using VirtoCommerce.Xapi.Core.Services;
using VirtoCommerce.Xapi.Data.Services;
using VirtoCommerce.XCart.Core.Schemas;
using VirtoCommerce.XCatalog.Core.Schemas;

namespace VirtoCommerce.ProductComparison.Tests.ComponentTests.Infrastructure;

/// <summary>
/// The component harness's service collection, in three slices: the platform pieces the module runs on, the module
/// itself, and the X-API around its schema. The module registers through its own <see cref="Module.Initialize"/>,
/// so the tests run the registrations that ship; only its database moves to in-memory SQLite. What the module only
/// reads from other modules (stores and their settings, module settings, accounts, contacts, currencies) is a
/// hand-written double.
/// </summary>
internal static class TestServicesConfiguration
{
    /// <summary>
    /// Logging, the platform memory cache (the CRUD and search caches, on as in production), the in-process event
    /// bus (the CRUD events, and the catalog's product event the module handles), the CRUD options, and the doubles
    /// for settings, accounts, contacts, stores and currencies.
    /// </summary>
    public static IServiceCollection AddPlatformSlice(this IServiceCollection services)
    {
        services.AddLogging();
        services.AddMemoryCache();
        services.Configure<CachingOptions>(_ => { });
        services.AddSingleton<IPlatformMemoryCache, PlatformMemoryCache>();
        services.Configure<CrudOptions>(_ => { });

        services.AddSingleton<InProcessBus>();
        services.AddSingleton<IEventPublisher>(sp => sp.GetRequiredService<InProcessBus>());
        services.AddSingleton<IEventHandlerRegistrar>(sp => sp.GetRequiredService<InProcessBus>());

        services.AddSingleton<TestSettingsManager>();
        services.AddSingleton<ISettingsManager>(sp => sp.GetRequiredService<TestSettingsManager>());

        // A real UserManager over the in-memory accounts: the guest rule and the account-state check call it as
        // they call the platform's, one manager per call, disposed by the caller.
        services.AddSingleton<TestUsers>();
        services.AddSingleton<IUserSearchService>(sp => sp.GetRequiredService<TestUsers>());
        services.AddSingleton<TestUserStore>();
        services.AddSingleton<Func<UserManager<ApplicationUser>>>(sp => () =>
            new UserManager<ApplicationUser>(sp.GetRequiredService<TestUserStore>(), null, null, null, null, null, null, null, null));

        services.AddSingleton<TestMemberService>();
        services.AddSingleton<IMemberService>(sp => sp.GetRequiredService<TestMemberService>());
        services.AddSingleton<TestStoreService>();
        services.AddSingleton<IStoreService>(sp => sp.GetRequiredService<TestStoreService>());
        services.AddSingleton<ICurrencyService, TestCurrencyService>();

        return services;
    }

    /// <summary>
    /// The module under test, registered by its own <see cref="Module.Initialize"/>: the repository, the CRUD and
    /// search services, the cleanup job, the product event handler, the aggregate repository, the authorization
    /// handler, the validators, and the schema (graph types, MediatR handlers, schema builders, the scoped schema).
    /// Its DbContext options, which the module builds from the configured provider, give way to SQLite.
    /// </summary>
    public static IServiceCollection AddProductComparisonModule(this IServiceCollection services, DbContextOptions<ProductComparisonDbContext> dbContextOptions)
    {
        new Module().Initialize(services);

        services.RemoveAll<DbContextOptions<ProductComparisonDbContext>>();
        services.AddSingleton(dbContextOptions);

        return services;
    }

    /// <summary>
    /// What the X-API module and the modules whose types the compare list returns (X-Catalog's product and category,
    /// X-Cart's configuration section) set up in production, minus their schema builders: only this module's fields
    /// are in the schema, as on <c>/graphql/product-comparison</c>.
    /// </summary>
    public static IServiceCollection AddXapiSlice(this IServiceCollection services)
    {
        // What the X-API module sets at startup (its PostInitialize).
#pragma warning disable CS0618 // Type or member is obsolete: mirrors the X-API module, which sets the same switches.
        GlobalSwitches.UseLegacyTypeNaming = true;
        GlobalSwitches.InferFieldNullabilityFromNRTAnnotations = false;
#pragma warning restore CS0618

        // The builders authorize through IAuthorizationService; the module's requirement handler is registered by
        // Module.Initialize.
        services.AddAuthorization();

        // The real account-state check every compare list endpoint runs. No ambient request scope here, so the
        // check runs uncached, as it does outside a request.
        services.AddSingleton<IRequestScopedCacheAccessor, NoRequestScopedCacheAccessor>();
        services.AddTransient<IUserManagerCore>(sp => new UserManagerCore(
            sp.GetRequiredService<Func<UserManager<ApplicationUser>>>(),
            sp.GetRequiredService<IRequestScopedCacheAccessor>()));

        // ScopedSchemaFactory depends on ISchemaFilter (registered by the X-API module in production).
        services.AddSingleton<ISchemaFilter, DefaultSchemaFilter>();

        // Constructor dependencies of the X-Catalog types under a compared product. The tests don't select the
        // product, so these only have to exist; the geo service is optional, and absent, as on a default install.
        services.AddSingleton<IMeasureService, EmptyMeasureService>();
        services.AddSingleton<IPropertyGroupService, EmptyPropertyGroupService>();
        services.AddSingleton(typeof(IOptionalDependency<>), typeof(AbsentOptionalDependency<>));

        services.AddGraphQL(builder =>
        {
            builder.AddGraphTypes(typeof(MoneyType).Assembly);                // X-API core types
            builder.AddGraphTypes(typeof(ProductType).Assembly);              // X-Catalog: CompareListItem.product, CompareListCategory.category
            builder.AddGraphTypes(typeof(ConfigurationSectionType).Assembly); // X-Cart: configurationSection, ConfigurationSectionInput
            builder.AddSystemTextJson();                                      // IGraphQLTextSerializer, for the responses the tests read
            builder.AddDataLoader();                                          // the product, category and section loaders
            builder.AddErrorInfoProvider(options => options.ExposeExceptionDetails = true); // an unexpected failure shows its exception in extensions.details
        });

        return services;
    }

    private sealed class NoRequestScopedCacheAccessor : IRequestScopedCacheAccessor
    {
        public IRequestScopedCache Cache => null;
    }

    private sealed class AbsentOptionalDependency<T> : IOptionalDependency<T>
    {
        public bool HasValue => false;

        public T Value => default;
    }

    private sealed class EmptyMeasureService : IMeasureService
    {
        public Task<IList<Measure>> GetAsync(IList<string> ids, string responseGroup = null, bool clone = true) => Task.FromResult<IList<Measure>>([]);

        public Task SaveChangesAsync(IList<Measure> models) => throw new NotSupportedException();

        public Task DeleteAsync(IList<string> ids, bool softDelete = false) => throw new NotSupportedException();
    }

    private sealed class EmptyPropertyGroupService : IPropertyGroupService
    {
        public Task<IList<PropertyGroup>> GetAsync(IList<string> ids, string responseGroup = null, bool clone = true) => Task.FromResult<IList<PropertyGroup>>([]);

        public Task SaveChangesAsync(IList<PropertyGroup> models) => throw new NotSupportedException();

        public Task DeleteAsync(IList<string> ids, bool softDelete = false) => throw new NotSupportedException();
    }
}
