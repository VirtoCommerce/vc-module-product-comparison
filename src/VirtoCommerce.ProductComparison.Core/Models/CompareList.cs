using System;
using System.Collections.Generic;
using System.Linq;
using VirtoCommerce.Platform.Core.Common;

namespace VirtoCommerce.ProductComparison.Core.Models;

public class CompareList : AuditableEntity, ICloneable
{
    public string StoreId { get; set; }

    public string UserId { get; set; }

    public string OrganizationId { get; set; }

    public bool IsAnonymous { get; set; }

    public IList<CompareListItem> Items { get; set; } = [];

    public virtual object Clone()
    {
        var result = (CompareList)MemberwiseClone();
        result.Items = Items?.Select(x => x.CloneTyped()).ToList();
        return result;
    }
}
