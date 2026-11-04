using Microsoft.AspNetCore.Authorization;
using VirtoCommerce.ProductComparison.Xapi.Schemas;

namespace VirtoCommerce.ProductComparison.Xapi.Commands;

public class SaveProductsToCompareListCommandBuilder(IAuthorizationService authorizationService)
    : CompareListCommandBuilder<SaveProductsToCompareListCommand, InputSaveProductsToCompareListType>(authorizationService)
{
    protected override string Name => "saveProductsToCompareList";
}
