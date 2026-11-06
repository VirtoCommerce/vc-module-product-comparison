namespace VirtoCommerce.ProductComparison.Xapi.Models;

public interface ICompareListRequest
{
    string StoreId { get; }

    string UserId { get; set; }

    string CultureName { get; }

    string CurrencyCode { get; }

    string OrganizationId { get; set; }

    bool IsAnonymous { get; set; }
}
