using VirtoCommerce.ProductComparison.Xapi.Aggregates;
using VirtoCommerce.ProductComparison.Xapi.Models;
using VirtoCommerce.Xapi.Core.Infrastructure;

namespace VirtoCommerce.ProductComparison.Xapi.Commands;

public abstract class CompareListCommand : ICommand<CompareListAggregate>, ICompareListRequest
{
    public string StoreId { get; set; }

    public string UserId { get; set; }

    public string CultureName { get; set; }

    public string CurrencyCode { get; set; }

    public string OrganizationId { get; set; }

    public bool IsAnonymous { get; set; }
}
