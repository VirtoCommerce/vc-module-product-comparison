using VirtoCommerce.ProductComparison.Xapi.Aggregates;

namespace VirtoCommerce.ProductComparison.Xapi.Commands;

public class SaveProductsToCompareListCommandHandler(ICompareListAggregateRepository repository)
    : CompareListCommandHandler<SaveProductsToCompareListCommand>(repository)
{
    protected override bool CreatesList => true;

    protected override void Update(CompareListAggregate aggregate, SaveProductsToCompareListCommand request)
    {
        aggregate.SaveProducts(request.Products);
    }
}
