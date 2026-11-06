using System.Collections.Generic;
using VirtoCommerce.ProductComparison.Xapi.Models;

namespace VirtoCommerce.ProductComparison.Xapi.Commands;

public class SaveProductsToCompareListCommand : CompareListCommand
{
    public IList<CompareListProduct> Products { get; set; } = [];
}
