using FluentValidation;
using VirtoCommerce.ProductComparison.Xapi.Aggregates;

namespace VirtoCommerce.ProductComparison.Xapi.Commands;

public class RemoveCompareListItemsCommandHandler(ICompareListAggregateRepository repository, AbstractValidator<RemoveCompareListItemsCommand> validator)
    : CompareListCommandHandler<RemoveCompareListItemsCommand>(repository, validator)
{
    protected override void Update(CompareListAggregate aggregate, RemoveCompareListItemsCommand request)
    {
        aggregate.RemoveItems(request.ItemIds);
    }
}
