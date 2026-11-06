using VirtoCommerce.ProductComparison.Xapi.Commands;

namespace VirtoCommerce.ProductComparison.Xapi.Schemas;

public class InputClearCompareListCategoryType : InputCompareListCommandType<ClearCompareListCategoryCommand>
{
    public InputClearCompareListCategoryType()
    {
        Field(x => x.CategoryId, nullable: true).Description("Category ID as the compare list returns it; null for products without a category");
    }
}
