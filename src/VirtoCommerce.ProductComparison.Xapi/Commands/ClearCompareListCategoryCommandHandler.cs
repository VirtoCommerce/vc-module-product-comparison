using VirtoCommerce.ProductComparison.Xapi.Aggregates;

namespace VirtoCommerce.ProductComparison.Xapi.Commands;

public class ClearCompareListCategoryCommandHandler(ICompareListAggregateRepository repository)
    : CompareListCommandHandler<ClearCompareListCategoryCommand>(repository)
{
    protected override void Update(CompareListAggregate aggregate, ClearCompareListCategoryCommand request)
    {
        aggregate.ClearCategory(request.CategoryId);
    }
}
