using Xunit;

namespace VirtoCommerce.ProductComparison.Tests.StorefrontE2E.GuestListsOff;

/// <summary>Group 2 of the storefront tests: one backend, one store, one browser; tests run one at a time.</summary>
[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class GuestListsOffCollection : ICollectionFixture<GuestListsOffEnvironment>
{
    public const string Name = "Storefront E2E: a store that keeps compare for signed-in buyers";
}
