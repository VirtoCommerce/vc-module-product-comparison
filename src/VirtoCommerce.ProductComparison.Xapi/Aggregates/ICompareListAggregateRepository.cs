using System.Threading.Tasks;
using VirtoCommerce.ProductComparison.Core.Models;
using VirtoCommerce.ProductComparison.Xapi.Models;

namespace VirtoCommerce.ProductComparison.Xapi.Aggregates;

public interface ICompareListAggregateRepository
{
    // The caller's list, or a new unsaved one when they have none yet.
    Task<CompareListAggregate> GetAsync(ICompareListRequest request);

    Task<CompareList> GetListByIdAsync(string listId);

    Task SaveAsync(CompareListAggregate aggregate);

    Task DeleteListAsync(CompareList list);
}
