using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using GraphQL;
using VirtoCommerce.ProductComparison.Core;
using VirtoCommerce.ProductComparison.Core.Models;
using CatalogModuleConstants = VirtoCommerce.CatalogModule.Core.ModuleConstants;
using VirtoCommerce.ProductComparison.Xapi;
using VirtoCommerce.ProductComparison.Xapi.Aggregates;
using VirtoCommerce.ProductComparison.Xapi.Models;
using VirtoCommerce.XCart.Core.Models;
using Xunit;

namespace VirtoCommerce.ProductComparison.Tests.UnitTests;

/// <summary>
/// The compare list rules the storefront's own compare had and the server now enforces (VCST-5918):
/// a category holds at most MaxItems products, a product with the same configuration is in the list once,
/// a list holds at most ModuleConstants.MaxListItems products, and a merge at sign-in lets the guest's items win.
/// </summary>
[Trait("Category", "Unit")]
public class CompareListAggregateTests
{
    private const string Printers = "printers";
    private const string Laptops = "laptops";

    private static readonly DateTime _start = new(2026, 11, 1, 0, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void SaveProducts_AddsNewProductsLastInTheirCategory()
    {
        var aggregate = CreateAggregate(items: [Item("i1", "p1", Printers, position: 0)]);

        aggregate.SaveProducts([Product("p2", Printers), Product("p3", Laptops)]);

        aggregate.List.Items.Select(x => (x.ProductId, x.CategoryId, x.Position)).Should().Equal(
            ("p1", Printers, 0),
            ("p2", Printers, 1),
            ("p3", Laptops, 0));
    }

    [Fact]
    public void SaveProducts_EmptyCategory_IsStoredAsNoCategory()
    {
        // The storefront sends "" for a product without category breadcrumbs; it groups with the other ones.
        var aggregate = CreateAggregate();

        aggregate.SaveProducts([Product("p1", string.Empty), Product("p2", null)]);

        aggregate.List.Items.Should().OnlyContain(x => x.CategoryId == null);
        aggregate.Categories.Should().ContainSingle().Which.Items.Should().HaveCount(2);
    }

    [Fact]
    public void SaveProducts_SameProductAndConfiguration_IsKeptOnce()
    {
        // Both an item already in the list and a repeat within one call: a double click must not duplicate.
        var aggregate = CreateAggregate(items: [Item("i1", "p1", Printers, position: 0)]);

        aggregate.SaveProducts([Product("P1", Printers), Product("p2", Printers), Product("p2", Printers)]);

        aggregate.List.Items.Select(x => x.ProductId).Should().Equal("p1", "p2");
    }

    [Fact]
    public void SaveProducts_SameProductOtherConfiguration_IsASecondItem()
    {
        var aggregate = CreateAggregate();

        aggregate.SaveProducts([
            Product("p1", Printers, ProductSection("s1", "option-a")),
            Product("p1", Printers, ProductSection("s1", "option-b")),
            Product("p1", Printers, ProductSection("s1", "option-a", quantity: 2)),
            Product("p1", Printers),
        ]);

        aggregate.List.Items.Should().HaveCount(4);
        aggregate.List.Items.Select(x => x.ConfigurationKey).Should().OnlyHaveUniqueItems();
        aggregate.List.Items.Last().ConfigurationKey.Should().BeEmpty();
    }

    [Fact]
    public void SaveProducts_ConfigurationKey_IgnoresTheOrderOfSections()
    {
        var aggregate = CreateAggregate();

        aggregate.SaveProducts([Product("p1", Printers, ProductSection("s1", "a"), TextSection("s2", "engraving"))]);
        aggregate.SaveProducts([Product("p1", Printers, TextSection("s2", "engraving"), ProductSection("s1", "a"))]);

        aggregate.List.Items.Should().ContainSingle();
        aggregate.List.Items[0].Sections.Select(x => (x.SectionId, x.Type, x.ProductId, x.CustomText)).Should().BeEquivalentTo(
        [
            ("s1", "Product", "a", (string)null),
            ("s2", "Text", (string)null, "engraving"),
        ]);
    }

    [Fact]
    public void SaveProducts_FullCategory_RefusesTheCall()
    {
        var aggregate = CreateAggregate(maxItems: 2, items: [Item("i1", "p1", Printers, 0), Item("i2", "p2", Printers, 1)]);

        var act = () => aggregate.SaveProducts([Product("p3", Laptops), Product("p4", Printers)]);

        act.Should().Throw<ExecutionError>().Which.Code.Should().Be(CompareListErrors.LimitReachedCode);
    }

    [Fact]
    public void SaveProducts_CategoryOverTheLimitButUntouched_DoesNotBlockOtherCategories()
    {
        // MaxItems was lowered after printers filled up: printers keeps its items, laptops still accepts new ones.
        var aggregate = CreateAggregate(maxItems: 1, items: [Item("i1", "p1", Printers, 0), Item("i2", "p2", Printers, 1)]);

        aggregate.SaveProducts([Product("p3", Laptops)]);

        aggregate.List.Items.Should().HaveCount(3);
    }

    [Fact]
    public void SaveProducts_PastTheListCap_RefusesWithFull()
    {
        var items = Enumerable.Range(0, ModuleConstants.MaxListItems)
            .Select(i => Item($"i{i}", $"p{i}", $"category{i}", position: 0))
            .ToArray();
        var aggregate = CreateAggregate(maxItems: 5, items: items);

        var act = () => aggregate.SaveProducts([Product("one-more", "another-category")]);

        act.Should().Throw<ExecutionError>().Which.Code.Should().Be(CompareListErrors.FullCode);
    }

    [Fact]
    public void Categories_ComeInFirstAddedOrder_AndShowAtMostMaxItemsEach()
    {
        var aggregate = CreateAggregate(maxItems: 2, items:
        [
            Item("i1", "p1", Laptops, position: 0, minutes: 10),
            Item("i2", "p2", Printers, position: 1, minutes: 1),
            Item("i3", "p3", Printers, position: 0, minutes: 2),
            Item("i4", "p4", Printers, position: 2, minutes: 3),
        ]);

        aggregate.Categories.Select(x => x.CategoryId).Should().Equal(Printers, Laptops);
        aggregate.Categories[0].Items.Select(x => x.Id).Should().Equal("i3", "i2");
        aggregate.VisibleItems.Select(x => x.Id).Should().Equal("i3", "i2", "i1");
    }

    [Fact]
    public void RemoveItems_RemovesOnlyTheGivenItems()
    {
        var aggregate = CreateAggregate(items: [Item("i1", "p1", Printers, 0), Item("i2", "p2", Printers, 1)]);

        aggregate.RemoveItems(["I1", "unknown"]);

        aggregate.List.Items.Select(x => x.Id).Should().Equal("i2");
    }

    [Fact]
    public void ClearCategory_Null_ClearsOnlyProductsWithoutACategory()
    {
        var aggregate = CreateAggregate(items: [Item("i1", "p1", null, 0), Item("i2", "p2", Printers, 0)]);

        aggregate.ClearCategory(null);

        aggregate.List.Items.Select(x => x.Id).Should().Equal("i2");
    }

    [Fact]
    public void MoveItem_MovesWithinTheCategoryAndRenumbersIt()
    {
        var aggregate = CreateAggregate(items:
        [
            Item("i1", "p1", Printers, 0),
            Item("i2", "p2", Printers, 1),
            Item("i3", "p3", Printers, 2),
            Item("l1", "p4", Laptops, 0),
        ]);

        aggregate.MoveItem("i3", 0);

        PositionsOf(aggregate, Printers).Should().Equal(("i3", 0), ("i1", 1), ("i2", 2));
        aggregate.List.Items.Single(x => x.Id == "l1").Position.Should().Be(0);
    }

    [Fact]
    public void MoveItem_PositionPastTheEnd_MovesToTheEnd()
    {
        var aggregate = CreateAggregate(items: [Item("i1", "p1", Printers, 0), Item("i2", "p2", Printers, 1)]);

        aggregate.MoveItem("i1", 99);

        PositionsOf(aggregate, Printers).Should().Equal(("i2", 0), ("i1", 1));
    }

    [Fact]
    public void Merge_IncomingItemsWinAtTheLimit()
    {
        // AC 1.3.5: the guest's products come over; the account's oldest products in that category make room.
        var aggregate = CreateAggregate(maxItems: 3, items:
        [
            Item("t1", "old1", Printers, 0, minutes: 1),
            Item("t2", "old2", Printers, 1, minutes: 2),
            Item("t3", "old3", Printers, 2, minutes: 3),
        ]);
        var source = CreateList(isAnonymous: true, Item("s1", "new1", Printers, 0, minutes: 10), Item("s2", "new2", Printers, 1, minutes: 11));

        aggregate.Merge(source);

        aggregate.List.Items.Select(x => x.ProductId).Should().BeEquivalentTo("old3", "new1", "new2");
        aggregate.List.Items.Select(x => x.Position).Should().BeEquivalentTo([0, 1, 2]);
    }

    [Fact]
    public void Merge_DuplicateKeepsTheTargetsItem()
    {
        var aggregate = CreateAggregate(items: [Item("t1", "p1", Printers, 0)]);
        var source = CreateList(isAnonymous: true, Item("s1", "p1", Printers, 0, minutes: 10), Item("s2", "p2", Printers, 1, minutes: 11));

        aggregate.Merge(source);

        aggregate.List.Items.Select(x => x.Id).Should().Equal("t1", null);
        aggregate.List.Items.Select(x => x.ProductId).Should().Equal("p1", "p2");
    }

    [Fact]
    public void Merge_TakesTheSourcesNewestMaxItemsPerCategory()
    {
        var aggregate = CreateAggregate(maxItems: 2);
        var source = CreateList(
            isAnonymous: true,
            Item("s1", "p1", Printers, 0, minutes: 1),
            Item("s2", "p2", Printers, 1, minutes: 2),
            Item("s3", "p3", Printers, 2, minutes: 3));

        aggregate.Merge(source);

        aggregate.List.Items.Select(x => x.ProductId).Should().Equal("p2", "p3");
    }

    [Fact]
    public void Merge_CopiesConfigurationsWithoutTheSourcesIds()
    {
        var sourceItem = Item("s1", "p1", Printers, 0);
        sourceItem.ConfigurationKey = "key";
        sourceItem.Sections = [new CompareListItemSection { Id = "section-id", SectionId = "s1", Type = "Text", CustomText = "x" }];
        var aggregate = CreateAggregate();

        aggregate.Merge(CreateList(isAnonymous: true, sourceItem));

        var item = aggregate.List.Items.Should().ContainSingle().Subject;
        item.Id.Should().BeNull();
        item.ConfigurationKey.Should().Be("key");
        item.Sections.Should().ContainSingle().Which.Id.Should().BeNull();
        sourceItem.Sections[0].Id.Should().Be("section-id");
    }

    [Fact]
    public void EnsureAvailable_RefusesADisabledStore()
    {
        var aggregate = CreateAggregate();
        aggregate.IsEnabled = false;

        var act = aggregate.EnsureAvailable;

        act.Should().Throw<ExecutionError>().Which.Code.Should().Be(CompareListErrors.DisabledCode);
    }

    [Fact]
    public void EnsureAvailable_GuestListsOff_RefusesOnlyGuestLists()
    {
        var guest = CreateAggregate(isAnonymous: true);
        guest.IsAnonymousListEnabled = false;
        var account = CreateAggregate(isAnonymous: false);
        account.IsAnonymousListEnabled = false;

        var guestAct = guest.EnsureAvailable;

        guestAct.Should().Throw<ExecutionError>().Which.Code.Should().Be(CompareListErrors.AnonymousDisabledCode);
        account.Invoking(x => x.EnsureAvailable()).Should().NotThrow();
    }

    private static CompareListAggregate CreateAggregate(int maxItems = 5, bool isAnonymous = false, CompareListItem[] items = null)
    {
        return new CompareListAggregate
        {
            List = CreateList(isAnonymous, items ?? []),
            IsEnabled = true,
            IsAnonymousListEnabled = true,
            MaxItems = maxItems,
        };
    }

    private static CompareList CreateList(bool isAnonymous, params CompareListItem[] items)
    {
        return new CompareList
        {
            Id = isAnonymous ? "guest-list" : "account-list",
            StoreId = "store",
            UserId = isAnonymous ? "guest" : "user",
            IsAnonymous = isAnonymous,
            Items = items.ToList(),
        };
    }

    private static CompareListItem Item(string id, string productId, string categoryId, int position, int minutes = 0)
    {
        return new CompareListItem
        {
            Id = id,
            ProductId = productId,
            CategoryId = categoryId,
            ConfigurationKey = string.Empty,
            Position = position,
            CreatedDate = _start.AddMinutes(minutes),
        };
    }

    private static CompareListProduct Product(string productId, string categoryId, params ProductConfigurationSection[] sections)
    {
        return new CompareListProduct
        {
            ProductId = productId,
            CategoryId = categoryId,
            ConfigurationSections = sections.Length > 0 ? sections.ToList() : null,
        };
    }

    private static ProductConfigurationSection ProductSection(string sectionId, string optionProductId, int quantity = 1)
    {
        return new ProductConfigurationSection
        {
            SectionId = sectionId,
            Type = CatalogModuleConstants.ConfigurationSectionTypeProduct,
            Option = new ConfigurableProductOption { ProductId = optionProductId, Quantity = quantity },
        };
    }

    private static ProductConfigurationSection TextSection(string sectionId, string text)
    {
        return new ProductConfigurationSection
        {
            SectionId = sectionId,
            Type = CatalogModuleConstants.ConfigurationSectionTypeText,
            CustomText = text,
        };
    }

    private static IEnumerable<(string Id, int Position)> PositionsOf(CompareListAggregate aggregate, string categoryId)
    {
        return aggregate.List.Items
            .Where(x => x.CategoryId == categoryId)
            .OrderBy(x => x.Position)
            .Select(x => (x.Id, x.Position));
    }
}
