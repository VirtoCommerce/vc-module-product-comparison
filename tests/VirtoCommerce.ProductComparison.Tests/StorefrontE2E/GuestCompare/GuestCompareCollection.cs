using Xunit;

namespace VirtoCommerce.ProductComparison.Tests.StorefrontE2E.GuestCompare;

/// <summary>Group 1 of the storefront tests: one backend, one store, one browser; tests run one at a time.</summary>
[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class GuestCompareCollection : ICollectionFixture<GuestCompareEnvironment>
{
    public const string Name = "Storefront E2E: compare lists on a store with compare on";
}
