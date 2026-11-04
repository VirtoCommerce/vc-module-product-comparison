using FluentValidation;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.ProductComparison.Core;
using VirtoCommerce.ProductComparison.Xapi.Commands;

namespace VirtoCommerce.ProductComparison.Xapi.Validators;

public class SaveProductsToCompareListCommandValidator : CompareListRequestValidator<SaveProductsToCompareListCommand>
{
    public SaveProductsToCompareListCommandValidator()
    {
        RuleFor(x => x.Products)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .Must(products => products.Count <= ModuleConstants.MaxListItems)
            .WithMessage($"A call can save at most {ModuleConstants.MaxListItems} products");
        RuleForEach(x => x.Products).SetValidator(AbstractTypeFactory<CompareListProductValidator>.TryCreateInstance());
    }
}
