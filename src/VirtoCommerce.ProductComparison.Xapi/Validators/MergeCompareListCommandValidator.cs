using FluentValidation;
using VirtoCommerce.ProductComparison.Core;
using VirtoCommerce.ProductComparison.Xapi.Commands;

namespace VirtoCommerce.ProductComparison.Xapi.Validators;

public class MergeCompareListCommandValidator : CompareListRequestValidator<MergeCompareListCommand>
{
    public MergeCompareListCommandValidator()
    {
        RuleFor(x => x.SecondListId).NotEmpty().MaximumLength(ModuleConstants.IdLength);
    }
}
