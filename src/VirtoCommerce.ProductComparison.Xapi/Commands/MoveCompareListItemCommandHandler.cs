using FluentValidation;
using VirtoCommerce.ProductComparison.Xapi.Aggregates;

namespace VirtoCommerce.ProductComparison.Xapi.Commands;

public class MoveCompareListItemCommandHandler(ICompareListAggregateRepository repository, AbstractValidator<MoveCompareListItemCommand> validator)
    : CompareListCommandHandler<MoveCompareListItemCommand>(repository, validator)
{
    protected override void Update(CompareListAggregate aggregate, MoveCompareListItemCommand request)
    {
        aggregate.MoveItem(request.ItemId, request.Position);
    }
}
