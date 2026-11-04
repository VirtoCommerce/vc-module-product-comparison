using VirtoCommerce.Platform.Security.Authorization;

namespace VirtoCommerce.ProductComparison.Xapi.Authorization;

public class CanAccessCompareListAuthorizationRequirement : PermissionAuthorizationRequirement
{
    public CanAccessCompareListAuthorizationRequirement()
        : base("CanAccessCompareList")
    {
    }
}
