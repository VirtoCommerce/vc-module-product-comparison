using Xunit;

namespace VirtoCommerce.ProductComparison.Tests.StorefrontE2E.CompareLayout;

/// <summary>Group 4 of the storefront tests, the compare page's layout: one backend, one seed, one browser, tests run one at a time.</summary>
[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class CompareLayoutCollection : ICollectionFixture<CompareLayoutEnvironment>
{
    public const string Name = "Storefront E2E: compare page layout";
}
