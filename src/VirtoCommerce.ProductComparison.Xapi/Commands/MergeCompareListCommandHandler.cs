using System.Threading;
using System.Threading.Tasks;
using MediatR;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.ProductComparison.Xapi.Aggregates;

namespace VirtoCommerce.ProductComparison.Xapi.Commands;

public class MergeCompareListCommandHandler(ICompareListAggregateRepository repository)
    : IRequestHandler<MergeCompareListCommand, CompareListAggregate>
{
    public virtual async Task<CompareListAggregate> Handle(MergeCompareListCommand request, CancellationToken cancellationToken)
    {
        var aggregate = await repository.GetAsync(request);
        aggregate.EnsureAvailable();

        var source = await repository.GetListByIdAsync(request.SecondListId);

        // Nothing to merge: the source is gone (merged before, or cleaned up), belongs to another store, or is this list.
        if (source == null || !source.StoreId.EqualsIgnoreCase(request.StoreId) || source.Id.EqualsIgnoreCase(aggregate.List.Id))
        {
            return aggregate.IsSaved ? aggregate : null;
        }

        aggregate.Merge(source);
        await repository.SaveAsync(aggregate);
        await repository.DeleteListAsync(source);

        return aggregate;
    }
}
