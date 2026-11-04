using System.Threading.Tasks;
using VirtoCommerce.Platform.Core.DynamicProperties;

namespace VirtoCommerce.ProductComparison.Tests.StorefrontE2E.Infrastructure;

/// <summary>No dynamic properties are defined: the store query and the profile types load none.</summary>
internal sealed class NoDynamicPropertySearchService : IDynamicPropertySearchService
{
    public Task<DynamicPropertySearchResult> SearchAsync(DynamicPropertySearchCriteria criteria, bool clone = true)
        => Task.FromResult(new DynamicPropertySearchResult());
}
