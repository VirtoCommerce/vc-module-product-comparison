using Microsoft.AspNetCore.Authorization;
using VirtoCommerce.ProductComparison.Xapi.Schemas;

namespace VirtoCommerce.ProductComparison.Xapi.Commands;

public class RemoveCompareListItemsCommandBuilder(IAuthorizationService authorizationService)
    : CompareListCommandBuilder<RemoveCompareListItemsCommand, InputRemoveCompareListItemsType>(authorizationService)
{
    protected override string Name => "removeCompareListItems";
}
