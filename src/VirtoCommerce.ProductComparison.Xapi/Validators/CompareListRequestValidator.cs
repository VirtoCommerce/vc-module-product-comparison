using FluentValidation;
using VirtoCommerce.Platform.Data.Infrastructure;
using VirtoCommerce.ProductComparison.Xapi.Models;

namespace VirtoCommerce.ProductComparison.Xapi.Validators;

public class CompareListRequestValidator<TRequest> : AbstractValidator<TRequest>
    where TRequest : ICompareListRequest
{
    public CompareListRequestValidator()
    {
        RuleFor(x => x.StoreId).NotEmpty().MaximumLength(DbContextBase.IdLength);
        RuleFor(x => x.UserId).NotEmpty().MaximumLength(DbContextBase.IdLength);
    }
}
