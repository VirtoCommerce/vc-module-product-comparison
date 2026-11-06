using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using VirtoCommerce.ProductComparison.Xapi.Aggregates;
using VirtoCommerce.Xapi.Core.Infrastructure;

namespace VirtoCommerce.ProductComparison.Xapi.Queries;

public class GetCompareListQueryHandler(ICompareListAggregateRepository repository, AbstractValidator<GetCompareListQuery> validator)
    : IQueryHandler<GetCompareListQuery, CompareListAggregate>
{
    public virtual async Task<CompareListAggregate> Handle(GetCompareListQuery request, CancellationToken cancellationToken)
    {
        await validator.ValidateAndThrowAsync(request, cancellationToken);

        var aggregate = await repository.GetAsync(request);

        // Nothing to show: no list yet, compare is off for the store, or guest lists are off.
        return aggregate.IsSaved && aggregate.IsAvailable ? aggregate : null;
    }
}
