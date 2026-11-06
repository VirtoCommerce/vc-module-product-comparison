using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Core.Domain;
using VirtoCommerce.Platform.Data.Infrastructure;
using VirtoCommerce.ProductComparison.Data.Models;

namespace VirtoCommerce.ProductComparison.Data.Repositories;

public class ProductComparisonRepository : DbContextRepositoryBase<ProductComparisonDbContext>, IProductComparisonRepository
{
    public ProductComparisonRepository(ProductComparisonDbContext dbContext, IUnitOfWork unitOfWork = null)
        : base(dbContext, unitOfWork)
    {
    }

    public virtual IQueryable<CompareListEntity> CompareLists => DbContext.Set<CompareListEntity>();

    public virtual IQueryable<CompareListItemEntity> CompareListItems => DbContext.Set<CompareListItemEntity>();

    public virtual IQueryable<CompareListItemSectionEntity> CompareListItemSections => DbContext.Set<CompareListItemSectionEntity>();

    public virtual async Task<IList<CompareListEntity>> GetCompareListsByIdsAsync(IList<string> ids)
    {
        if (ids.IsNullOrEmpty())
        {
            return [];
        }

        var lists = await CompareLists.Where(x => ids.Contains(x.Id)).ToListAsync();

        if (lists.Count > 0)
        {
            await CompareListItems.Where(x => ids.Contains(x.CompareListId)).LoadAsync();
            await CompareListItemSections.Where(x => ids.Contains(x.CompareListItem.CompareListId)).LoadAsync();
        }

        return lists;
    }

    public virtual Task<int> DeleteItemsByProductIdsAsync(IList<string> productIds)
    {
        return CompareListItems.Where(x => productIds.Contains(x.ProductId)).ExecuteDeleteAsync();
    }

    // Ordered by id and continued after the last one, so a batch's deletions don't shift the next batch.
    public virtual async Task<IList<CompareListOwner>> GetRegisteredListOwnersAsync(string afterId, int take)
    {
        var query = CompareLists.Where(x => !x.IsAnonymous);

        if (afterId != null)
        {
            query = query.Where(x => string.Compare(x.Id, afterId) > 0);
        }

        return await query
            .OrderBy(x => x.Id)
            .Take(take)
            .Select(x => new CompareListOwner { Id = x.Id, UserId = x.UserId, OrganizationId = x.OrganizationId })
            .ToListAsync();
    }
}
