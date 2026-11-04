using FluentValidation;
using VirtoCommerce.Platform.Data.Infrastructure;
using VirtoCommerce.ProductComparison.Xapi.Commands;

namespace VirtoCommerce.ProductComparison.Xapi.Validators;

public class ClearCompareListCategoryCommandValidator : CompareListRequestValidator<ClearCompareListCategoryCommand>
{
    public ClearCompareListCategoryCommandValidator()
    {
        RuleFor(x => x.CategoryId).MaximumLength(DbContextBase.IdLength);
    }
}
