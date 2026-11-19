using VirtoCommerce.ProductComparison.Xapi.Commands;
using VirtoCommerce.Xapi.Core.Schemas;

namespace VirtoCommerce.ProductComparison.Xapi.Schemas;

public abstract class InputCompareListCommandType<TCommand> : ExtendableInputObjectGraphType<TCommand>
    where TCommand : CompareListCommand
{
    protected InputCompareListCommandType()
    {
        Field(x => x.StoreId, nullable: false).Description("Store ID");
        Field(x => x.UserId, nullable: false).Description("The signed-in user's ID, or the guest's anonymous ID");
        Field(x => x.CultureName, nullable: true).Description("Culture name (\"en-US\") for the returned products");
        Field(x => x.CurrencyCode, nullable: true).Description("Currency code (\"USD\") for the returned products");
    }
}
