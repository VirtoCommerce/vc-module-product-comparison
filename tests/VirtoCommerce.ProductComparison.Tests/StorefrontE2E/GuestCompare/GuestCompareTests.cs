using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using FluentAssertions;
using VirtoCommerce.ProductComparison.Tests.StorefrontE2E.Infrastructure;
using Xunit;
using static Microsoft.Playwright.Assertions;
using static VirtoCommerce.ProductComparison.Tests.StorefrontE2E.Infrastructure.CompareControls;
using static VirtoCommerce.ProductComparison.Tests.StorefrontE2E.Infrastructure.E2ECatalog;

namespace VirtoCommerce.ProductComparison.Tests.StorefrontE2E.GuestCompare;

/// <summary>
/// A guest's compare list from the storefront (VCST-5918): the plugin's toggle in the product page's price block, the
/// header's count, the compare page, and the rows behind them. Each test is a guest of its own (a fresh browser
/// context with its own guest id, the id the storefront keeps for a guest's cart), so the lists never meet.
/// </summary>
[Collection(GuestCompareCollection.Name)]
[Trait("Category", "StorefrontE2E")]
[Trait("Category", "IntegrationTest")]
public sealed class GuestCompareTests(GuestCompareEnvironment env)
{
    /// <summary>
    /// AC 1 and 4 end to end, for a guest: adding from the product page creates the guest's list on the server (keyed
    /// by the storefront's guest id, flagged anonymous, without an organization) with the product in its compare
    /// category, which the plugin takes from the product's breadcrumbs (the category at depth 2, not the product's
    /// own); the header counts it; the compare page shows it under that category's name; removing it empties the page,
    /// the count and the list, which itself stays for the next save.
    /// </summary>
    [Fact(Timeout = 300_000)]
    public async Task Guest_AddsAProductFromItsPage_ComparesIt_AndRemovesIt()
    {
        var guestId = StorefrontEnvironment.NewGuestId();
        await using var session = await env.OpenAsGuestAsync(guestId, ProductPage(LaserJet100Id));

        await session.RunAsync(nameof(Guest_AddsAProductFromItsPage_ComparesIt_AndRemovesIt), async page =>
        {
            // The plugin's toggle sits in the price block's action row; the guest has no list yet, so it offers to add.
            await AddToCompare(page).ClickAsync(new() { Timeout = 60_000 });

            // Added: the toggle flips, the toast names what's left of the category's limit (5 by default), and the
            // header counts the product.
            await Expect(RemoveFromCompare(page)).ToHaveAttributeAsync("aria-pressed", "true");
            await Expect(Toasts(page)).ToContainTextAsync("is added to compare list (items left - 4)");
            await Expect(HeaderCompareLink(page, 1)).ToBeVisibleAsync();
        });

        // The save reached the server as the guest's own list.
        var list = (await env.GetListsAsync(guestId)).Should().ContainSingle().Subject;
        (list.IsAnonymous, list.OrganizationId).Should().Be((true, (string)null));
        list.Items.Select(x => (x.ProductId, x.CategoryId)).Should().Equal((LaserJet100Id, LaserPrintersId));

        await session.RunAsync($"{nameof(Guest_AddsAProductFromItsPage_ComparesIt_AndRemovesIt)}_ComparePage", async page =>
        {
            // The header link opens the compare page: a tab for the category, named by the server, and a column for
            // the product.
            await HeaderCompareLink(page, 1).ClickAsync();
            await Expect(CompareTitle(page)).ToBeVisibleAsync();
            await Expect(CategoryTab(page, LaserPrintersName, 1)).ToBeVisibleAsync();
            var column = ProductColumn(page, LaserJet100Name);
            await Expect(column).ToBeVisibleAsync();

            // Removing the only product empties the page and the header's count.
            await RemoveButton(column).ClickAsync();
            await Expect(EmptyState(page)).ToBeVisibleAsync();
            await Expect(HeaderCompareLink(page, 0)).ToBeVisibleAsync();

            // Every request of the way, the storefront's and the plugin's, was answered without an error.
            session.GraphQlErrors.Should().BeEmpty();
        });

        // Gone from the list on the server; the list itself stays, so the guest's next save needs no new one.
        var emptied = (await env.GetListsAsync(guestId)).Should().ContainSingle().Subject;
        emptied.Id.Should().Be(list.Id);
        emptied.Items.Should().BeEmpty();
    }

    /// <summary>
    /// §11.5: an item whose product the catalog no longer has (deleted after it was compared) stays in its category on
    /// the compare page as a "No longer available" column with a remove button, next to the products that still
    /// resolve; removing it deletes the item on the server and leaves the rest.
    /// </summary>
    [Fact(Timeout = 300_000)]
    public async Task ComparePage_ShowsAProductTheCatalogNoLongerHas_AndRemovesIt()
    {
        // Saved through the real mutation, as the storefront saves: the server stores what it is given, so an id the
        // catalog doesn't know gets in just as a product deleted later would have.
        var guestId = StorefrontEnvironment.NewGuestId();
        await env.SaveAsGuestAsync(guestId, $"{LaserJet100Id}@{LaserPrintersId}", $"{DiscontinuedProductId}@{LaserPrintersId}");

        await using var session = await env.OpenAsGuestAsync(guestId, ComparePage);

        await session.RunAsync(nameof(ComparePage_ShowsAProductTheCatalogNoLongerHas_AndRemovesIt), async page =>
        {
            // Both items count in their category; the missing product's column says so instead of showing details.
            await Expect(CategoryTab(page, LaserPrintersName, 2)).ToBeVisibleAsync(new() { Timeout = 60_000 });
            await Expect(ProductColumn(page, LaserJet100Name)).ToBeVisibleAsync();
            var unavailable = UnavailableColumn(page);
            await Expect(unavailable).ToBeVisibleAsync();

            await RemoveButton(unavailable).ClickAsync();
            await Expect(unavailable).ToHaveCountAsync(0);
            await Expect(CategoryTab(page, LaserPrintersName, 1)).ToBeVisibleAsync();
            await Expect(ProductColumn(page, LaserJet100Name)).ToBeVisibleAsync();

            // A product that doesn't resolve is not an error: the list's product field is null for it.
            session.GraphQlErrors.Should().BeEmpty();
        });

        var list = (await env.GetListsAsync(guestId)).Should().ContainSingle().Subject;
        list.Items.Select(x => x.ProductId).Should().Equal(LaserJet100Id);
    }

    /// <summary>
    /// "Clear all" asks for confirmation, then empties every category on the server; the empty page offers "Restore
    /// products", which saves the cleared products back, each in its own category, while the page that showed the
    /// empty list is open.
    /// </summary>
    [Fact(Timeout = 300_000)]
    public async Task ClearAll_EmptiesTheList_AndRestoreBringsTheProductsBack()
    {
        var guestId = StorefrontEnvironment.NewGuestId();
        await env.SaveAsGuestAsync(guestId, $"{LaserJet100Id}@{LaserPrintersId}", $"{LaserJet200Id}@{LaserPrintersId}", $"{CopyPaperA4Id}@{CopyPaperId}");

        await using var session = await env.OpenAsGuestAsync(guestId, ComparePage);

        await session.RunAsync(nameof(ClearAll_EmptiesTheList_AndRestoreBringsTheProductsBack), async page =>
        {
            // A tab per category, each with its count.
            await Expect(CategoryTab(page, LaserPrintersName, 2)).ToBeVisibleAsync(new() { Timeout = 60_000 });
            await Expect(CategoryTab(page, CopyPaperName, 1)).ToBeVisibleAsync();
            await Expect(HeaderCompareLink(page, 3)).ToBeVisibleAsync();

            await ClearAll(page).ClickAsync();
            var dialog = Dialog(page, "Clear all");
            await Expect(dialog).ToContainTextAsync("All 3 products will be removed from comparison.");
            await OkButton(dialog).ClickAsync();

            await Expect(EmptyState(page)).ToBeVisibleAsync();
            await Expect(HeaderCompareLink(page, 0)).ToBeVisibleAsync();
        });

        (await env.GetListsAsync(guestId)).Should().ContainSingle().Which.Items.Should().BeEmpty();

        await session.RunAsync($"{nameof(ClearAll_EmptiesTheList_AndRestoreBringsTheProductsBack)}_Restore", async page =>
        {
            await RestoreProducts(page).ClickAsync();

            await Expect(CategoryTab(page, LaserPrintersName, 2)).ToBeVisibleAsync();
            await Expect(CategoryTab(page, CopyPaperName, 1)).ToBeVisibleAsync();
            await Expect(HeaderCompareLink(page, 3)).ToBeVisibleAsync();
            session.GraphQlErrors.Should().BeEmpty();
        });

        (await env.GetListsAsync(guestId)).Should().ContainSingle().Which.Items.Select(x => (x.ProductId, x.CategoryId)).Should().BeEquivalentTo(
        [
            (LaserJet100Id, LaserPrintersId),
            (LaserJet200Id, LaserPrintersId),
            (CopyPaperA4Id, CopyPaperId),
        ]);
    }

    /// <summary>
    /// Deep test C5.6: "Clear category" asks for confirmation, naming the category and its count, then empties that
    /// category alone, through the plugin's own clearCompareListCategory: the one compare-list operation of the plugin
    /// that no other E2E test sends, so its document meets the server's schema only here. The page moves to the category
    /// that is left and offers no "Restore products", which only a clear that empties the list offers.
    /// </summary>
    [Fact(Timeout = 300_000)]
    public async Task ClearCategory_EmptiesThatCategoryAlone()
    {
        var guestId = StorefrontEnvironment.NewGuestId();
        await env.SaveAsGuestAsync(guestId, $"{LaserJet100Id}@{LaserPrintersId}", $"{LaserJet200Id}@{LaserPrintersId}", $"{CopyPaperA4Id}@{CopyPaperId}");

        await using var session = await env.OpenAsGuestAsync(guestId, ComparePage);

        await session.RunAsync(nameof(ClearCategory_EmptiesThatCategoryAlone), async page =>
        {
            // The page opens on the category added first.
            await Expect(CategoryTab(page, LaserPrintersName, 2)).ToHaveAttributeAsync("aria-pressed", "true", new() { Timeout = 60_000 });

            await ClearCategory(page).ClickAsync();
            var dialog = Dialog(page, "Clear category");
            await Expect(dialog).ToContainTextAsync("All 2 products in Laser printers will be removed from comparison.");
            await OkButton(dialog).ClickAsync();

            // The category is gone; the other one shows, with its product, and is all the header counts.
            await Expect(CategoryTab(page, LaserPrintersName)).ToHaveCountAsync(0);
            await Expect(CategoryTab(page, CopyPaperName, 1)).ToBeVisibleAsync();
            await Expect(ProductColumn(page, CopyPaperA4Name)).ToBeVisibleAsync();
            await Expect(HeaderCompareLink(page, 1)).ToBeVisibleAsync();
            await Expect(RestoreProducts(page)).ToHaveCountAsync(0);

            session.Operations.Where(x => x == "ClearCompareListCategory").Should().ContainSingle();
            session.Operations.Should().NotContain("ClearCompareList");
            session.GraphQlErrors.Should().BeEmpty();
        });

        (await env.GetListsAsync(guestId)).Should().ContainSingle().Which.Items.Select(x => (x.ProductId, x.CategoryId))
            .Should().Equal((CopyPaperA4Id, CopyPaperId));
    }

    /// <summary>
    /// Deep test C2.2 and C8.1: the toast an add shows has a "Compare" link (the host's toast renders the plugin's button
    /// with its route) to the compare page on the product's category, named in the URL, and the page opens on that
    /// category's tab rather than on the category added first. The plugin's specs cover each side alone.
    /// </summary>
    [Fact(Timeout = 300_000)]
    public async Task TheToastsCompareLink_OpensThePageOnTheProductsCategory()
    {
        // Copy paper first: a page whose URL names no category would open on it.
        var guestId = StorefrontEnvironment.NewGuestId();
        await env.SaveAsGuestAsync(guestId, $"{CopyPaperA4Id}@{CopyPaperId}");

        await using var session = await env.OpenAsGuestAsync(guestId, ProductPage(LaserJet100Id));

        await session.RunAsync(nameof(TheToastsCompareLink_OpensThePageOnTheProductsCategory), async page =>
        {
            await AddToCompare(page).ClickAsync(new() { Timeout = 60_000 });
            await ToastCompareLink(page).ClickAsync();

            await Expect(page).ToHaveURLAsync(new Regex($@"{Regex.Escape(ComparePage)}\?category={Regex.Escape(LaserPrintersId)}$"));
            await Expect(CategoryTab(page, LaserPrintersName, 1)).ToHaveAttributeAsync("aria-pressed", "true");
            await Expect(CategoryTab(page, CopyPaperName, 1)).ToHaveAttributeAsync("aria-pressed", "false");
            await Expect(ProductColumn(page, LaserJet100Name)).ToBeVisibleAsync();
            await Expect(HeaderCompareLink(page, 2)).ToBeVisibleAsync();

            session.GraphQlErrors.Should().BeEmpty();
        });

        (await env.GetListsAsync(guestId)).Should().ContainSingle().Which.Items.Select(x => (x.ProductId, x.CategoryId)).Should().BeEquivalentTo(
        [
            (CopyPaperA4Id, CopyPaperId),
            (LaserJet100Id, LaserPrintersId),
        ]);
    }
}
