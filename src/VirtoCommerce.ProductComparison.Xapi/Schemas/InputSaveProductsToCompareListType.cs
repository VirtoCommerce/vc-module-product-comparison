using GraphQL.Types;
using VirtoCommerce.ProductComparison.Xapi.Commands;

namespace VirtoCommerce.ProductComparison.Xapi.Schemas;

public class InputSaveProductsToCompareListType : InputCompareListCommandType<SaveProductsToCompareListCommand>
{
    public InputSaveProductsToCompareListType()
    {
        Field<NonNullGraphType<ListGraphType<NonNullGraphType<InputCompareListProductType>>>>("products")
            .Description("Products to compare; a product already in the list with the same configuration stays where it is");
    }
}
