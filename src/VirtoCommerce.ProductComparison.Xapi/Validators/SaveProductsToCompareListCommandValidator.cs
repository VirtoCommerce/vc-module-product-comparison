using FluentValidation;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.ProductComparison.Xapi.Commands;

namespace VirtoCommerce.ProductComparison.Xapi.Validators;

public class SaveProductsToCompareListCommandValidator : CompareListRequestValidator<SaveProductsToCompareListCommand>
{
    public SaveProductsToCompareListCommandValidator()
    {
        RuleFor(x => x.Products).NotEmpty();
        RuleForEach(x => x.Products).SetValidator(AbstractTypeFactory<CompareListProductValidator>.TryCreateInstance());
    }
}
