using FluentValidation;
using VirtoCommerce.ProductComparison.Core;
using VirtoCommerce.ProductComparison.Xapi.Queries;

namespace VirtoCommerce.ProductComparison.Xapi.Validators;

public class GetCompareListQueryValidator : AbstractValidator<GetCompareListQuery>
{
    public GetCompareListQueryValidator()
    {
        RuleFor(x => x.StoreId).NotEmpty().MaximumLength(ModuleConstants.IdLength);
        RuleFor(x => x.UserId).NotEmpty().MaximumLength(ModuleConstants.IdLength);
    }
}
