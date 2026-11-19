using System.Collections.Generic;
using VirtoCommerce.ProductComparison.Core.Models;

namespace VirtoCommerce.ProductComparison.Xapi.Models;

public class CompareListCategory
{
    public string CategoryId { get; set; }

    public IList<CompareListItem> Items { get; set; } = [];
}
