using Microsoft.AspNetCore.Authorization;
using VirtoCommerce.ProductComparison.Xapi.Schemas;

namespace VirtoCommerce.ProductComparison.Xapi.Commands;

public class MoveCompareListItemCommandBuilder(IAuthorizationService authorizationService)
    : CompareListCommandBuilder<MoveCompareListItemCommand, InputMoveCompareListItemType>(authorizationService)
{
    protected override string Name => "moveCompareListItem";
}
