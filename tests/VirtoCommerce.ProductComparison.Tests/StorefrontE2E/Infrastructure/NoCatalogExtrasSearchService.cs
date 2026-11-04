using System.Threading.Tasks;
using VirtoCommerce.CatalogModule.Core.Model.Search;
using VirtoCommerce.CatalogModule.Core.Search;

namespace VirtoCommerce.ProductComparison.Tests.StorefrontE2E.Infrastructure;

/// <summary>
/// The catalog's searches behind the product page's extras, for X-Catalog's real query handlers: the catalog has no
/// product configurations, videos or associations, so every product is plain, has no videos and no related products.
/// </summary>
internal sealed class NoCatalogExtrasSearchService : IProductConfigurationSearchService, IVideoSearchService, IProductAssociationSearchService
{
    public Task<ProductConfigurationSearchResult> SearchAsync(ProductConfigurationSearchCriteria criteria, bool clone = true)
        => Task.FromResult(new ProductConfigurationSearchResult());

    public Task<VideoSearchResult> SearchAsync(VideoSearchCriteria criteria, bool clone = true)
        => Task.FromResult(new VideoSearchResult());

    public Task<ProductAssociationSearchResult> SearchProductAssociationsAsync(ProductAssociationSearchCriteria criteria)
        => Task.FromResult(new ProductAssociationSearchResult());
}
