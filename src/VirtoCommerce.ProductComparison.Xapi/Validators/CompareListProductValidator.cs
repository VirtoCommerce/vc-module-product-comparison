using FluentValidation;
using VirtoCommerce.ProductComparison.Core;
using VirtoCommerce.ProductComparison.Xapi.Models;
using CatalogModuleConstants = VirtoCommerce.CatalogModule.Core.ModuleConstants;

namespace VirtoCommerce.ProductComparison.Xapi.Validators;

public class CompareListProductValidator : AbstractValidator<CompareListProduct>
{
    public CompareListProductValidator()
    {
        RuleFor(x => x.ProductId).NotEmpty().MaximumLength(ModuleConstants.IdLength);
        RuleFor(x => x.CategoryId).MaximumLength(ModuleConstants.IdLength);

        RuleForEach(x => x.ConfigurationSections).ChildRules(section =>
        {
            section.RuleFor(x => x.SectionId).NotEmpty().MaximumLength(ModuleConstants.IdLength);
            section.RuleFor(x => x.Type)
                .Must(type => type is CatalogModuleConstants.ConfigurationSectionTypeProduct or CatalogModuleConstants.ConfigurationSectionTypeText)
                .WithMessage("Only Product and Text sections can be compared");
            section.RuleFor(x => x.Option.ProductId)
                .NotEmpty()
                .MaximumLength(ModuleConstants.IdLength)
                .When(x => x.Option != null);
            section.RuleFor(x => x.CustomText).MaximumLength(ModuleConstants.CustomTextLength);
        });
    }
}
