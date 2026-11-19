using Xunit;

namespace VirtoCommerce.ProductComparison.Tests.StorefrontE2E.CompareOff;

/// <summary>Group 3 of the storefront tests: one backend, one store, one browser; tests run one at a time.</summary>
[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class CompareOffCollection : ICollectionFixture<CompareOffEnvironment>
{
    public const string Name = "Storefront E2E: a store with compare off";
}
