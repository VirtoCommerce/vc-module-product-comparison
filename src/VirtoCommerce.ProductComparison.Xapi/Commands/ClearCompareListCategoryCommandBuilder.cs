using Microsoft.AspNetCore.Authorization;
using VirtoCommerce.ProductComparison.Xapi.Schemas;

namespace VirtoCommerce.ProductComparison.Xapi.Commands;

public class ClearCompareListCategoryCommandBuilder(IAuthorizationService authorizationService)
    : CompareListCommandBuilder<ClearCompareListCategoryCommand, InputClearCompareListCategoryType>(authorizationService)
{
    protected override string Name => "clearCompareListCategory";
}
