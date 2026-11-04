using FluentValidation;
using VirtoCommerce.ProductComparison.Core;
using VirtoCommerce.ProductComparison.Xapi.Commands;

namespace VirtoCommerce.ProductComparison.Xapi.Validators;

public class MoveCompareListItemCommandValidator : CompareListRequestValidator<MoveCompareListItemCommand>
{
    public MoveCompareListItemCommandValidator()
    {
        RuleFor(x => x.ItemId).NotEmpty().MaximumLength(ModuleConstants.IdLength);
        RuleFor(x => x.Position).GreaterThanOrEqualTo(0);
    }
}
