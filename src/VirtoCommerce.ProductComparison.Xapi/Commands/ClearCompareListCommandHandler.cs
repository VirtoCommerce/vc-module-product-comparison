using FluentValidation;
using VirtoCommerce.ProductComparison.Xapi.Aggregates;

namespace VirtoCommerce.ProductComparison.Xapi.Commands;

public class ClearCompareListCommandHandler(ICompareListAggregateRepository repository, AbstractValidator<ClearCompareListCommand> validator)
    : CompareListCommandHandler<ClearCompareListCommand>(repository, validator)
{
    protected override void Update(CompareListAggregate aggregate, ClearCompareListCommand request)
    {
        aggregate.Clear();
    }
}
