using FluentValidation;
using VirtoCommerce.Platform.Data.Infrastructure;
using VirtoCommerce.ProductComparison.Core;
using VirtoCommerce.ProductComparison.Xapi.Commands;

namespace VirtoCommerce.ProductComparison.Xapi.Validators;

public class RemoveCompareListItemsCommandValidator : CompareListRequestValidator<RemoveCompareListItemsCommand>
{
    public RemoveCompareListItemsCommandValidator()
    {
        RuleFor(x => x.ItemIds)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .Must(itemIds => itemIds.Count <= ModuleConstants.MaxListItems)
            .WithMessage($"A call can remove at most {ModuleConstants.MaxListItems} items");
        RuleForEach(x => x.ItemIds).NotEmpty().MaximumLength(DbContextBase.IdLength);
    }
}
