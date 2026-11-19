using System.Threading.Tasks;
using VirtoCommerce.ProductComparison.Tests.ComponentTests.Infrastructure;
using VirtoCommerce.ProductComparison.Tests.StorefrontE2E.Infrastructure;
using ProductComparisonSettings = VirtoCommerce.ProductComparison.Core.ModuleConstants.Settings;

namespace VirtoCommerce.ProductComparison.Tests.StorefrontE2E.GuestListsOff;

/// <summary>
/// Group 2: a store with compare on and guests' lists off (<c>ProductComparison.AnonymousListEnabled</c> false), as an
/// administrator saves it on the store's settings.
/// </summary>
public sealed class GuestListsOffEnvironment : StorefrontEnvironment
{
    private protected override Task ConfigureStoreAsync(ProductComparisonTestContext context)
        => context.SetStoreSettingAsync(InProcessBackend.StoreId, ProductComparisonSettings.General.AnonymousListEnabled, false);
}
