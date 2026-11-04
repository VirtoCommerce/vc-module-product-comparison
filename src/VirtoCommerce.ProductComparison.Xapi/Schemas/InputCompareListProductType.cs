using GraphQL.Types;
using VirtoCommerce.ProductComparison.Xapi.Models;
using VirtoCommerce.Xapi.Core.Schemas;
using VirtoCommerce.XCart.Core.Schemas;

namespace VirtoCommerce.ProductComparison.Xapi.Schemas;

public class InputCompareListProductType : ExtendableInputObjectGraphType<CompareListProduct>
{
    public InputCompareListProductType()
    {
        Field(x => x.ProductId, nullable: false).Description("Product ID");
        Field(x => x.CategoryId, nullable: true).Description("Category to compare the product in; empty for none");
        Field<ListGraphType<NonNullGraphType<ConfigurationSectionInput>>>("configurationSections")
            .Description("The chosen configuration of a configurable product: Product and Text sections");
    }
}
