using GraphQL.Types;
using VirtoCommerce.ProductComparison.Xapi.Commands;

namespace VirtoCommerce.ProductComparison.Xapi.Schemas;

public class InputRemoveCompareListItemsType : InputCompareListCommandType<RemoveCompareListItemsCommand>
{
    public InputRemoveCompareListItemsType()
    {
        Field<NonNullGraphType<ListGraphType<NonNullGraphType<StringGraphType>>>>("itemIds")
            .Description("IDs of the compare list items to remove");
    }
}
