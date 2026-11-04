using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GraphQL;
using GraphQL.Types;
using MediatR;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.IdentityModel.Tokens;
using OpenIddict.Abstractions;
using VirtoCommerce.CatalogModule.Core.Search;
using VirtoCommerce.CustomerModule.Core.Services;
using VirtoCommerce.Platform.Core.DynamicProperties;
using VirtoCommerce.Platform.Core.Modularity;
using VirtoCommerce.Platform.Core.Security;
using VirtoCommerce.Platform.Core.Settings;
using VirtoCommerce.Platform.Modules;
using VirtoCommerce.ProductComparison.Tests.ComponentTests.Infrastructure;
using VirtoCommerce.ProfileExperienceApiModule.Data.Aggregates;
using VirtoCommerce.ProfileExperienceApiModule.Data.Aggregates.Contact;
using VirtoCommerce.ProfileExperienceApiModule.Data.Services;
using VirtoCommerce.ShippingModule.Core.Services;
using VirtoCommerce.StoreModule.Core.Services;
using VirtoCommerce.Xapi.Core.Extensions;
using VirtoCommerce.Xapi.Core.Infrastructure;
using VirtoCommerce.Xapi.Core.Models;
using VirtoCommerce.Xapi.Core.Queries;
using VirtoCommerce.Xapi.Core.Services;
using VirtoCommerce.Xapi.Core.Subscriptions;
using VirtoCommerce.Xapi.Data.Queries;
using VirtoCommerce.Xapi.Data.Services;
using VirtoCommerce.XCart.Core.Services;
using VirtoCommerce.XCatalog.Core.Authorization;
using VirtoCommerce.XCatalog.Core.Models;
using VirtoCommerce.XCatalog.Core.Queries;
using VirtoCommerce.XCatalog.Core.Services;
using VirtoCommerce.XCatalog.Data.Queries;
using VirtoCommerce.XCatalog.Data.Schemas;
using VirtoCommerce.XCatalog.Data.Services;
using ProductComparisonSettings = VirtoCommerce.ProductComparison.Core.ModuleConstants.Settings;
using ProfileUserType = VirtoCommerce.ProfileExperienceApiModule.Data.Schemas.UserType;
using StoreSettings = VirtoCommerce.StoreModule.Core.ModuleConstants.Settings;

namespace VirtoCommerce.ProductComparison.Tests.StorefrontE2E.Infrastructure;

/// <summary>
/// What the real storefront needs from the backend beyond the compare list's schema, split by where it is registered:
/// harness overrides (services and schema), web-host services (authentication, GraphQL transport) and the pipeline.
/// Real code wherever a referenced package has it; the rest are the smallest stand-ins that satisfy the storefront.
/// </summary>
internal static class StorefrontServices
{
    public const string DefaultCulture = "en-US";

    /// <summary>The platform's paths: what a clone's dev proxy forwards to the backend (vc-frontend's vite.config.ts).</summary>
    private static readonly string[] _platformPaths =
        ["/api", "/graphql", "/ucp", "/connect", "/revoke", "/.well-known", "/cms-content", "/modules", "/externalsignin", "/signin-oidc", "/signin-google"];

    /// <summary>Applied as the harness's last-wins overrides, so the component tests keep their own defaults.</summary>
    public static void AddStorefrontShell(IServiceCollection services)
    {
        // Identity claim types as the platform configures them (OpenIddict names), as the tokens carry them.
        services.Configure<IdentityOptions>(options =>
        {
            options.ClaimsIdentity.UserIdClaimType = OpenIddictConstants.Claims.Subject;
            options.ClaimsIdentity.UserNameClaimType = OpenIddictConstants.Claims.Name;
            options.ClaimsIdentity.RoleClaimType = OpenIddictConstants.Claims.Role;
            options.ClaimsIdentity.EmailClaimType = OpenIddictConstants.Claims.Email;
        });
        services.AddSingleton<TestTokenService>();

        AddStoreQuery(services);
        AddCatalog(services);
        AddProfileTypes(services);

        // Constructor dependencies of the real cart graph types (cart resolves to null, but the types must build).
        services.AddSingleton<IXapiMapper, XapiMapper>();
        services.AddSingleton<ICartAvailMethodsService, NoCartAvailMethodsService>();
        services.AddSingleton(UnusedService.Of<IPickupLocationSearchService>());
        services.AddSingleton(UnusedService.Of<ICartSharingService>());

        // The shell's remaining root fields: schema-complete (real response types, so the storefront's selection sets
        // validate) and data-empty.
        services.AddSingleton<ISchemaBuilder, StorefrontShellSchemaBuilder>();
        services.AddTransient<PageContextResponseType>();
        services.AddTransient<MenuLinkListType>();
        services.AddTransient<MenuLinkType>();
        services.AddTransient<SearchHistoryResultType>();

        // One schema over every registered builder (the compare list's, the store's, the catalog's and the shell's):
        // the storefront sends ALL operations to /graphql; the scoped /graphql/product-comparison only serves codegen.
        services.AddSingleton<ISchema, SchemaFactory>();
    }

    /// <summary>
    /// store(domain): the REAL Xapi query builder and handler over the harness's store double, with small stand-ins for
    /// the services the handler reads around the store; store.plugins comes from the platform's own AppManifestService
    /// over the module list double, so the storefront finds the compare plugin the way it does in production.
    /// </summary>
    private static void AddStoreQuery(IServiceCollection services)
    {
        services.Configure<StoresOptions>(options => options.DefaultStore = ProductComparisonTestContext.StoreId);
        services.Configure<GraphQLWebSocketOptions>(_ => { });
        services.AddTransient<IStoreDomainResolverService, StoreDomainResolverService>();
        services.AddSingleton<IStoreSearchService, EmptyStoreSearchService>();
        services.AddSingleton<IStoreCurrencyResolver, CurrencyServiceStoreCurrencyResolver>();
        services.AddSingleton<IStoreAuthenticationService, PasswordOnlyStoreAuthenticationService>();
        services.AddSingleton<IModuleService, StorefrontModuleService>();
        services.AddSingleton<IDynamicPropertySearchService, NoDynamicPropertySearchService>();
        services.AddSingleton<IDynamicPropertyResolverService, DynamicPropertyResolverService>();
        // The dynamic property value type's: dictionary values only, and nothing here has dynamic properties.
        services.AddSingleton(UnusedService.Of<IDynamicPropertyDictionaryItemsService>());
        services.AddSingleton<IAppManifestService, AppManifestService>();
        services.AddSingleton<ISchemaBuilder, GetStoreQueryBuilder>();
        services.AddTransient<IRequestHandler<GetStoreQuery, StoreResponse>, GetStoreQueryHandler>();
    }

    /// <summary>
    /// product(id): X-Catalog's REAL root fields (DigitalCatalogSchema) and its store access rule, over the in-memory
    /// <see cref="StorefrontCatalog"/>, which answers the product and category loads the index answers in production.
    /// The product page's extras (configuration, slug, outline, videos, associations) go through X-Catalog's REAL
    /// handlers over empty catalog searches.
    /// </summary>
    private static void AddCatalog(IServiceCollection services)
    {
        services.AddSingleton<ISchemaBuilder, DigitalCatalogSchema>();
        services.AddSingleton<IAuthorizationHandler, CanAccessStoreAuthorizationHandler>();

        services.AddSingleton<StorefrontCatalog>();
        services.AddSingleton<IRequestHandler<LoadProductsQuery, LoadProductResponse>>(provider => provider.GetRequiredService<StorefrontCatalog>());
        services.AddSingleton<IRequestHandler<LoadCategoryQuery, LoadCategoryResponse>>(provider => provider.GetRequiredService<StorefrontCatalog>());

        services.AddSingleton<NoCatalogExtrasSearchService>();
        services.AddSingleton<IProductConfigurationSearchService>(provider => provider.GetRequiredService<NoCatalogExtrasSearchService>());
        services.AddSingleton<IVideoSearchService>(provider => provider.GetRequiredService<NoCatalogExtrasSearchService>());
        services.AddSingleton<IProductAssociationSearchService>(provider => provider.GetRequiredService<NoCatalogExtrasSearchService>());
        services.AddSingleton<IFacetMapper, FacetMapper>();
        services.AddSingleton<IXCatalogMapper, XCatalogMapper>();
        services.AddTransient<IRequestHandler<GetProductConfigurationsQuery, Dictionary<string, bool>>, GetProductConfigurationsQueryHandler>();
        services.AddTransient<IRequestHandler<LoadRelatedSlugPathQuery, LoadRelatedSlugPathResponse>, LoadRelatedSlugPathQueryHandler>();
        services.AddTransient<IRequestHandler<LoadRelatedCatalogOutlineQuery, LoadRelatedCatalogOutlineResponse>, LoadRelatedCatalogOutlineQueryHandler>();
        services.AddTransient<IRequestHandler<SearchVideoQuery, SearchVideoQueryResponse>, SearchVideoQueryHandler>();
        services.AddTransient<IRequestHandler<SearchProductAssociationsQuery, SearchProductAssociationsResponse>, SearchProductAssociationsQueryHandler>();
    }

    /// <summary>
    /// pageContext.user: the REAL profile UserType. The contact and organization types under it take customer-module
    /// services the harness doesn't host; the E2E buyers have no contact, so those resolvers never run.
    /// </summary>
    private static void AddProfileTypes(IServiceCollection services)
    {
        services.AddGraphQL(builder => builder.AddGraphTypes(typeof(ProfileUserType).Assembly));

        services.AddSingleton(UnusedService.Of<IContactAggregateRepository>());
        services.AddSingleton(UnusedService.Of<IMemberAggregateFactory>());
        services.AddSingleton(UnusedService.Of<IMemberAddressService>());
        services.AddSingleton(UnusedService.Of<ICustomerPreferenceService>());
        services.AddSingleton(UnusedService.Of<IOrganizationMembershipSearchService>());
        services.AddSingleton<Func<RoleManager<Role>>>(() => throw new NotSupportedException("The storefront E2E backend has no roles."));
    }

    /// <summary>Web-host-only registrations: bearer authentication for the storefront's token, GraphQL user context.</summary>
    public static void AddWebServices(IServiceCollection services)
    {
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.MapInboundClaims = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidIssuer = TestTokenService.Issuer,
                    ValidAudience = TestTokenService.Audience,
                    IssuerSigningKey = TestTokenService.SigningKey,
                    NameClaimType = OpenIddictConstants.Claims.Name,
                    RoleClaimType = OpenIddictConstants.Claims.Role,
                };
            });

        services.PostConfigure<AuthenticationOptions>(options =>
        {
            options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        });

        services.AddDataProtection().UseEphemeralDataProtectionProvider();

        // What Xapi.Web's Module does for the HTTP transport: the GraphQL user context is the request principal.
        services.AddGraphQL(builder => builder.AddUserContextBuilder(httpContext => new GraphQLUserContext(httpContext.User)));
    }

    public static void ConfigurePipeline(WebApplication app, RequestGate gate, string storefrontBuild)
    {
        // The plugin's build, on the platform's route for it; the storefront's dev proxy forwards that path here.
        // Without a build there is nothing to serve, and the groups say so (StorefrontPlugin).
        if (Directory.Exists(StorefrontPlugin.BuildDirectory))
        {
            app.UseStaticFiles(new StaticFileOptions
            {
                FileProvider = new PhysicalFileProvider(StorefrontPlugin.BuildDirectory),
                RequestPath = StorefrontPlugin.RequestPath,
            });
        }

        // A built storefront, served from the backend's own origin as the cloud serves it: its files, and index.html
        // for a page navigation to one of its routes. The platform's paths never fall back, so a missing endpoint
        // stays a 404 as it would behind the dev proxy.
        if (storefrontBuild != null)
        {
            var storefrontFiles = new PhysicalFileProvider(storefrontBuild);
            app.Use((context, next) =>
            {
                if (IsStorefrontRoute(context.Request) && !storefrontFiles.GetFileInfo(context.Request.Path.Value).Exists)
                {
                    context.Request.Path = "/index.html";
                }

                return next(context);
            });
            app.UseStaticFiles(new StaticFileOptions { FileProvider = storefrontFiles, ServeUnknownFileTypes = true });
        }

        // Everything past the static files takes its turn at the gate: the storefront sends its queries in parallel.
        gate.Use(app);

        app.UseAuthentication();
        app.UseAuthorization();

        // Xapi.Core's own HTTP transport registration; the Xapi module calls the same extension for /graphql.
        app.UseSchemaGraphQL<ISchema>(schemaIntrospectionEnabled: false);

        app.MapGet("/health", () => Results.Text("Healthy"));
        TestTokenEndpoints.Map(app);
    }

    /// <summary>
    /// The store the harness double hands out, made into a storefront's: a name, its language and currency, guests
    /// allowed in, and the module's settings under the module's id, as the platform's settings registrar stamps them
    /// when the module registers its store-level settings (public ones reach the storefront as store.settings.modules).
    /// </summary>
    public static void ConfigureStore(ProductComparisonTestContext context)
    {
        var store = context.Stores.Add(ProductComparisonTestContext.StoreId);
        store.Name = "E2E store";
        store.Languages = [DefaultCulture];
        store.DefaultLanguage = DefaultCulture;
        store.Currencies = ["USD"];
        store.DefaultCurrency = "USD";

        foreach (var setting in store.Settings.Where(x => ProductComparisonSettings.StoreLevelSettings.Any(descriptor => descriptor.Name == x.Name)))
        {
            setting.ModuleId = StorefrontPlugin.ModuleId;
        }

        store.Settings.Add(new ObjectSettingEntry
        {
            Name = StoreSettings.General.AllowAnonymousUsers.Name,
            ValueType = SettingValueType.Boolean,
            Value = true,
        });
    }

    /// <summary>A page navigation (a GET for HTML) outside the platform's paths.</summary>
    private static bool IsStorefrontRoute(HttpRequest request)
        => (HttpMethods.IsGet(request.Method) || HttpMethods.IsHead(request.Method))
           && request.Headers.Accept.ToString().Contains("text/html", StringComparison.OrdinalIgnoreCase)
           && !_platformPaths.Any(x => request.Path.StartsWithSegments(x, StringComparison.OrdinalIgnoreCase));
}
