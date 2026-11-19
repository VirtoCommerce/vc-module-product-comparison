using System;
using VirtoCommerce.Platform.Core.Common;

namespace VirtoCommerce.ProductComparison.Core.Models;

public class CompareListItemSection : Entity, ICloneable
{
    public string SectionId { get; set; }

    public string Type { get; set; }

    public string ProductId { get; set; }

    public int? Quantity { get; set; }

    public string CustomText { get; set; }

    public virtual object Clone()
    {
        return MemberwiseClone();
    }
}
