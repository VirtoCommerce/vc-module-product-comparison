using System;
using System.IO;
using System.Linq;
using GraphQL;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.IdentityModel.Tokens;
using OpenIddict.Abstractions;
using VirtoCommerce.Platform.Core.Modularity;
using VirtoCommerce.Platform.Modules;
using VirtoCommerce.ProductComparison.Tests.ComponentTests.Infrastructure;
using VirtoCommerce.ProductComparison.Tests.Infrastructure;

namespace VirtoCommerce.ProductComparison.Tests.StorefrontE2E.Infrastructure;

/// <summary>
/// What the real storefront needs from the backend beyond the platform and the modules the harness hosts
/// (<see cref="PlatformModules"/>), split by where it is registered: harness services, web-host services
/// (authentication, the storefront's files) and the pipeline before the modules' GraphQL endpoints.
/// </summary>
internal static class StorefrontServices
{
    public const string DefaultCulture = ProductComparisonTestContext.CultureName;

    /// <summary>The platform's paths: what a clone's dev proxy forwards to the backend (vc-frontend's vite.config.ts).</summary>
    private static readonly string[] _platformPaths =
        ["/api", "/graphql", "/ucp", "/connect", "/revoke", "/.well-known", "/cms-content", "/modules", "/externalsignin", "/signin-oidc", "/signin-google"];

    /// <summary>Added after the platform's and the module's registrations.</summary>
    public static void AddStorefrontShell(IServiceCollection services)
    {
        services.AddSingleton<TestTokenService>();

        // The module list the store query reports, and the platform's own AppManifestService over it for store.plugins.
        services.AddSingleton<IModuleService, StorefrontModuleService>();
        services.AddSingleton<IAppManifestService, AppManifestService>();

        services.AddStorefrontShellModules();
    }

    /// <summary>Web-host-only registrations: the platform's Kestrel options, bearer authentication for the storefront's token.</summary>
    public static void AddWebServices(IServiceCollection services)
    {
        // As Platform.Web's Startup: X-API's Newtonsoft serializer reads a request's body synchronously.
        services.Configure<KestrelServerOptions>(options => options.AllowSynchronousIO = true);

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

        // ASP.NET Identity makes its cookie the default scheme; the platform sets its own back, as this sets the token's.
        services.PostConfigure<AuthenticationOptions>(options =>
        {
            options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        });
    }

    /// <summary>
    /// The request pipeline up to the modules' endpoints, which their startup adds after it, as the platform's does: the
    /// plugin's and the storefront's files, the gate, authentication; then the health check and the token endpoints.
    /// </summary>
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

        app.MapGet("/health", () => Results.Text("Healthy"));
        TestTokenEndpoints.Map(app);
    }

    /// <summary>A page navigation (a GET for HTML) outside the platform's paths.</summary>
    private static bool IsStorefrontRoute(HttpRequest request)
        => (HttpMethods.IsGet(request.Method) || HttpMethods.IsHead(request.Method))
           && request.Headers.Accept.ToString().Contains("text/html", StringComparison.OrdinalIgnoreCase)
           && !_platformPaths.Any(x => request.Path.StartsWithSegments(x, StringComparison.OrdinalIgnoreCase));
}
