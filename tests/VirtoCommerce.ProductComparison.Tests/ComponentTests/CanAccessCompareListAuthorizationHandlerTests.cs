using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using VirtoCommerce.Platform.Core;
using VirtoCommerce.ProductComparison.Core.Models;
using VirtoCommerce.ProductComparison.Tests.ComponentTests.Infrastructure;
using VirtoCommerce.ProductComparison.Xapi.Authorization;
using Xunit;

namespace VirtoCommerce.ProductComparison.Tests.ComponentTests;

/// <summary>
/// Who may use a compare list (VCST-5918), the guest-cart rule: a signed-in buyer only their own user id, a guest
/// only an id no registered user has, an administrator any. A merge source must be a guest list or the caller's own.
/// The handler is the one the module registers, and the registered user an account of the platform's security
/// database (<see cref="ProductComparisonTestContext"/>).
/// </summary>
[Trait("Category", "Component")]
public class CanAccessCompareListAuthorizationHandlerTests : IAsyncLifetime
{
    private const string RegisteredUserId = "registered-user";

    private ProductComparisonTestContext _ctx;

    public async ValueTask InitializeAsync()
    {
        _ctx = await ProductComparisonTestContext.CreateAsync();
        await _ctx.AddUserAsync(RegisteredUserId);
    }

    public ValueTask DisposeAsync() => _ctx.DisposeAsync();

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

    private async Task<bool> AuthorizeAsync(ClaimsPrincipal user, object resource)
    {
        // The handler as the module registers it, over the platform's accounts.
        var handler = _ctx.GetRequiredService<IEnumerable<IAuthorizationHandler>>().OfType<CanAccessCompareListAuthorizationHandler>().Single();
        var context = new AuthorizationHandlerContext([new CanAccessCompareListAuthorizationRequirement()], user, resource);

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
}
