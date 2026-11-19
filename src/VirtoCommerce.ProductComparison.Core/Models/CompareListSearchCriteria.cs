using System;
using VirtoCommerce.Platform.Core.Common;

namespace VirtoCommerce.ProductComparison.Core.Models;

public class CompareListSearchCriteria : SearchCriteriaBase
{
    public string StoreId { get; set; }

    public string UserId { get; set; }

    public string OrganizationId { get; set; }

    public bool OrganizationIdIsEmpty { get; set; }

    public bool? IsAnonymous { get; set; }

    public DateTime? ModifiedBefore { get; set; }
}
