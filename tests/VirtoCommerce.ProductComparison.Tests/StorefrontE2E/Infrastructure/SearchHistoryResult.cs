using System.Collections.Generic;

namespace VirtoCommerce.ProductComparison.Tests.StorefrontE2E.Infrastructure;

internal sealed class SearchHistoryResult
{
    public IList<string> Queries { get; set; } = [];
}
