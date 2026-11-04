using VirtoCommerce.ProductComparison.Xapi.Aggregates;

namespace VirtoCommerce.ProductComparison.Xapi.Commands;

public class RemoveCompareListItemsCommandHandler(ICompareListAggregateRepository repository)
    : CompareListCommandHandler<RemoveCompareListItemsCommand>(repository)
{
    protected override void Update(CompareListAggregate aggregate, RemoveCompareListItemsCommand request)
    {
        aggregate.RemoveItems(request.ItemIds);
    }
}
