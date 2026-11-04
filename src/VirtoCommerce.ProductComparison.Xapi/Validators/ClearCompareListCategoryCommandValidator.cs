using FluentValidation;
using VirtoCommerce.ProductComparison.Core;
using VirtoCommerce.ProductComparison.Xapi.Commands;

namespace VirtoCommerce.ProductComparison.Xapi.Validators;

public class ClearCompareListCategoryCommandValidator : CompareListRequestValidator<ClearCompareListCategoryCommand>
{
    public ClearCompareListCategoryCommandValidator()
    {
        RuleFor(x => x.CategoryId).MaximumLength(ModuleConstants.IdLength);
    }
}
