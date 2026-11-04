using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using VirtoCommerce.CustomerModule.Core.Model;
using VirtoCommerce.CustomerModule.Core.Services;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Core.Security;
using VirtoCommerce.Platform.Core.Security.Search;
using VirtoCommerce.Platform.Core.Settings;
using VirtoCommerce.ProductComparison.Core;
using VirtoCommerce.ProductComparison.Core.Models;
using VirtoCommerce.ProductComparison.Core.Services;

namespace VirtoCommerce.ProductComparison.Data.BackgroundJobs;

public class CleanupCompareListsJob(
    ICompareListService compareListService,
    ICompareListSearchService compareListSearchService,
    IUserSearchService userSearchService,
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
        var criteria = AbstractTypeFactory<CompareListSearchCriteria>.TryCreateInstance();
        criteria.IsAnonymous = false;
        criteria.Take = BatchSize;

        var deletedCount = 0;
        IList<CompareList> lists;

        do
        {
            lists = (await compareListSearchService.SearchNoCloneAsync(criteria)).Results;

            var orphanedListIds = lists.Count > 0 ? await GetOrphanedListIdsAsync(lists) : [];

            if (orphanedListIds.Count > 0)
            {
                await compareListService.DeleteAsync(orphanedListIds);
                deletedCount += orphanedListIds.Count;
            }

            // Deleted lists no longer take up the page, so skip only the ones that stay.
            criteria.Skip += lists.Count - orphanedListIds.Count;
        }
        while (lists.Count == BatchSize);

        return deletedCount;
    }

    protected virtual async Task<IList<string>> GetOrphanedListIdsAsync(IList<CompareList> lists)
    {
        var userIds = lists.Select(x => x.UserId).Distinct().ToList();

        var userCriteria = AbstractTypeFactory<UserSearchCriteria>.TryCreateInstance();
        userCriteria.ObjectIds = userIds;
        userCriteria.Take = userIds.Count;
        var users = (await userSearchService.SearchUsersAsync(userCriteria)).Results;

        var memberIds = users.Select(x => x.MemberId).Where(x => !string.IsNullOrEmpty(x)).Distinct().ToArray();
        var members = memberIds.Length > 0 ? await memberService.GetByIdsAsync(memberIds) : [];

        var memberIdsByUserId = users.ToDictionary(x => x.Id, x => x.MemberId);
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

    private static bool IsOrphaned(CompareList list, Dictionary<string, string> memberIdsByUserId, Dictionary<string, IList<string>> organizationIdsByMemberId)
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
