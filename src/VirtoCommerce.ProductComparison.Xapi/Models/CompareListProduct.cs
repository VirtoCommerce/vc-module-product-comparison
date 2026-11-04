using System.Collections.Generic;
using VirtoCommerce.XCart.Core.Models;

namespace VirtoCommerce.ProductComparison.Xapi.Models;

public class CompareListProduct
{
    public string ProductId { get; set; }

    public string CategoryId { get; set; }

    public IList<ProductConfigurationSection> ConfigurationSections { get; set; }
}
