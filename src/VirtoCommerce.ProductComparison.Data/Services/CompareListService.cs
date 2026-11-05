using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VirtoCommerce.Platform.Caching;
using VirtoCommerce.Platform.Core.Caching;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Core.Events;
using VirtoCommerce.Platform.Data.GenericCrud;
using VirtoCommerce.ProductComparison.Core.Events;
using VirtoCommerce.ProductComparison.Core.Models;
using VirtoCommerce.ProductComparison.Core.Services;
using VirtoCommerce.ProductComparison.Data.Models;
using VirtoCommerce.ProductComparison.Data.Repositories;

namespace VirtoCommerce.ProductComparison.Data.Services;

public class CompareListService(
        Func<IProductComparisonRepository> repositoryFactory,
        IPlatformMemoryCache platformMemoryCache,
        IEventPublisher eventPublisher)
    : CrudService<CompareList, CompareListEntity, CompareListChangingEvent, CompareListChangedEvent>(
        repositoryFactory,
        platformMemoryCache,
        eventPublisher),
    ICompareListService
{
    private readonly Func<IProductComparisonRepository> _repositoryFactory = repositoryFactory;

    protected override Task<IList<CompareListEntity>> LoadEntities(IRepository repository, IList<string> ids, string responseGroup)
    {
        return ((IProductComparisonRepository)repository).GetCompareListsByIdsAsync(ids);
    }

    // One buyer's save expires only that buyer's lookups, as the cart does: not every shopper's cached list.
    protected override void ClearSearchCache(IList<CompareList> models)
    {
        GenericSearchCachingRegion<CompareList>.ExpireTokenForKey(string.Empty);

        foreach (var userId in models.Select(x => x.UserId).Distinct())
        {
            GenericSearchCachingRegion<CompareList>.ExpireTokenForKey(userId);
        }
    }

    public virtual async Task RemoveProductsAsync(IList<string> productIds)
    {
        using var repository = _repositoryFactory();

        var deletedCount = await repository.DeleteItemsByProductIdsAsync(productIds);

        if (deletedCount > 0)
        {
            ClearCacheAfterProductRemoval(productIds);
        }
    }

    // The set-based delete bypasses the CrudService pipeline and doesn't know which lists it changed: every list goes.
    protected virtual void ClearCacheAfterProductRemoval(IList<string> productIds)
    {
        GenericCachingRegion<CompareList>.ExpireRegion();
        GenericSearchCachingRegion<CompareList>.ExpireRegion();
    }
}
