using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Core.Security;
using VirtoCommerce.Platform.Core.Security.Search;

namespace VirtoCommerce.ProductComparison.Tests.ComponentTests.Infrastructure;

/// <summary>
/// The platform's accounts, as the module reads them: the guest rule asks whether an id belongs to a registered
/// user, the account-state check whether the caller is locked out, and the cleanup whether a list's user still
/// exists and which contact it has. One registry answers all three, through a real <c>UserManager</c> over
/// <see cref="TestUserStore"/> and through this <see cref="IUserSearchService"/>.
/// </summary>
internal sealed class TestUsers : IUserSearchService
{
    private readonly ConcurrentDictionary<string, ApplicationUser> _users = new(StringComparer.Ordinal);

    /// <summary>The user ids of every search the cleanup ran, one entry per call, for asserting its batching.</summary>
    public ConcurrentQueue<IList<string>> Searches { get; } = new();

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

    /// <summary>Only what the cleanup sends: user ids, and a page of them.</summary>
    public Task<UserSearchResult> SearchUsersAsync(UserSearchCriteria criteria)
    {
        Searches.Enqueue(criteria.ObjectIds?.ToList() ?? []);

        var users = _users.Values
            .Where(x => criteria.ObjectIds.IsNullOrEmpty() || criteria.ObjectIds.Contains(x.Id))
            .OrderBy(x => x.Id, StringComparer.Ordinal)
            .ToList();

        var result = new UserSearchResult
        {
            TotalCount = users.Count,
            Results = users.Skip(criteria.Skip).Take(criteria.Take).ToArray(),
        };

        return Task.FromResult(result);
    }
}
