using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using OpenIddict.Abstractions;
using VirtoCommerce.LuceneSearchModule.Data;
using VirtoCommerce.Platform.Core;
using VirtoCommerce.Platform.Core.DistributedLock;
using VirtoCommerce.Platform.Core.Modularity;
using VirtoCommerce.Platform.Core.Security;
using VirtoCommerce.Platform.Core.Security.Search;
using VirtoCommerce.Platform.Core.Settings;
using VirtoCommerce.Platform.Data.Extensions;
using VirtoCommerce.Platform.Data.Repositories;
using VirtoCommerce.Platform.DistributedLock;
using VirtoCommerce.Platform.DistributedLock.InProcess;
using VirtoCommerce.Platform.Modules;
using VirtoCommerce.Platform.Security;
using VirtoCommerce.Platform.Security.Authorization;
using VirtoCommerce.Platform.Security.Repositories;
using VirtoCommerce.Platform.Security.Services;
using VirtoCommerce.SearchModule.Core.Extensions;
using VirtoCommerce.SearchModule.Core.Model;
using VirtoCommerce.SearchModule.Core.Services;
using VirtoCommerce.SearchModule.Data.SearchPhraseParsing;
using VirtoCommerce.SearchModule.Data.Services;
using PlatformSettings = VirtoCommerce.Platform.Core.PlatformConstants.Settings;
using SearchSettings = VirtoCommerce.SearchModule.Core.ModuleConstants.Settings;

namespace VirtoCommerce.ProductComparison.Tests.Infrastructure;

/// <summary>
/// The platform and the modules the harnesses host, registered as Platform.Web's <c>Startup</c> and each module's own
/// <c>Module.Initialize</c> register them (at the package versions the test project references), with the parts no test
/// path reaches left out: export and import, bulk actions, background jobs, other modules' event handlers, admin
/// authorization, notifications. Every database is an in-memory SQLite one (<see cref="ModuleDatabases"/>) and the
/// search index is Lucene's, in RAM. A module's <c>Module.cs</c> ships in its zip, not on NuGet, so what it registers is
/// restated here from its source at the referenced version; where a module exposes its registration as an extension
/// (the platform's, X-API's, X-Catalog's, X-Cart's), that extension is called.
/// </summary>
internal static partial class PlatformModules
{
    public const string SearchProviderName = "Lucene";

    /// <summary>
    /// The modules' assemblies, loaded once, as the platform loads every installed module's at startup before the modules
    /// register: X-API finds a module's optional graph types (X-Frontend's white labeling ones) among the loaded ones.
    /// </summary>
    private static readonly Lazy<Assembly[]> _moduleAssemblies = new(() => Directory
        .GetFiles(AppContext.BaseDirectory, "VirtoCommerce.*.dll")
        .Select(path => Assembly.Load(AssemblyName.GetAssemblyName(path)))
        .ToArray());

    /// <summary>Everything both harnesses host, over <paramref name="configuration"/>.</summary>
    public static IServiceCollection AddPlatformModules(this IServiceCollection services, IConfiguration configuration)
    {
        _ = _moduleAssemblies.Value;

        return services
            .AddPlatform(configuration)
            .AddSecurity()
            .AddSearchModule()
            .AddStoreModule()
            .AddCoreModule()
            .AddCustomerModule()
            .AddCatalogModule()
            .AddPricingModule()
            .AddMarketingModule()
            .AddAssetsModule()
            .AddInventoryModule()
            .AddPaymentModule()
            .AddShippingModule()
            .AddCartModule()
            .AddXapiModule(configuration)
            .AddXCatalogModule()
            .AddXCartModule();
    }

    /// <summary>A module's database: the options a module builds from its connection string give way to SQLite.</summary>
    public static IServiceCollection AddModuleDatabase<TContext>(this IServiceCollection services) where TContext : DbContext
    {
        services.AddSingleton(provider => provider.GetRequiredService<ModuleDatabases>().Create<TContext>());
        services.AddScoped<TContext>();

        return services;
    }

    /// <summary>
    /// The platform's own services (Platform.Data's <c>AddPlatformServices</c>: settings, dynamic properties, localized
    /// items, the in-process event bus, the change log, the memory and request caches), over its database; the HTTP
    /// client factory Startup adds; optional module dependencies as the module manager resolves them; the distributed
    /// lock as the platform registers it without Redis.
    /// </summary>
    private static IServiceCollection AddPlatform(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddLogging();
        services.AddHttpClient();
        services.AddSingleton<SqlCommandLog>();
        services.AddSingleton<ModuleDatabases>();

        services.AddOptions<PlatformOptions>().Bind(configuration.GetSection("VirtoCommerce"));
        services.AddModuleDatabase<PlatformDbContext>();
        services.AddPlatformServices(configuration);
        services.AddOptions<FixedSettings>();

        services.AddTransient(typeof(IOptionalDependency<>), typeof(OptionalDependencyManager<>));

        services.AddOptions<DistributedLockOptions>();
        services.AddSingleton<IDistributedLock, InProcessDistributedLock>();

        return services;
    }

    /// <summary>
    /// Accounts and roles, as Platform.Web registers them: its <c>AddSecurityServices</c> (the platform's custom
    /// Identity stack and the user and role searches), then ASP.NET Identity over the security database. The data
    /// protection keys live in memory.
    /// </summary>
    private static IServiceCollection AddSecurity(this IServiceCollection services)
    {
        services.AddModuleDatabase<SecurityDbContext>();
        services.AddTransient<ISecurityRepository, SecurityRepository>();
        services.AddTransient<Func<ISecurityRepository>>(provider => () => provider.CreateScope().ServiceProvider.GetService<ISecurityRepository>());

        services.AddScoped<IUserNameResolver, HttpContextUserResolver>();
        services.AddSingleton<IPermissionsRegistrar, DefaultPermissionProvider>();
        services.AddScoped<IRoleSearchService, RoleSearchService>();
        services.AddSingleton<IUserSearchService>(provider =>
        {
            var scope = provider.CreateScope();
            return new UserSearchService(scope.ServiceProvider.GetService<Func<UserManager<ApplicationUser>>>(),
                                         scope.ServiceProvider.GetService<Func<RoleManager<Role>>>());
        });

        services.TryAddScoped<RoleManager<Role>, CustomRoleManager>();
        services.TryAddScoped<UserManager<ApplicationUser>, CustomUserManager>();
        services.TryAddScoped<IPasswordValidator<ApplicationUser>, CustomPasswordValidator>();
        services.TryAddScoped<IdentityErrorDescriber, CustomIdentityErrorDescriber>();
        services.TryAddScoped<IUserStore<ApplicationUser>, CustomUserStore>();
        services.AddSingleton<Func<RoleManager<Role>>>(provider => () => provider.CreateScope().ServiceProvider.GetService<RoleManager<Role>>());
        services.AddSingleton<Func<UserManager<ApplicationUser>>>(provider => () => provider.CreateScope().ServiceProvider.GetService<UserManager<ApplicationUser>>());
        services.AddSingleton<Func<SignInManager<ApplicationUser>>>(provider => () => provider.CreateScope().ServiceProvider.GetService<SignInManager<ApplicationUser>>());
        services.TryAddScoped<IUserClaimsPrincipalFactory<ApplicationUser>, CustomUserClaimsPrincipalFactory>();

        services.AddIdentity<ApplicationUser, Role>(options => options.Stores.MaxLengthForKeys = 128)
            .AddEntityFrameworkStores<SecurityDbContext>()
            .AddDefaultTokenProviders()
            .AddUserValidator<CustomUserValidator>();

        // The JWT claim types OpenIddict issues, which the platform makes Identity's (and the current user's).
        services.Configure<IdentityOptions>(options =>
        {
            options.ClaimsIdentity.UserNameClaimType = OpenIddictConstants.Claims.Name;
            options.ClaimsIdentity.UserIdClaimType = OpenIddictConstants.Claims.Subject;
            options.ClaimsIdentity.RoleClaimType = OpenIddictConstants.Claims.Role;
            options.ClaimsIdentity.EmailClaimType = OpenIddictConstants.Claims.Email;

            ClaimsPrincipalExtensions.UserIdClaimTypes = [options.ClaimsIdentity.UserIdClaimType, ClaimTypes.NameIdentifier];
            ClaimsPrincipalExtensions.UserNameClaimTypes = [options.ClaimsIdentity.UserNameClaimType];
        });
        services.AddOptions<PasswordOptionsExtended>();
        services.AddOptions<LockoutOptionsExtended>();
        services.AddOptions<UserOptionsExtended>();
        services.AddOptions<PasswordLoginOptions>();

        services.AddDataProtection().UseEphemeralDataProtectionProvider();

        // Platform.Web's authorization (its permission policy provider, by permission name, is the host's own): the
        // permission requirement handler; the modules add their own handlers.
        services.AddAuthorization();
        services.AddSingleton<IAuthorizationHandler, DefaultPermissionAuthorizationHandler>();

        return services;
    }

    /// <summary>The search module and the Lucene provider, which its gateway serves.</summary>
    private static IServiceCollection AddSearchModule(this IServiceCollection services)
    {
        services.AddSingleton(Options.Create(new SearchOptions { Provider = SearchProviderName, Scope = "test" }));
        services.AddSingleton<DummySearchProvider>();
        services.AddSingleton<SearchGateway>();
        services.AddSingleton<ISearchGateway>(provider => provider.GetRequiredService<SearchGateway>());
        services.AddSingleton<ISearchProvider>(provider => provider.GetRequiredService<SearchGateway>());
        services.AddTransient<ISearchPhraseParser, SearchPhraseParser>();
        services.AddSingleton<IIndexingManager, IndexingManager>();
        services.AddSingleton<ISearchRequestBuilderRegistrar, SearchRequestBuilderRegistrar>();
        services.AddTransient<IIndexFieldSettingService, IndexFieldSettingService>();
        services.AddTransient<IIndexFieldSettingSearchService, IndexFieldSettingService>();
        services.AddTransient<IIndexDocumentConverter, FieldValueConverter>();

        services.AddSingleton(Options.Create(new LuceneSearchOptions { UseInMemory = true }));
        services.AddSingleton<LuceneSearchProvider>();

        return services;
    }

    /// <summary>
    /// What Platform.Web's <c>Startup.Configure</c> and each hosted module's <c>PostInitialize</c> do at startup: register
    /// their settings (an unregistered setting can't be read) and their dynamic property and member types, add the
    /// prices and stock to the product index, register the payment and shipping methods, and serve the search
    /// providers. <paramref name="app"/> is the web host's in the storefront E2E and one over the harness's provider in the
    /// component tests.
    /// </summary>
    public static async Task PostInitializeAsync(IApplicationBuilder app)
    {
        var provider = app.ApplicationServices;
        var settings = provider.GetRequiredService<ISettingsRegistrar>();

        // Platform.Web's UsePlatformSettings.
        settings.RegisterSettings(PlatformSettings.AllSettings, "Platform");
        settings.RegisterSettingsForType(PlatformSettings.UserProfile.AllSettings, nameof(PlatformSettings.UserProfile));

        settings.RegisterSettings(SearchSettings.AllSettings, "VirtoCommerce.Search");
        settings.RegisterSettings(provider.GetServices<IndexDocumentConfiguration>()
            .Select(x => x.DocumentType)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Select(SearchSettings.IndexingJobs.IndexationDate)
            .ToArray(), "VirtoCommerce.Search");
        app.UseSearchProvider<DummySearchProvider>(name: null);
        app.UseSearchProvider<LuceneSearchProvider>(SearchProviderName);

        PostInitializeCommerceModules(provider);
        await PostInitializeSearchSourcesAsync(provider);
        PostInitializeExperienceApiModules(app);
    }
}
