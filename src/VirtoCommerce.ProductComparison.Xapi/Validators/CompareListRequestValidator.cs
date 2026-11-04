using FluentValidation;
using VirtoCommerce.ProductComparison.Core;
using VirtoCommerce.ProductComparison.Xapi.Models;

namespace VirtoCommerce.ProductComparison.Xapi.Validators;

public class CompareListRequestValidator<TRequest> : AbstractValidator<TRequest>
    where TRequest : ICompareListRequest
{
    public CompareListRequestValidator()
    {
        RuleFor(x => x.StoreId).NotEmpty().MaximumLength(ModuleConstants.IdLength);
        RuleFor(x => x.UserId).NotEmpty().MaximumLength(ModuleConstants.IdLength);
    }
}
