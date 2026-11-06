using FluentValidation;
using VirtoCommerce.ProductComparison.Xapi.Aggregates;

namespace VirtoCommerce.ProductComparison.Xapi.Commands;

public class SaveProductsToCompareListCommandHandler(ICompareListAggregateRepository repository, AbstractValidator<SaveProductsToCompareListCommand> validator)
    : CompareListCommandHandler<SaveProductsToCompareListCommand>(repository, validator)
{
    protected override bool CreatesList => true;

    protected override void Update(CompareListAggregate aggregate, SaveProductsToCompareListCommand request)
    {
        aggregate.SaveProducts(request.Products);
    }
}
