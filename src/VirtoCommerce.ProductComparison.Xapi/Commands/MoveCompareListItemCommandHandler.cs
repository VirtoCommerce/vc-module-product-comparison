using VirtoCommerce.ProductComparison.Xapi.Aggregates;

namespace VirtoCommerce.ProductComparison.Xapi.Commands;

public class MoveCompareListItemCommandHandler(ICompareListAggregateRepository repository)
    : CompareListCommandHandler<MoveCompareListItemCommand>(repository)
{
    protected override void Update(CompareListAggregate aggregate, MoveCompareListItemCommand request)
    {
        aggregate.MoveItem(request.ItemId, request.Position);
    }
}
