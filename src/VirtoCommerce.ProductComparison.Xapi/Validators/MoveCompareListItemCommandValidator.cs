using FluentValidation;
using VirtoCommerce.Platform.Data.Infrastructure;
using VirtoCommerce.ProductComparison.Xapi.Commands;

namespace VirtoCommerce.ProductComparison.Xapi.Validators;

public class MoveCompareListItemCommandValidator : CompareListRequestValidator<MoveCompareListItemCommand>
{
    public MoveCompareListItemCommandValidator()
    {
        RuleFor(x => x.ItemId).NotEmpty().MaximumLength(DbContextBase.IdLength);
        RuleFor(x => x.Position).GreaterThanOrEqualTo(0);
    }
}
