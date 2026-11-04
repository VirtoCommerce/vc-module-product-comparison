using System.Collections.Generic;

namespace VirtoCommerce.ProductComparison.Xapi.Commands;

public class RemoveCompareListItemsCommand : CompareListCommand
{
    public IList<string> ItemIds { get; set; } = [];
}
