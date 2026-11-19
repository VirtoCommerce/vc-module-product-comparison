using Microsoft.AspNetCore.Authorization;
using VirtoCommerce.ProductComparison.Xapi.Schemas;

namespace VirtoCommerce.ProductComparison.Xapi.Commands;

public class ClearCompareListCommandBuilder(IAuthorizationService authorizationService)
    : CompareListCommandBuilder<ClearCompareListCommand, InputClearCompareListType>(authorizationService)
{
    protected override string Name => "clearCompareList";
}
