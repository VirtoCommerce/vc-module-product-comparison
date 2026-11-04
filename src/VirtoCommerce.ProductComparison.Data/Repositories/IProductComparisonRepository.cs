using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.ProductComparison.Data.Models;

namespace VirtoCommerce.ProductComparison.Data.Repositories;

public interface IProductComparisonRepository : IRepository
{
    IQueryable<CompareListEntity> CompareLists { get; }

    IQueryable<CompareListItemEntity> CompareListItems { get; }

    IQueryable<CompareListItemSectionEntity> CompareListItemSections { get; }

    Task<IList<CompareListEntity>> GetCompareListsByIdsAsync(IList<string> ids);

    Task<int> DeleteItemsByProductIdsAsync(IList<string> productIds);

    Task<IList<CompareListOwner>> GetRegisteredListOwnersAsync(string afterId, int take);
}
