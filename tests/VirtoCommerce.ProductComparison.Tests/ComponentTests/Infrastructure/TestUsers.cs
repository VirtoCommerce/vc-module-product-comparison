using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using MockQueryable;
using VirtoCommerce.Platform.Core.Security;

namespace VirtoCommerce.ProductComparison.Tests.ComponentTests.Infrastructure;

/// <summary>
/// The platform's accounts, as the module reads them: the guest rule asks whether an id belongs to a registered
/// user, the account-state check whether the caller is locked out, and the cleanup whether a list's user still
/// exists and which contact it has. One registry answers all three, through a real <c>UserManager</c> over
/// <see cref="TestUserStore"/>.
/// </summary>
internal sealed class TestUsers
{
    private readonly ConcurrentDictionary<string, ApplicationUser> _users = new(StringComparer.Ordinal);
    private int _queries;

    /// <summary>How many times <c>UserManager.Users</c> was queried, for asserting the cleanup's batching.</summary>
    public int Queries => _queries;

    public ApplicationUser Add(string userId, string memberId = null)
    {
        var user = new ApplicationUser
        {
            Id = userId,
            UserName = $"{userId}@test.com",
            Email = $"{userId}@test.com",
            MemberId = memberId,
            UserType = "Customer",
            LockoutEnabled = true,
        };

        _users[userId] = user;

        return user;
    }

    /// <summary>The account is deleted; its compare lists stay behind.</summary>
    public void Remove(string userId)
    {
        _users.TryRemove(userId, out _);
    }

    /// <summary>Locked as the admin "Block" action locks it: LockoutEnd in the future.</summary>
    public void LockOut(string userId)
    {
        _users[userId].LockoutEnd = DateTimeOffset.UtcNow.AddYears(1);
    }

    public ApplicationUser Find(string userId)
    {
        return userId != null && _users.TryGetValue(userId, out var user) ? user : null;
    }

    /// <summary>
    /// <c>UserManager.Users</c>: a snapshot of the accounts that runs EF Core's async operators in memory, as the
    /// cleanup's id and contact lookup sends them.
    /// </summary>
    public IQueryable<ApplicationUser> Query()
    {
        Interlocked.Increment(ref _queries);

        return _users.Values.ToList().BuildMock();
    }
}
