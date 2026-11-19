using VirtoCommerce.ProductComparison.Xapi.Commands;

namespace VirtoCommerce.ProductComparison.Xapi.Schemas;

public class InputMergeCompareListType : InputCompareListCommandType<MergeCompareListCommand>
{
    public InputMergeCompareListType()
    {
        Field(x => x.SecondListId, nullable: false).Description("ID of the guest's compare list to merge into this one");
    }
}
