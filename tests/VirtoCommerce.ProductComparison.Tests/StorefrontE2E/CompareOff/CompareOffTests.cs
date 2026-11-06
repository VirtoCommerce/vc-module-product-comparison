using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Playwright;
using VirtoCommerce.ProductComparison.Tests.StorefrontE2E.Infrastructure;
using Xunit;
using static Microsoft.Playwright.Assertions;
using static VirtoCommerce.ProductComparison.Tests.StorefrontE2E.Infrastructure.CompareControls;
using static VirtoCommerce.ProductComparison.Tests.StorefrontE2E.Infrastructure.E2ECatalog;

namespace VirtoCommerce.ProductComparison.Tests.StorefrontE2E.CompareOff;

/// <summary>
/// AC 4 and 7 (D13): with compare off for the store, the plugin-level <c>when</c> skips the plugin, so the storefront
/// shows no compare at all: no toggle (nor a place kept for one), no header link, and <c>/compare</c> is the
/// storefront's 404. The storefront decides before its router starts, so once the product page has rendered its
/// action row, whatever compare would have added is already there or never will be.
/// </summary>
[Collection(CompareOffCollection.Name)]
[Trait("Category", "StorefrontE2E")]
[Trait("Category", "IntegrationTest")]
public sealed class CompareOffTests(CompareOffEnvironment env)
{
    [Fact(Timeout = 300_000)]
    public async Task StoreWithCompareOff_ShowsNoCompare_AndHasNoComparePage()
    {
        await using (var session = await env.OpenAsGuestAsync(StorefrontEnvironment.NewGuestId(), ProductPage(LaserJet100Id)))
        {
            await session.RunAsync(nameof(StoreWithCompareOff_ShowsNoCompare_AndHasNoComparePage), async page =>
            {
                // The product page and its price block's action row are there...
                await Expect(page.GetByRole(AriaRole.Heading, new() { Level = 1, Name = LaserJet100Name, Exact = true })).ToBeVisibleAsync(new() { Timeout = 60_000 });
                await Expect(Share(page)).ToBeVisibleAsync();

                // ...without the compare toggle or the place the plugin's slot would keep for it, and the header has no
                // Compare link.
                await Expect(page.Locator("[data-slot='productActions/compare']")).ToHaveCountAsync(0);
                await Expect(AddToCompare(page)).ToHaveCountAsync(0);
                await Expect(HeaderCompareLink(page)).ToHaveCountAsync(0);
            });

            // Nothing of the plugin ran: it loads the list first thing, and it never asked for it.
            session.Operations.Should().Contain("GetProduct").And.NotContain("CompareList");
            (await session.GetGraphQlErrorsAsync()).Should().BeEmpty();
        }

        // The page by its address, in a session of its own: the storefront doesn't know the route.
        await using var compare = await env.OpenAsGuestAsync(StorefrontEnvironment.NewGuestId(), ComparePage);

        await compare.RunAsync($"{nameof(StoreWithCompareOff_ShowsNoCompare_AndHasNoComparePage)}_ComparePage", async page =>
        {
            await Expect(page.GetByRole(AriaRole.Heading, new() { Name = "Page not found", Exact = true })).ToBeVisibleAsync(new() { Timeout = 60_000 });
            await Expect(CompareTitle(page)).ToHaveCountAsync(0);
            (await compare.GetGraphQlErrorsAsync()).Should().BeEmpty();
        });
    }
}
