using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using VirtoCommerce.CustomerModule.Core.Model;
using VirtoCommerce.CustomerModule.Core.Services;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Core.Security;
using VirtoCommerce.Platform.Core.Settings;
using VirtoCommerce.ProductComparison.Core;
using VirtoCommerce.ProductComparison.Core.Models;
using VirtoCommerce.ProductComparison.Core.Services;
using VirtoCommerce.ProductComparison.Data.Models;
using VirtoCommerce.ProductComparison.Data.Repositories;

namespace VirtoCommerce.ProductComparison.Data.BackgroundJobs;

public class CleanupCompareListsJob(
    Func<IProductComparisonRepository> repositoryFactory,
    ICompareListService compareListService,
    ICompareListSearchService compareListSearchService,
    Func<UserManager<ApplicationUser>> userManagerFactory,
    IMemberService memberService,
    ISettingsManager settingsManager,
    ILogger<CleanupCompareListsJob> logger)
{
    private const int BatchSize = 500;

    public virtual async Task Process()
    {
        var expiredCount = await DeleteExpiredAnonymousListsAsync();
        var orphanedCount = await DeleteOrphanedListsAsync();

        logger.LogInformation("Deleted {ExpiredCount} expired guest compare lists and {OrphanedCount} orphaned ones.", expiredCount, orphanedCount);
    }

    protected virtual async Task<int> DeleteExpiredAnonymousListsAsync()
    {
        var lifetimeHours = await settingsManager.GetValueAsync<int>(ModuleConstants.Settings.Cleanup.AnonymousListLifetimeHours);

        var criteria = AbstractTypeFactory<CompareListSearchCriteria>.TryCreateInstance();
        criteria.IsAnonymous = true;
        criteria.ModifiedBefore = DateTime.UtcNow.AddHours(-lifetimeHours);
        criteria.Take = BatchSize;

        var deletedCount = 0;
        IList<CompareList> lists;

        do
        {
            lists = (await compareListSearchService.SearchNoCloneAsync(criteria)).Results;

            if (lists.Count > 0)
            {
                await compareListService.DeleteAsync(lists.Select(x => x.Id).ToList());
                deletedCount += lists.Count;
            }
        }
        while (lists.Count == BatchSize);

        return deletedCount;
    }

    protected virtual async Task<int> DeleteOrphanedListsAsync()
    {
        var deletedCount = 0;
        string lastId = null;
        IList<CompareListOwner> lists;

        do
        {
            using (var repository = repositoryFactory())
            {
                lists = await repository.GetRegisteredListOwnersAsync(lastId, BatchSize);
            }

            if (lists.Count == 0)
            {
                break;
            }

            lastId = lists[^1].Id;

            var orphanedListIds = await GetOrphanedListIdsAsync(lists);

            if (orphanedListIds.Count > 0)
            {
                await compareListService.DeleteAsync(orphanedListIds);
                deletedCount += orphanedListIds.Count;
            }
        }
        while (lists.Count == BatchSize);

        return deletedCount;
    }

    protected virtual async Task<IList<string>> GetOrphanedListIdsAsync(IList<CompareListOwner> lists)
    {
        var userIds = lists.Select(x => x.UserId).Distinct().ToList();

        // Only the id and contact: the platform's user search would also load each user's roles, one query per user.
        Dictionary<string, string> memberIdsByUserId;
        using (var userManager = userManagerFactory())
        {
            memberIdsByUserId = await userManager.Users
                .Where(x => userIds.Contains(x.Id))
                .Select(x => new { x.Id, x.MemberId })
                .ToDictionaryAsync(x => x.Id, x => x.MemberId);
        }

        var memberIds = memberIdsByUserId.Values.Where(x => !string.IsNullOrEmpty(x)).Distinct().ToArray();
        // Organizations come with any response group; the rest of a member isn't needed here.
        var members = memberIds.Length > 0 ? await memberService.GetByIdsAsync(memberIds, MemberResponseGroup.Default.ToString()) : [];

        var organizationIdsByMemberId = members.ToDictionary(x => x.Id, GetOrganizationIds);

        return lists
            .Where(list => IsOrphaned(list, memberIdsByUserId, organizationIdsByMemberId))
            .Select(x => x.Id)
            .ToList();
    }

    // The rule the sign-in token applies to an organization (Customer's OrganizationIdRequestValidator): a user can
    // pick only an organization their contact or employee belongs to.
    protected virtual IList<string> GetOrganizationIds(Member member)
    {
        return member switch
        {
            Contact contact => contact.Organizations ?? [],
            Employee employee => employee.Organizations ?? [],
            _ => [],
        };
    }

    // Orphaned: the user is gone, or the list's organization is one the user can no longer pick.
    protected virtual bool IsOrphaned(CompareListOwner list, IDictionary<string, string> memberIdsByUserId, IDictionary<string, IList<string>> organizationIdsByMemberId)
    {
        if (!memberIdsByUserId.TryGetValue(list.UserId, out var memberId))
        {
            return true;
        }

        if (string.IsNullOrEmpty(list.OrganizationId))
        {
            return false;
        }

        return memberId == null
            || !organizationIdsByMemberId.TryGetValue(memberId, out var organizationIds)
            || !organizationIds.Contains(list.OrganizationId);
    }
}
