using FluentValidation;
using VirtoCommerce.Platform.Data.Infrastructure;
using VirtoCommerce.ProductComparison.Xapi.Commands;

namespace VirtoCommerce.ProductComparison.Xapi.Validators;

public class RemoveCompareListItemsCommandValidator : CompareListRequestValidator<RemoveCompareListItemsCommand>
{
    public RemoveCompareListItemsCommandValidator()
    {
        RuleFor(x => x.ItemIds).NotEmpty();
        RuleForEach(x => x.ItemIds).NotEmpty().MaximumLength(DbContextBase.IdLength);
    }
}
