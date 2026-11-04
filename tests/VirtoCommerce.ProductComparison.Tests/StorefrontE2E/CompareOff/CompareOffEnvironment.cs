using VirtoCommerce.ProductComparison.Tests.ComponentTests.Infrastructure;
using VirtoCommerce.ProductComparison.Tests.StorefrontE2E.Infrastructure;
using ProductComparisonSettings = VirtoCommerce.ProductComparison.Core.ModuleConstants.Settings;

namespace VirtoCommerce.ProductComparison.Tests.StorefrontE2E.CompareOff;

/// <summary>
/// Group 3: the module installed, its plugin built and listed in store.plugins, and compare off for the store
/// (<c>ProductComparison.Enabled</c> false), as an administrator saves it on the store's settings.
/// </summary>
public sealed class CompareOffEnvironment : StorefrontEnvironment
{
    private protected override void ConfigureStore(ProductComparisonTestContext context)
        => context.SetStoreSetting(InProcessBackend.StoreId, ProductComparisonSettings.General.Enabled, false);
}
