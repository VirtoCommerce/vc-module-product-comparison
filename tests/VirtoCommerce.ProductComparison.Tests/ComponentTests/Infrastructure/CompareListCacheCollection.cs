using Xunit;

namespace VirtoCommerce.ProductComparison.Tests.ComponentTests.Infrastructure;

/// <summary>
/// Tests that observe the CRUD and search caches. Each harness has its own memory cache, but the cache regions are
/// process-wide statics, and every save expires the search region: a test running alongside would hand these tests
/// fresh data and hide a missing expiry. So they run alone.
/// </summary>
[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class CompareListCacheCollection
{
    public const string Name = "Compare list caches";
}
