using System;
using System.Collections.Generic;
using System.Linq;
using VirtoCommerce.Platform.Core.Common;

namespace VirtoCommerce.ProductComparison.Core.Models;

public class CompareListItem : AuditableEntity, ICloneable
{
    public string ProductId { get; set; }

    public string CategoryId { get; set; }

    public string ConfigurationKey { get; set; }

    public int Position { get; set; }

    public IList<CompareListItemSection> Sections { get; set; } = [];

    public virtual object Clone()
    {
        var result = (CompareListItem)MemberwiseClone();
        result.Sections = Sections?.Select(x => x.CloneTyped()).ToList();
        return result;
    }
}
