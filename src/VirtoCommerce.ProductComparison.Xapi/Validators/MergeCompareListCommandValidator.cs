using FluentValidation;
using VirtoCommerce.Platform.Data.Infrastructure;
using VirtoCommerce.ProductComparison.Xapi.Commands;

namespace VirtoCommerce.ProductComparison.Xapi.Validators;

public class MergeCompareListCommandValidator : CompareListRequestValidator<MergeCompareListCommand>
{
    public MergeCompareListCommandValidator()
    {
        RuleFor(x => x.SecondListId).NotEmpty().MaximumLength(DbContextBase.IdLength);
    }
}
