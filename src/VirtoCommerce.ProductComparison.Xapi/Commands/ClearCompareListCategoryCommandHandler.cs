using FluentValidation;
using VirtoCommerce.ProductComparison.Xapi.Aggregates;

namespace VirtoCommerce.ProductComparison.Xapi.Commands;

public class ClearCompareListCategoryCommandHandler(ICompareListAggregateRepository repository, AbstractValidator<ClearCompareListCategoryCommand> validator)
    : CompareListCommandHandler<ClearCompareListCategoryCommand>(repository, validator)
{
    protected override void Update(CompareListAggregate aggregate, ClearCompareListCategoryCommand request)
    {
        aggregate.ClearCategory(request.CategoryId);
    }
}
