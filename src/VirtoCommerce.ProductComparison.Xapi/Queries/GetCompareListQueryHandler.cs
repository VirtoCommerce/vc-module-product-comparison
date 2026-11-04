using System.Threading;
using System.Threading.Tasks;
using VirtoCommerce.ProductComparison.Xapi.Aggregates;
using VirtoCommerce.Xapi.Core.Infrastructure;

namespace VirtoCommerce.ProductComparison.Xapi.Queries;

public class GetCompareListQueryHandler(ICompareListAggregateRepository repository)
    : IQueryHandler<GetCompareListQuery, CompareListAggregate>
{
    public virtual async Task<CompareListAggregate> Handle(GetCompareListQuery request, CancellationToken cancellationToken)
    {
        var aggregate = await repository.GetAsync(request);

        // Nothing to show: no list yet, compare is off for the store, or guest lists are off.
        if (!aggregate.IsSaved || !aggregate.IsEnabled || (aggregate.List.IsAnonymous && !aggregate.IsAnonymousListEnabled))
        {
            return null;
        }

        return aggregate;
    }
}
