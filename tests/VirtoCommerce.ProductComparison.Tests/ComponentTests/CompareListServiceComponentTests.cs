using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.ProductComparison.Core.Models;
using VirtoCommerce.ProductComparison.Data.Models;
using VirtoCommerce.ProductComparison.Tests.ComponentTests.Infrastructure;
using Xunit;
using static VirtoCommerce.ProductComparison.Tests.ComponentTests.Infrastructure.CompareListTestData;

namespace VirtoCommerce.ProductComparison.Tests.ComponentTests;

/// <summary>
/// The compare list's persistence (VCST-5918), through the real <c>CompareListService</c>, repository and EF model
/// over SQLite: what is saved comes back field by field, an item-only change moves the list's ModifiedDate (the
/// cleanup reads it to tell an active guest list from an abandoned one), and a removed item or list takes its rows
/// with it.
/// </summary>
[Trait("Category", "Component")]
public class CompareListServiceComponentTests
{
    [Fact]
    public async Task SavedList_LoadsBackFieldByField()
    {
        using var ctx = ProductComparisonTestContext.Create();
        var startedAt = DateTime.UtcNow;
        var list = AccountList("buyer-1", organizationId: "org-1", items:
        [
            Item("printer-1", "printers", position: 0),
            Item("printer-2", "printers", position: 1, configurationKey: "key-a", sections:
            [
                ProductSection("ink", "ink-black", quantity: 2),
                TextSection("engraving", "To the team"),
            ]),
            Item("laptop-1", categoryId: null, position: 0),
        ]);

        await ctx.SaveAsync(list);
        var loaded = await ctx.ReloadAsync(list.Id);

        // The save assigned ids to the list, its items and their sections.
        list.Id.Should().NotBeNullOrEmpty();
        list.Items.Should().OnlyContain(x => !string.IsNullOrEmpty(x.Id));
        list.Items.SelectMany(x => x.Sections).Should().OnlyContain(x => !string.IsNullOrEmpty(x.Id));

        loaded.Should().NotBeNull();
        loaded.Id.Should().Be(list.Id);
        loaded.StoreId.Should().Be(ProductComparisonTestContext.StoreId);
        loaded.UserId.Should().Be("buyer-1");
        loaded.OrganizationId.Should().Be("org-1");
        loaded.IsAnonymous.Should().BeFalse();
        loaded.CreatedDate.Should().BeOnOrAfter(startedAt);
        loaded.ModifiedDate.Should().Be(loaded.CreatedDate);
        loaded.CreatedBy.Should().Be(PlatformAuditTriggers.UserName);

        loaded.Items.Select(ItemFields).Should().BeEquivalentTo(list.Items.Select(ItemFields));
        loaded.Items.Should().OnlyContain(x => x.CreatedDate >= startedAt && x.ModifiedDate == x.CreatedDate);

        // A configured item's sections come back by section id, the order the configuration key hashes them in.
        loaded.Items.Single(x => x.ProductId == "printer-2").Sections.Select(x => x.SectionId).Should().Equal("engraving", "ink");
    }

    [Theory]
    [InlineData("added")]
    [InlineData("moved")]
    [InlineData("removed")]
    public async Task ItemOnlyChange_IsSaved_AndMovesTheListsModifiedDate(string change)
    {
        // An add, a move or a remove changes only CompareListItem rows; nothing on the CompareList row changes. The
        // platform's CrudService still stamps the list's ModifiedDate on every update, and the cleanup relies on it:
        // a guest list whose date stood still while the guest kept comparing would be deleted as abandoned.
        using var ctx = ProductComparisonTestContext.Create();
        var list = await ctx.SaveAsync(GuestList("guest-1", items: [Item("printer-1", "printers", 0), Item("printer-2", "printers", 1)]));
        var threeDaysAgo = DateTime.UtcNow.AddDays(-3);
        await ctx.SetDatesAsync(list.Id, createdDate: threeDaysAgo, modifiedDate: threeDaysAgo);
        var changedAt = DateTime.UtcNow;

        var changed = await ctx.CompareListService.GetByIdAsync(list.Id);
        switch (change)
        {
            case "added":
                changed.Items.Add(Item("printer-3", "printers", 2));
                break;
            case "moved":
                changed.Items.Single(x => x.ProductId == "printer-1").Position = 1;
                changed.Items.Single(x => x.ProductId == "printer-2").Position = 0;
                break;
            case "removed":
                changed.Items.Remove(changed.Items.Single(x => x.ProductId == "printer-2"));
                break;
        }
        await ctx.SaveAsync(changed);

        var loaded = await ctx.ReloadAsync(list.Id);

        var expectedPositions = change switch
        {
            "added" => new[] { ("printer-1", 0), ("printer-2", 1), ("printer-3", 2) },
            "moved" => [("printer-2", 0), ("printer-1", 1)],
            _ => [("printer-1", 0)],
        };
        loaded.Items.Select(x => (x.ProductId, x.Position)).Should().Equal(expectedPositions);

        loaded.ModifiedDate.Should().BeOnOrAfter(changedAt, "an item-only change is a change of the list");
        loaded.CreatedDate.Should().BeCloseTo(threeDaysAgo, TimeSpan.FromMilliseconds(1), "an update keeps the list's creation date");
    }

    [Fact]
    public async Task RemovedItem_TakesItsSectionsWithIt()
    {
        using var ctx = ProductComparisonTestContext.Create();
        var list = await ctx.SaveAsync(AccountList("buyer-1", items:
        [
            Item("printer-1", "printers", 0, configurationKey: "key-a", sections: [ProductSection("ink", "ink-black"), TextSection("engraving", "Hi")]),
            Item("printer-2", "printers", 1, configurationKey: "key-b", sections: [ProductSection("ink", "ink-color")]),
        ]));

        var changed = await ctx.CompareListService.GetByIdAsync(list.Id);
        changed.Items.Remove(changed.Items.Single(x => x.ProductId == "printer-1"));
        await ctx.SaveAsync(changed);

        await using var db = ctx.NewDbContext();
        (await db.Set<CompareListItemEntity>().Select(x => x.ProductId).ToListAsync(TestContext.Current.CancellationToken)).Should().Equal("printer-2");
        (await db.Set<CompareListItemSectionEntity>().Select(x => x.ProductId).ToListAsync(TestContext.Current.CancellationToken)).Should().Equal("ink-color");
    }

    [Fact]
    public async Task DeletedList_TakesItsItemsAndSectionsWithIt()
    {
        using var ctx = ProductComparisonTestContext.Create();
        var deleted = await ctx.SaveAsync(GuestList("guest-1", items:
        [
            Item("printer-1", "printers", 0, configurationKey: "key-a", sections: [ProductSection("ink", "ink-black"), TextSection("engraving", "Hi")]),
            Item("laptop-1", "laptops", 0),
        ]));
        // Another list's rows must stay: the delete is by list, not by table.
        var kept = await ctx.SaveAsync(GuestList("guest-2", items:
        [
            Item("printer-1", "printers", 0, configurationKey: "key-a", sections: [ProductSection("ink", "ink-black")]),
        ]));

        await ctx.CompareListService.DeleteAsync([deleted.Id]);

        (await ctx.ReloadAsync(deleted.Id)).Should().BeNull();
        await using var db = ctx.NewDbContext();
        (await db.Set<CompareListEntity>().Select(x => x.Id).ToListAsync(TestContext.Current.CancellationToken)).Should().Equal(kept.Id);
        (await db.Set<CompareListItemEntity>().Select(x => x.CompareListId).ToListAsync(TestContext.Current.CancellationToken)).Should().Equal(kept.Id);
        (await db.Set<CompareListItemSectionEntity>().Select(x => x.CompareListItem.CompareListId).ToListAsync(TestContext.Current.CancellationToken)).Should().Equal(kept.Id);
    }

    private static object ItemFields(CompareListItem item)
    {
        return new
        {
            item.Id,
            item.ProductId,
            item.CategoryId,
            item.ConfigurationKey,
            item.Position,
            Sections = item.Sections.Select(x => new { x.Id, x.SectionId, x.Type, x.ProductId, x.Quantity, x.CustomText }),
        };
    }
}
