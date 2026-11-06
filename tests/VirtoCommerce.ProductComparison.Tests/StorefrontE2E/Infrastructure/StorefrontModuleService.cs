using System.Collections.Generic;
using System.Linq;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Core.Modularity;
using VirtoCommerce.ProductComparison.Tests.Infrastructure;

namespace VirtoCommerce.ProductComparison.Tests.StorefrontE2E.Infrastructure;

/// <summary>
/// The module list the store query reports to the storefront, and the one the platform's AppManifestService walks for
/// store.plugins. The module under test comes from its own module.manifest, the file the platform reads, so its id and
/// version are the real ones and its root is the Web project, where the plugin build lies. X-Frontend stands in for the
/// module that declares the storefront app ("vc-frontend") whose plugins store.plugins lists.
/// </summary>
internal sealed class StorefrontModuleService : IModuleService
{
    /// <summary>
    /// What the stand-in reports: a version no storefront expects to exceed. The storefront compares each module's version
    /// with its own backend-packages.json and covers the page with an "outdated backend" error for a lower one.
    /// </summary>
    private const string StandInVersion = "999.0.0";

    private readonly IList<ManifestModuleInfo> _installed = CreateInstalledModules();

    public IList<ManifestModuleInfo> GetModules() => _installed;

    public IList<ManifestModuleInfo> GetInstalledModules() => _installed;

    public IList<ManifestModuleInfo> GetFailedModules() => [];

    public bool IsInstalled(string moduleId) => GetModule(moduleId) != null;

    public bool IsInstalled(string moduleId, string minVersion) => IsInstalled(moduleId);

    public ManifestModuleInfo GetModule(string moduleId) => _installed.FirstOrDefault(x => x.Id.EqualsIgnoreCase(moduleId));

    private static List<ManifestModuleInfo> CreateInstalledModules()
    {
        // Rooted at the Web project, where the platform looks for the plugin's build (plugins/vc-frontend).
        var productComparison = TestedModule.CreateInfo();

        // X-Frontend's own manifest ships only in its module zip, so its app declaration is restated here.
        var xFrontend = new ManifestModuleInfo().LoadFromManifest(new ModuleManifest
        {
            Id = "VirtoCommerce.XFrontend",
            Version = StandInVersion,
            PlatformVersion = productComparison.PlatformVersion.ToString(),
            Apps = [new ManifestApp { Id = "vc-frontend" }],
        });

        return [productComparison, xFrontend];
    }
}
