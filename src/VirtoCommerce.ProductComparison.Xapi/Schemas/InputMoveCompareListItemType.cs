using VirtoCommerce.ProductComparison.Xapi.Commands;

namespace VirtoCommerce.ProductComparison.Xapi.Schemas;

public class InputMoveCompareListItemType : InputCompareListCommandType<MoveCompareListItemCommand>
{
    public InputMoveCompareListItemType()
    {
        Field(x => x.ItemId, nullable: false).Description("ID of the compare list item to move");
        Field(x => x.Position, nullable: false).Description("New position within the item's category, from 0");
    }
}
