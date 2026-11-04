namespace VirtoCommerce.ProductComparison.Data.Models;

/// <summary>The columns of a list that say whose it is: all the cleanup's orphan check reads.</summary>
public class CompareListOwner
{
    public string Id { get; set; }

    public string UserId { get; set; }

    public string OrganizationId { get; set; }
}
