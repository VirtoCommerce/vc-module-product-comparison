using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using VirtoCommerce.Platform.Core;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Core.Security;
using VirtoCommerce.Platform.Security.Authorization;
using VirtoCommerce.ProductComparison.Core.Models;

namespace VirtoCommerce.ProductComparison.Xapi.Authorization;

public class CanAccessCompareListAuthorizationHandler(Func<UserManager<ApplicationUser>> userManagerFactory)
    : PermissionAuthorizationHandlerBase<CanAccessCompareListAuthorizationRequirement>
{
    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, CanAccessCompareListAuthorizationRequirement requirement)
    {
        var authorized = context.User.IsInRole(PlatformConstants.Security.SystemRoles.Administrator);

        if (!authorized)
        {
            var isAuthenticated = context.User.Identity?.IsAuthenticated == true;

            switch (context.Resource)
            {
                // Fails closed: an empty id would match no user and pass the guest rule below.
                case string userId when string.IsNullOrEmpty(userId):
                    break;
                case string userId when isAuthenticated:
                    authorized = userId.EqualsIgnoreCase(context.User.GetUserId());
                    break;
                case string userId:
                    // A guest may use only an id that no registered user has.
                    using (var userManager = userManagerFactory())
                    {
                        authorized = await userManager.FindByIdAsync(userId) == null;
                    }
                    break;
                // The source of a merge: a guest list, or the caller's own (the rule VCST-6185 gave mergeCart).
                case CompareList list when isAuthenticated:
                    authorized = list.IsAnonymous || list.UserId.EqualsIgnoreCase(context.User.GetUserId());
                    break;
            }
        }

        if (authorized)
        {
            context.Succeed(requirement);
        }
        else
        {
            context.Fail();
        }
    }
}
