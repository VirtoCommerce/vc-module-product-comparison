using System;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using VirtoCommerce.Platform.Core;
using VirtoCommerce.Platform.Core.Security;
using VirtoCommerce.ProductComparison.Core.Models;
using VirtoCommerce.ProductComparison.Xapi.Authorization;
using Xunit;

namespace VirtoCommerce.ProductComparison.Tests.UnitTests;

/// <summary>
/// Who may use a compare list (VCST-5918), the guest-cart rule: a signed-in buyer only their own user id, a guest
/// only an id no registered user has, an administrator any. A merge source must be a guest list or the caller's own.
/// </summary>
[Trait("Category", "Unit")]
public class CanAccessCompareListAuthorizationHandlerTests
{
    private const string RegisteredUserId = "registered-user";

    public CanAccessCompareListAuthorizationHandlerTests()
    {
        // The platform sets this at startup; GetUserId() finds nothing without it.
        ClaimsPrincipalExtensions.UserIdClaimTypes = [ClaimTypes.NameIdentifier];
    }

    [Fact]
    public async Task SignedInBuyer_MayUseOnlyTheirOwnUserId()
    {
        var buyer = SignedIn("buyer-1");

        (await AuthorizeAsync(buyer, "buyer-1")).Should().BeTrue();
        (await AuthorizeAsync(buyer, "BUYER-1")).Should().BeTrue();
        (await AuthorizeAsync(buyer, "buyer-2")).Should().BeFalse();
        (await AuthorizeAsync(buyer, RegisteredUserId)).Should().BeFalse();
    }

    [Fact]
    public async Task Guest_MayUseAnIdNoRegisteredUserHas()
    {
        (await AuthorizeAsync(Anonymous(), "2cd0bf46-543c-4857-af38-db1ca6072120")).Should().BeTrue();
    }

    [Fact]
    public async Task Guest_MayNotUseARegisteredUsersId()
    {
        // Otherwise anyone who knows a buyer's id could read and edit that buyer's list without signing in.
        (await AuthorizeAsync(Anonymous(), RegisteredUserId)).Should().BeFalse();
    }

    [Fact]
    public async Task EmptyUserId_IsRefused()
    {
        // An empty id matches no registered user, so the guest rule alone would let it through.
        (await AuthorizeAsync(Anonymous(), string.Empty)).Should().BeFalse();
        (await AuthorizeAsync(Anonymous(), null)).Should().BeFalse();
        (await AuthorizeAsync(SignedIn("buyer-1"), string.Empty)).Should().BeFalse();
    }

    [Fact]
    public async Task Administrator_MayUseAnyUserIdAndList()
    {
        var administrator = Administrator();

        (await AuthorizeAsync(administrator, RegisteredUserId)).Should().BeTrue();
        (await AuthorizeAsync(administrator, List(userId: "someone", isAnonymous: false))).Should().BeTrue();
    }

    [Fact]
    public async Task MergeSource_GuestListOrOwnList_Passes()
    {
        var buyer = SignedIn("buyer-1");

        (await AuthorizeAsync(buyer, List(userId: "some-guest", isAnonymous: true))).Should().BeTrue();
        (await AuthorizeAsync(buyer, List(userId: "buyer-1", isAnonymous: false))).Should().BeTrue();
    }

    [Fact]
    public async Task MergeSource_AnotherBuyersList_IsRefused()
    {
        (await AuthorizeAsync(SignedIn("buyer-1"), List(userId: "buyer-2", isAnonymous: false))).Should().BeFalse();
    }

    [Fact]
    public async Task MergeSource_ForAGuest_IsRefused()
    {
        // Only a signed-in buyer takes a guest list over; the builder refuses anonymous merges before this runs.
        (await AuthorizeAsync(Anonymous(), List(userId: "some-guest", isAnonymous: true))).Should().BeFalse();
    }

    private static async Task<bool> AuthorizeAsync(ClaimsPrincipal user, object resource)
    {
        var requirement = new CanAccessCompareListAuthorizationRequirement();
        var handler = new CanAccessCompareListAuthorizationHandler(() => CreateUserManager(RegisteredUserId));
        var context = new AuthorizationHandlerContext([requirement], user, resource);

        await handler.HandleAsync(context);

        return context.HasSucceeded;
    }

    private static ClaimsPrincipal SignedIn(string userId)
    {
        return new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, userId)], authenticationType: "Test"));
    }

    private static ClaimsPrincipal Anonymous()
    {
        return new ClaimsPrincipal(new ClaimsIdentity());
    }

    private static ClaimsPrincipal Administrator()
    {
        var identity = new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, "admin")], authenticationType: "Test");
        identity.AddClaim(new Claim(identity.RoleClaimType, PlatformConstants.Security.SystemRoles.Administrator));

        return new ClaimsPrincipal(identity);
    }

    private static CompareList List(string userId, bool isAnonymous)
    {
        return new CompareList { Id = "list", StoreId = "store", UserId = userId, IsAnonymous = isAnonymous };
    }

    private static UserManager<ApplicationUser> CreateUserManager(params string[] registeredUserIds)
    {
        return new UserManager<ApplicationUser>(new UserStore(registeredUserIds), null, null, null, null, null, null, null, null);
    }

    /// <summary>Only FindByIdAsync is used: the guest rule asks whether a registered user has the id.</summary>
    private sealed class UserStore(string[] userIds) : IUserStore<ApplicationUser>
    {
        public Task<ApplicationUser> FindByIdAsync(string userId, CancellationToken cancellationToken)
        {
            return Task.FromResult(userIds.Contains(userId) ? new ApplicationUser { Id = userId } : null);
        }

        public Task<ApplicationUser> FindByNameAsync(string normalizedUserName, CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<string> GetUserIdAsync(ApplicationUser user, CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<string> GetUserNameAsync(ApplicationUser user, CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task SetUserNameAsync(ApplicationUser user, string userName, CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<string> GetNormalizedUserNameAsync(ApplicationUser user, CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task SetNormalizedUserNameAsync(ApplicationUser user, string normalizedName, CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<IdentityResult> CreateAsync(ApplicationUser user, CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<IdentityResult> UpdateAsync(ApplicationUser user, CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<IdentityResult> DeleteAsync(ApplicationUser user, CancellationToken cancellationToken) => throw new NotSupportedException();

        public void Dispose()
        {
        }
    }
}
