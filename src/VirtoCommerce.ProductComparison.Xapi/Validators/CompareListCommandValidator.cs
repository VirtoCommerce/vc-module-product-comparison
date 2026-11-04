using FluentValidation;
using VirtoCommerce.ProductComparison.Core;
using VirtoCommerce.ProductComparison.Xapi.Commands;

namespace VirtoCommerce.ProductComparison.Xapi.Validators;

public class CompareListCommandValidator<TCommand> : AbstractValidator<TCommand>
    where TCommand : CompareListCommand
{
    public CompareListCommandValidator()
    {
        RuleFor(x => x.StoreId).NotEmpty().MaximumLength(ModuleConstants.IdLength);
        RuleFor(x => x.UserId).NotEmpty().MaximumLength(ModuleConstants.IdLength);
    }
}
