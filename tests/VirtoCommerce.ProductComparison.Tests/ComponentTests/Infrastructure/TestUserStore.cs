using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using VirtoCommerce.Platform.Core.Security;

namespace VirtoCommerce.ProductComparison.Tests.ComponentTests.Infrastructure;

/// <summary>
/// The store under the harness's <c>UserManager</c>: lookups by id (the guest rule and the account-state check)
/// and the lockout state (the account-state check). Writes are not the module's business, so they throw.
/// </summary>
internal sealed class TestUserStore(TestUsers users) : IUserLockoutStore<ApplicationUser>
{
    public Task<ApplicationUser> FindByIdAsync(string userId, CancellationToken cancellationToken)
    {
        return Task.FromResult(users.Find(userId));
    }

    public Task<bool> GetLockoutEnabledAsync(ApplicationUser user, CancellationToken cancellationToken)
    {
        return Task.FromResult(user.LockoutEnabled);
    }

    public Task<DateTimeOffset?> GetLockoutEndDateAsync(ApplicationUser user, CancellationToken cancellationToken)
    {
        return Task.FromResult(user.LockoutEnd);
    }

    public Task<string> GetUserIdAsync(ApplicationUser user, CancellationToken cancellationToken)
    {
        return Task.FromResult(user.Id);
    }

    public Task<string> GetUserNameAsync(ApplicationUser user, CancellationToken cancellationToken)
    {
        return Task.FromResult(user.UserName);
    }

    public Task<ApplicationUser> FindByNameAsync(string normalizedUserName, CancellationToken cancellationToken) => throw new NotSupportedException();

    public Task SetUserNameAsync(ApplicationUser user, string userName, CancellationToken cancellationToken) => throw new NotSupportedException();

    public Task<string> GetNormalizedUserNameAsync(ApplicationUser user, CancellationToken cancellationToken) => throw new NotSupportedException();

    public Task SetNormalizedUserNameAsync(ApplicationUser user, string normalizedName, CancellationToken cancellationToken) => throw new NotSupportedException();

    public Task<IdentityResult> CreateAsync(ApplicationUser user, CancellationToken cancellationToken) => throw new NotSupportedException();

    public Task<IdentityResult> UpdateAsync(ApplicationUser user, CancellationToken cancellationToken) => throw new NotSupportedException();

    public Task<IdentityResult> DeleteAsync(ApplicationUser user, CancellationToken cancellationToken) => throw new NotSupportedException();

    public Task SetLockoutEndDateAsync(ApplicationUser user, DateTimeOffset? lockoutEnd, CancellationToken cancellationToken) => throw new NotSupportedException();

    public Task<int> IncrementAccessFailedCountAsync(ApplicationUser user, CancellationToken cancellationToken) => throw new NotSupportedException();

    public Task ResetAccessFailedCountAsync(ApplicationUser user, CancellationToken cancellationToken) => throw new NotSupportedException();

    public Task<int> GetAccessFailedCountAsync(ApplicationUser user, CancellationToken cancellationToken) => throw new NotSupportedException();

    public Task SetLockoutEnabledAsync(ApplicationUser user, bool enabled, CancellationToken cancellationToken) => throw new NotSupportedException();

    // A UserManager disposes its store; this one is shared by every manager the harness hands out.
    public void Dispose()
    {
    }
}
