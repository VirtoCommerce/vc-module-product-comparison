using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using VirtoCommerce.Platform.Caching;
using VirtoCommerce.Platform.Core.Caching;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Core.GenericCrud;
using VirtoCommerce.Platform.Data.GenericCrud;
using VirtoCommerce.ProductComparison.Core.Models;
using VirtoCommerce.ProductComparison.Core.Services;
using VirtoCommerce.ProductComparison.Data.Models;
using VirtoCommerce.ProductComparison.Data.Repositories;

namespace VirtoCommerce.ProductComparison.Data.Services;

public class CompareListSearchService(
        Func<IProductComparisonRepository> repositoryFactory,
        IPlatformMemoryCache platformMemoryCache,
        ICompareListService crudService,
        IOptions<CrudOptions> crudOptions)
    : SearchService<CompareListSearchCriteria, CompareListSearchResult, CompareList, CompareListEntity>(
        repositoryFactory,
        platformMemoryCache,
        crudService,
        crudOptions),
    ICompareListSearchService
{
    protected override IQueryable<CompareListEntity> BuildQuery(IRepository repository, CompareListSearchCriteria criteria)
    {
        var query = ((IProductComparisonRepository)repository).CompareLists;

        if (!criteria.ObjectIds.IsNullOrEmpty())
        {
            query = query.Where(x => criteria.ObjectIds.Contains(x.Id));
        }

        if (!string.IsNullOrEmpty(criteria.StoreId))
        {
            query = query.Where(x => x.StoreId == criteria.StoreId);
        }

        if (!string.IsNullOrEmpty(criteria.UserId))
        {
            query = query.Where(x => x.UserId == criteria.UserId);
        }

        if (!string.IsNullOrEmpty(criteria.OrganizationId))
        {
            query = query.Where(x => x.OrganizationId == criteria.OrganizationId);
        }

        if (criteria.OrganizationIdIsEmpty)
        {
            query = query.Where(x => x.OrganizationId == null);
        }

        if (criteria.IsAnonymous != null)
        {
            query = query.Where(x => x.IsAnonymous == criteria.IsAnonymous);
        }

        if (criteria.ModifiedBefore != null)
        {
            query = query.Where(x => x.ModifiedDate < criteria.ModifiedBefore);
        }

        return query;
    }

    // Keyed by user, so the service can expire one buyer's lookups; a search without a user is keyed by "".
    protected override IChangeToken CreateCacheToken(CompareListSearchCriteria criteria)
    {
        return GenericSearchCachingRegion<CompareList>.CreateChangeTokenForKey(criteria.UserId ?? string.Empty);
    }

    protected override IList<SortInfo> BuildSortExpression(CompareListSearchCriteria criteria)
    {
        var sortInfos = criteria.SortInfos;

        // Oldest first: when a race left two lists for one key, every lookup picks the same one.
        if (sortInfos.IsNullOrEmpty())
        {
            sortInfos =
            [
                new SortInfo { SortColumn = nameof(CompareListEntity.CreatedDate) },
                new SortInfo { SortColumn = nameof(CompareListEntity.Id) },
            ];
        }

        return sortInfos;
    }
}
