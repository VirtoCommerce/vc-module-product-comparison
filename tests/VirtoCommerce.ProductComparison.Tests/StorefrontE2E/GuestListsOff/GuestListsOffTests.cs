using System.Text.RegularExpressions;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Playwright;
using VirtoCommerce.ProductComparison.Tests.StorefrontE2E.Infrastructure;
using Xunit;
using static Microsoft.Playwright.Assertions;
using static VirtoCommerce.ProductComparison.Tests.StorefrontE2E.Infrastructure.CompareControls;
using static VirtoCommerce.ProductComparison.Tests.StorefrontE2E.Infrastructure.E2ECatalog;

namespace VirtoCommerce.ProductComparison.Tests.StorefrontE2E.GuestListsOff;

/// <summary>
/// AC 3: on a store whose guests can't keep a compare list, a guest still sees the toggle, and their click is answered
/// with "Sign in to add products to compare" and a way to sign in, not with a list.
/// </summary>
[Collection(GuestListsOffCollection.Name)]
[Trait("Category", "StorefrontE2E")]
[Trait("Category", "IntegrationTest")]
public sealed class GuestListsOffTests(GuestListsOffEnvironment env)
{
    [Fact(Timeout = 300_000)]
    public async Task GuestsClick_AsksThemToSignIn_AndSavesNothing()
    {
        var guestId = StorefrontEnvironment.NewGuestId();
        await using var session = await env.OpenAsGuestAsync(guestId, ProductPage(LaserJet100Id));

        await session.RunAsync(nameof(GuestsClick_AsksThemToSignIn_AndSavesNothing), async page =>
        {
            await AddToCompare(page).ClickAsync(new() { Timeout = 60_000 });

            // The warning, with a Sign in button that leads to the sign-in page and back.
            var toasts = Toasts(page);
            await Expect(toasts).ToContainTextAsync("Sign in to add products to compare");
            await Expect(toasts.GetByRole(AriaRole.Link, new() { Name = "Sign in", Exact = true }))
                .ToHaveAttributeAsync("href", new Regex($"^/sign-in\\?returnUrl=.*{Regex.Escape(LaserJet100Id)}"));

            // The toggle didn't flip and the header counts nothing.
            await Expect(AddToCompare(page)).ToHaveAttributeAsync("aria-pressed", "false");
            await Expect(HeaderCompareLink(page, 0)).ToBeVisibleAsync();
        });

        // The plugin refused before calling the server, and a guest who never compared asks it nothing, not even for
        // the list; the server holds no list for the guest.
        session.Operations.Should().NotContain("CompareList").And.NotContain("SaveProductsToCompareList");
        (await session.GetGraphQlErrorsAsync()).Should().BeEmpty();
        (await env.GetListsAsync(guestId)).Should().BeEmpty();
    }
}
