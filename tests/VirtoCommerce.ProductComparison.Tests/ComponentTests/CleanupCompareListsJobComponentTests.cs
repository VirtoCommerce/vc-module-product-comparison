using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.ProductComparison.Core;
using VirtoCommerce.ProductComparison.Core.Models;
using VirtoCommerce.ProductComparison.Data.Models;
using VirtoCommerce.ProductComparison.Tests.ComponentTests.Infrastructure;
using Xunit;
using static VirtoCommerce.ProductComparison.Tests.ComponentTests.Infrastructure.CompareListTestData;

namespace VirtoCommerce.ProductComparison.Tests.ComponentTests;

/// <summary>
/// The daily cleanup (VCST-5918, PLAN §4.4 and §11.9), run as the recurring job runs it: <c>Process</c> on the real
/// <c>CleanupCompareListsJob</c> over SQLite, with the accounts and contacts it checks as doubles. It deletes guest
/// lists left unchanged past the lifetime setting, then registered lists nobody can reach any more: their user is
/// gone, or their organization is one the user's contact no longer belongs to, so no sign-in token can carry it.
/// Everything else stays.
/// </summary>
[Trait("Category", "Component")]
public class CleanupCompareListsJobComponentTests
{
    // The job's page size (a private constant): the paging tests need more lists than this.
    private const int BatchSize = 500;

    [Fact]
    public async Task GuestListsUnchangedPastTheLifetime_AreDeleted_FreshAndRegisteredOnesStay()
    {
        using var ctx = ProductComparisonTestContext.Create();
        ctx.Users.Add("buyer-1");
        var expired = await ctx.SaveAsync(GuestList("guest-expired", items: [Item("printer-1", "printers", 0, configurationKey: "key-a", sections: [TextSection("engraving", "Hi")])]));
        var fresh = await ctx.SaveAsync(GuestList("guest-fresh", items: [Item("printer-1", "printers", 0)]));
        // A registered buyer's list never expires, however old: it follows the account.
        var registered = await ctx.SaveAsync(AccountList("buyer-1", items: [Item("printer-1", "printers", 0)]));
        await ctx.SetDatesAsync(expired.Id, modifiedDate: DateTime.UtcNow.AddHours(-49));
        await ctx.SetDatesAsync(fresh.Id, modifiedDate: DateTime.UtcNow.AddHours(-47));
        await ctx.SetDatesAsync(registered.Id, modifiedDate: DateTime.UtcNow.AddDays(-365));

        await ctx.CleanupJob.Process();

        (await RemainingListIdsAsync(ctx)).Should().BeEquivalentTo([fresh.Id, registered.Id]);
        await using var db = ctx.NewDbContext();
        (await db.Set<CompareListItemEntity>().CountAsync(x => x.CompareListId == expired.Id, TestContext.Current.CancellationToken)).Should().Be(0);
        (await db.Set<CompareListItemSectionEntity>().CountAsync(TestContext.Current.CancellationToken)).Should().Be(0, "the expired list's configured item took its sections with it");
    }

    [Fact]
    public async Task GuestListLifetime_ComesFromTheSetting()
    {
        // Under the default 48 hours both lists would stay; at 2 hours the one unchanged for 3 goes.
        using var ctx = ProductComparisonTestContext.Create();
        ctx.SetSetting(ModuleConstants.Settings.Cleanup.AnonymousListLifetimeHours, 2);
        var threeHoursOld = await ctx.SaveAsync(GuestList("guest-1"));
        var oneHourOld = await ctx.SaveAsync(GuestList("guest-2"));
        await ctx.SetDatesAsync(threeHoursOld.Id, modifiedDate: DateTime.UtcNow.AddHours(-3));
        await ctx.SetDatesAsync(oneHourOld.Id, modifiedDate: DateTime.UtcNow.AddHours(-1));

        await ctx.CleanupJob.Process();

        (await RemainingListIdsAsync(ctx)).Should().Equal(oneHourOld.Id);
    }

    [Fact]
    public async Task GuestList_ChangedRecently_StaysHoweverOldItIs()
    {
        // The lifetime counts from the last change, not from creation (D15): a guest who keeps comparing keeps the
        // list. The change goes through the service, so this also needs the save to move ModifiedDate.
        using var ctx = ProductComparisonTestContext.Create();
        var list = await ctx.SaveAsync(GuestList("guest-1", items: [Item("printer-1", "printers", 0)]));
        await ctx.SetDatesAsync(list.Id, createdDate: DateTime.UtcNow.AddDays(-10), modifiedDate: DateTime.UtcNow.AddDays(-10));

        var changed = await ctx.CompareListService.GetByIdAsync(list.Id);
        changed.Items.Add(Item("printer-2", "printers", 1));
        await ctx.SaveAsync(changed);

        await ctx.CleanupJob.Process();

        (await RemainingListIdsAsync(ctx)).Should().Equal(list.Id);
    }

    [Fact]
    public async Task RegisteredLists_NobodyCanReachAnyMore_AreDeleted()
    {
        using var ctx = ProductComparisonTestContext.Create();

        // buyer-1's contact belongs to org-1; it left org-2.
        ctx.Members.AddContact("contact-1", "org-1");
        ctx.Users.Add("buyer-1", memberId: "contact-1");
        // buyer-2's account was deleted; its lists stayed behind.
        // buyer-3 has an account without a contact, so it can pick no organization.
        ctx.Users.Add("buyer-3");
        // buyer-4's account points at a contact that no longer exists.
        ctx.Users.Add("buyer-4", memberId: "contact-deleted");
        // An employee picks organizations as a contact does.
        ctx.Members.AddEmployee("employee-member-1", "org-3");
        ctx.Users.Add("employee-1", memberId: "employee-member-1");

        var lists = new Dictionary<string, CompareList>
        {
            ["buyer-1"] = AccountList("buyer-1"),
            ["buyer-1 org-1"] = AccountList("buyer-1", organizationId: "org-1"),
            ["buyer-1 org-2"] = AccountList("buyer-1", organizationId: "org-2"),
            ["buyer-2"] = AccountList("buyer-2"),
            ["buyer-2 org-1"] = AccountList("buyer-2", organizationId: "org-1"),
            ["buyer-3"] = AccountList("buyer-3"),
            ["buyer-3 org-1"] = AccountList("buyer-3", organizationId: "org-1"),
            ["buyer-4"] = AccountList("buyer-4"),
            ["buyer-4 org-1"] = AccountList("buyer-4", organizationId: "org-1"),
            ["employee-1 org-3"] = AccountList("employee-1", organizationId: "org-3"),
            // A guest id is never a registered user's: the orphan check must not take a live guest list for one.
            ["guest"] = GuestList("guest-1"),
        };
        await ctx.SaveAsync(lists.Values.ToList());

        await ctx.CleanupJob.Process();

        var remaining = await RemainingListIdsAsync(ctx);
        remaining.Should().BeEquivalentTo(
            new[] { "buyer-1", "buyer-1 org-1", "buyer-3", "buyer-4", "employee-1 org-3", "guest" }.Select(x => lists[x].Id),
            "a list stays while its user exists and the list's organization, if any, is one the user can still pick");
    }

    [Fact]
    public async Task ExpiredGuestLists_BeyondOneBatch_AreAllDeleted()
    {
        // The job deletes a page of 500 and searches again until a page comes back short. Two full pages and one
        // more list prove it keeps going; the fresh lists prove it stops at the lifetime.
        using var ctx = ProductComparisonTestContext.Create();
        var expired = Enumerable.Range(0, (2 * BatchSize) + 1).Select(i => GuestList($"expired-{i:D4}")).ToList();
        var fresh = Enumerable.Range(0, 3).Select(i => GuestList($"fresh-{i}")).ToList();
        await ctx.SaveAsync([.. expired, .. fresh]);
        await ctx.SetDatesAsync(x => x.UserId.StartsWith("expired-"), modifiedDate: DateTime.UtcNow.AddDays(-3));

        await ctx.CleanupJob.Process();

        (await RemainingListIdsAsync(ctx)).Should().BeEquivalentTo(fresh.Select(x => x.Id));
    }

    [Fact]
    public async Task OrphanedLists_BeyondOneBatch_AreAllFound()
    {
        // The orphan pass pages through every registered list, oldest first, and skips only the lists it keeps:
        // deleting a page's orphans moves the next lists up into it. With every third list orphaned across 1,200
        // lists, stopping after one page, or skipping a full page each time, leaves orphans behind; deleting a
        // kept list would show too.
        using var ctx = ProductComparisonTestContext.Create();
        var lists = new List<CompareList>();
        for (var i = 0; i < 1200; i++)
        {
            var userId = $"user-{i:D4}";
            if (i % 3 != 0)
            {
                ctx.Users.Add(userId);
            }

            var list = AccountList(userId);
            list.Id = $"list-{i:D4}";
            lists.Add(list);
        }

        await ctx.SaveAsync(lists);
        // One creation instant for all, so the job's order (CreatedDate, then Id) is the id order above.
        await ctx.SetDatesAsync(x => true, createdDate: DateTime.UtcNow.AddDays(-1));

        await ctx.CleanupJob.Process();

        (await RemainingListIdsAsync(ctx)).Should().BeEquivalentTo(lists.Where((_, i) => i % 3 != 0).Select(x => x.Id));
        ctx.Users.Searches.Should().OnlyContain(x => x.Count > 0 && x.Count <= BatchSize, "the users are looked up a page of lists at a time");
    }

    private static async Task<IList<string>> RemainingListIdsAsync(ProductComparisonTestContext ctx)
    {
        await using var db = ctx.NewDbContext();

        return await db.Set<CompareListEntity>().Select(x => x.Id).ToListAsync(TestContext.Current.CancellationToken);
    }
}
