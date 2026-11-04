using FluentValidation;
using VirtoCommerce.ProductComparison.Core;
using VirtoCommerce.ProductComparison.Xapi.Commands;

namespace VirtoCommerce.ProductComparison.Xapi.Validators;

public class RemoveCompareListItemsCommandValidator : CompareListCommandValidator<RemoveCompareListItemsCommand>
{
    public RemoveCompareListItemsCommandValidator()
    {
        RuleFor(x => x.ItemIds).NotEmpty();
        RuleForEach(x => x.ItemIds).NotEmpty().MaximumLength(ModuleConstants.IdLength);
    }
}
