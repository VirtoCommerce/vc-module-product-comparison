using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using VirtoCommerce.ProductComparison.Data.Models;
using VirtoCommerce.ProductComparison.Tests.ComponentTests.Infrastructure;
using Xunit;
using static VirtoCommerce.ProductComparison.Tests.ComponentTests.Infrastructure.CompareListDocuments;

namespace VirtoCommerce.ProductComparison.Tests.ComponentTests;

/// <summary>
/// The compare list X-API end to end (VCST-5918): GraphQL documents as the storefront sends them, executed in-process
/// against the module's schema, its builders' gate, its MediatR handlers, the aggregate and the real services over
/// SQLite. This class follows a guest's list through every mutation; the access rules, the merge and the store
/// settings have classes of their own.
/// </summary>
[Trait("Category", "Component")]
public class CompareListGraphQlComponentTests
{
    private const string StoreId = ProductComparisonTestContext.StoreId;

    // The anonymous id the storefront keeps for a guest (the guest cart's), never a registered user's.
    private const string GuestId = "9c0f6a3e-guest-0001";

    [Fact]
    public async Task Guest_SavesQueriesMovesAndClearsTheirList()
    {
        using var ctx = ProductComparisonTestContext.Create();

        // The first save creates the guest's list, under the guest's id, without an organization.
        var saved = (await ctx.ExecuteAsGuestAsync(SaveProducts(GuestId, "printer-1@printers", "printer-2@printers"))).Data("saveProductsToCompareList");
        var listId = saved.GetProperty("id").GetString();

        Items(saved).Should().Equal("printer-1@printers#0", "printer-2@printers#1");
        await using (var db = ctx.NewDbContext())
        {
            var row = await db.Set<CompareListEntity>().SingleAsync(TestContext.Current.CancellationToken);
            (row.Id, row.StoreId, row.UserId, row.OrganizationId, row.IsAnonymous).Should().Be((listId, StoreId, GuestId, (string)null, true));
        }

        // A later save adds to the same list: a new product goes last in its group, one already there stays put.
        var added = (await ctx.ExecuteAsGuestAsync(SaveProducts(GuestId, "laptop-1@laptops", "printer-1@printers"))).Data("saveProductsToCompareList");

        added.GetProperty("id").GetString().Should().Be(listId);
        Items(added).Should().BeEquivalentTo("printer-1@printers#0", "printer-2@printers#1", "laptop-1@laptops#0");

        // The query reads the list back: groups in the order they were first added, each by position.
        var queried = (await ctx.ExecuteAsGuestAsync(Query(GuestId))).Data("compareList");

        queried.GetProperty("id").GetString().Should().Be(listId);
        queried.GetProperty("storeId").GetString().Should().Be(StoreId);
        ItemsCount(queried).Should().Be(3);
        Items(queried).Should().Equal("printer-1@printers#0", "printer-2@printers#1", "laptop-1@laptops#0");
        Categories(queried).Should().Equal("printers: printer-1 printer-2", "laptops: laptop-1");

        // A move renumbers the item's own group and leaves the other alone.
        var moved = (await ctx.ExecuteAsGuestAsync(Mutation("moveCompareListItem", GuestId, $"""itemId: "{ItemId(queried, "printer-2")}" position: 0"""))).Data("moveCompareListItem");

        Items(moved).Should().Equal("printer-2@printers#0", "printer-1@printers#1", "laptop-1@laptops#0");
        Items((await ctx.ExecuteAsGuestAsync(Query(GuestId))).Data("compareList")).Should().Equal(Items(moved));

        // Clearing empties the list; the list itself stays, so the next save needs no new one.
        var cleared = (await ctx.ExecuteAsGuestAsync(Mutation("clearCompareList", GuestId))).Data("clearCompareList");

        ItemsCount(cleared).Should().Be(0);
        var afterClear = (await ctx.ExecuteAsGuestAsync(Query(GuestId))).Data("compareList");
        afterClear.GetProperty("id").GetString().Should().Be(listId);
        ItemsCount(afterClear).Should().Be(0);
        await using (var db = ctx.NewDbContext())
        {
            (await db.Set<CompareListItemEntity>().CountAsync(TestContext.Current.CancellationToken)).Should().Be(0);
        }
    }

    [Fact]
    public async Task Guest_RemovesItemsAndClearsOneCategory()
    {
        using var ctx = ProductComparisonTestContext.Create();
        var saved = (await ctx.ExecuteAsGuestAsync(SaveProducts(GuestId, "printer-1@printers", "printer-2@printers", "laptop-1@laptops", "mouse-1"))).Data("saveProductsToCompareList");

        var removed = (await ctx.ExecuteAsGuestAsync(Mutation("removeCompareListItems", GuestId, $"""itemIds: ["{ItemId(saved, "printer-1")}"]"""))).Data("removeCompareListItems");
        Items(removed).Should().BeEquivalentTo("printer-2@printers#1", "laptop-1@laptops#0", "mouse-1@#0");

        var laptopsCleared = (await ctx.ExecuteAsGuestAsync(Mutation("clearCompareListCategory", GuestId, """categoryId: "laptops" """))).Data("clearCompareListCategory");
        Items(laptopsCleared).Should().BeEquivalentTo("printer-2@printers#1", "mouse-1@#0");

        // A null category is the group of products saved without one.
        var uncategorizedCleared = (await ctx.ExecuteAsGuestAsync(Mutation("clearCompareListCategory", GuestId, "categoryId: null"))).Data("clearCompareListCategory");
        Items(uncategorizedCleared).Should().Equal("printer-2@printers#1");

        Items((await ctx.ExecuteAsGuestAsync(Query(GuestId))).Data("compareList")).Should().Equal("printer-2@printers#1");
    }

    [Fact]
    public async Task MutationResponse_ShowsTheListAsTheQueryReturnsIt()
    {
        // The storefront renders the list a mutation returns until it next queries, so the two must agree: a
        // category added later comes after the earlier ones, and a new item carries the date it was added.
        using var ctx = ProductComparisonTestContext.Create();
        await ctx.ExecuteAsGuestAsync(SaveProducts(GuestId, "printer-1@printers"));

        var added = (await ctx.ExecuteAsGuestAsync(SaveProducts(GuestId, "laptop-1@laptops"))).Data("saveProductsToCompareList");
        var queried = (await ctx.ExecuteAsGuestAsync(Query(GuestId))).Data("compareList");

        Categories(queried).Should().Equal("printers: printer-1", "laptops: laptop-1");
        Categories(added).Should().Equal(Categories(queried));
        CreatedDate(added, "laptop-1").Should().Be(CreatedDate(queried, "laptop-1"));
    }

    [Fact]
    public async Task ConfiguredProduct_KeepsItsSections_AndEachConfigurationIsAnItemOfItsOwn()
    {
        using var ctx = ProductComparisonTestContext.Create();
        const string inkBlackWithEngraving = """
            { sectionId: "ink" type: "Product" option: { productId: "ink-black" quantity: 2 } }
            { sectionId: "engraving" type: "Text" customText: "To the team" }
            """;
        const string sameInOtherOrder = """
            { sectionId: "engraving" type: "Text" customText: "To the team" }
            { sectionId: "ink" type: "Product" option: { productId: "ink-black" quantity: 2 } }
            """;
        const string inkColor = """{ sectionId: "ink" type: "Product" option: { productId: "ink-color" quantity: 1 } }""";

        await ctx.ExecuteAsGuestAsync(Mutation("saveProductsToCompareList", GuestId, $$"""products: [{ productId: "printer-1" categoryId: "printers" configurationSections: [{{inkBlackWithEngraving}}] }]"""));
        // The same choices in another order are the same configuration: nothing is added.
        await ctx.ExecuteAsGuestAsync(Mutation("saveProductsToCompareList", GuestId, $$"""products: [{ productId: "printer-1" categoryId: "printers" configurationSections: [{{sameInOtherOrder}}] }]"""));
        // Another option, and the product without configuration, are two more items.
        await ctx.ExecuteAsGuestAsync(Mutation("saveProductsToCompareList", GuestId, $$"""products: [{ productId: "printer-1" categoryId: "printers" configurationSections: [{{inkColor}}] } { productId: "printer-1" categoryId: "printers" }]"""));

        var list = (await ctx.ExecuteAsGuestAsync(Query(GuestId))).Data("compareList");

        Items(list).Should().Equal("printer-1@printers#0", "printer-1@printers#1", "printer-1@printers#2");
        var sections = list.GetProperty("items").EnumerateArray()
            .Select(item => item.GetProperty("configurationSections").EnumerateArray()
                .Select(x => $"{x.GetProperty("sectionId").GetString()}|{x.GetProperty("type").GetString()}|{x.GetProperty("productId").GetString()}|{Quantity(x)}|{x.GetProperty("customText").GetString()}")
                .ToList())
            .ToList();
        // Sections come back by section id, whatever order the storefront sent them in.
        sections[0].Should().Equal("engraving|Text|||To the team", "ink|Product|ink-black|2|");
        sections[1].Should().Equal("ink|Product|ink-color|1|");
        sections[2].Should().BeEmpty();

        // The rows carry the configuration key: empty for the plain product, a SHA-256 for a configured one.
        await using var db = ctx.NewDbContext();
        var keys = await db.Set<CompareListItemEntity>().OrderBy(x => x.Position).Select(x => x.ConfigurationKey).ToListAsync(TestContext.Current.CancellationToken);
        keys[0].Should().MatchRegex("^[0-9a-f]{64}$");
        keys[1].Should().MatchRegex("^[0-9a-f]{64}$").And.NotBe(keys[0]);
        keys[2].Should().BeEmpty();
    }

    [Fact]
    public async Task InvalidInput_IsRefusedBeforeAnythingIsStored()
    {
        // The validators run in the builders, before the gate and the handler: a File section (the storefront never
        // compares uploads) and an empty user id are refused with VALIDATION, and no list is created.
        using var ctx = ProductComparisonTestContext.Create();
        const string fileSection = """{ sectionId: "logo" type: "File" fileUrls: ["https://example.com/logo.png"] }""";

        var fileResponse = await ctx.ExecuteAsGuestAsync(Mutation("saveProductsToCompareList", GuestId, $$"""products: [{ productId: "printer-1" configurationSections: [{{fileSection}}] }]"""));
        var emptyUserResponse = await ctx.ExecuteAsGuestAsync(SaveProducts(string.Empty, "printer-1@printers"));
        var emptyProductsResponse = await ctx.ExecuteAsGuestAsync(Mutation("saveProductsToCompareList", GuestId, "products: []"));

        fileResponse.ErrorCode.Should().Be("VALIDATION");
        emptyUserResponse.ErrorCode.Should().Be("VALIDATION");
        emptyProductsResponse.ErrorCode.Should().Be("VALIDATION");
        await using var db = ctx.NewDbContext();
        (await db.Set<CompareListEntity>().CountAsync(TestContext.Current.CancellationToken)).Should().Be(0);
    }

    private static string CreatedDate(JsonElement list, string productId)
    {
        return list.GetProperty("items").EnumerateArray()
            .Single(x => x.GetProperty("productId").GetString() == productId)
            .GetProperty("createdDate").GetString();
    }

    private static string Quantity(JsonElement section)
    {
        var quantity = section.GetProperty("quantity");

        return quantity.ValueKind == JsonValueKind.Null ? string.Empty : quantity.GetInt32().ToString();
    }
}
