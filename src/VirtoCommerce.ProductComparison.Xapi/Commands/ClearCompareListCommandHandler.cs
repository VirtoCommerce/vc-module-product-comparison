using VirtoCommerce.ProductComparison.Xapi.Aggregates;

namespace VirtoCommerce.ProductComparison.Xapi.Commands;

public class ClearCompareListCommandHandler(ICompareListAggregateRepository repository)
    : CompareListCommandHandler<ClearCompareListCommand>(repository)
{
    protected override void Update(CompareListAggregate aggregate, ClearCompareListCommand request)
    {
        aggregate.Clear();
    }
}
