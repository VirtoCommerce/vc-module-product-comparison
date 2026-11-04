using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using VirtoCommerce.ProductComparison.Tests.StorefrontE2E.Infrastructure;
using Xunit;
using static Microsoft.Playwright.Assertions;
using static VirtoCommerce.ProductComparison.Tests.StorefrontE2E.Infrastructure.CompareControls;
using static VirtoCommerce.ProductComparison.Tests.StorefrontE2E.Infrastructure.E2ECatalog;

namespace VirtoCommerce.ProductComparison.Tests.StorefrontE2E.GuestCompare;

/// <summary>
/// AC 6 (D10): what a guest compared follows them into their account. The plugin remembers the guest's list id in
/// localStorage; the first load as a signed-in buyer merges that list into the buyer's (mergeCompareList) and forgets
/// the id. The test brings its own guest and its own buyer.
/// </summary>
[Collection(GuestCompareCollection.Name)]
[Trait("Category", "StorefrontE2E")]
[Trait("Category", "IntegrationTest")]
public sealed class SignInMergeTests(GuestCompareEnvironment env)
{
    [Fact(Timeout = 300_000)]
    public async Task SigningIn_MergesTheGuestsList_IntoTheBuyers()
    {
        var guestId = StorefrontEnvironment.NewGuestId();
        var buyerId = env.NewBuyer();

        await using var session = await env.OpenAsGuestAsync(guestId, ProductPage(LaserJet200Id));

        string rememberedListId = null;
        await session.RunAsync(nameof(SigningIn_MergesTheGuestsList_IntoTheBuyers), async page =>
        {
            await AddToCompare(page).ClickAsync(new() { Timeout = 60_000 });
            await Expect(RemoveFromCompare(page)).ToBeVisibleAsync();

            // The plugin keeps the guest's list id for the sign-in.
            rememberedListId = await session.GetLocalStorageAsync(GuestListIdKey);
        });

        var guestList = (await env.GetListsAsync(guestId)).Should().ContainSingle().Subject;
        rememberedListId.Should().Be(guestList.Id);

        // The buyer signs in in the same browser. The storefront's sign-in leaves it like this before it reloads: the
        // token in localStorage.auth, the guest's user-id dropped. (The sign-in page itself is not driven: it posts the
        // password grant, which this backend's token endpoint doesn't implement.) The next load is the buyer's first.
        await session.Page.EvaluateAsync(
            "auth => { localStorage.setItem('auth', auth); localStorage.removeItem('user-id'); }",
            env.IssueToken(buyerId).ToLocalStorageJson());
        await session.OpenInNewPageAsync(ProductPage(LaserJet200Id));

        await session.RunAsync($"{nameof(SigningIn_MergesTheGuestsList_IntoTheBuyers)}_SignedIn", async page =>
        {
            // On that load the plugin merges the remembered list into the buyer's: the buyer's page shows the guest's
            // product as compared, and the header counts it.
            await Expect(RemoveFromCompare(page)).ToHaveAttributeAsync("aria-pressed", "true", new() { Timeout = 60_000 });
            await Expect(HeaderCompareLink(page, 1)).ToBeVisibleAsync();

            // And forgets the id, so no later load merges again.
            (await session.GetLocalStorageAsync(GuestListIdKey)).Should().BeNull();

            // Through the merge mutation, once, and without an error on the way.
            session.Operations.Where(x => x == "MergeCompareList").Should().ContainSingle();
            session.GraphQlErrors.Should().BeEmpty();
        });

        // On the server: the buyer's own list (signed in, no organization) holds the product, in the guest's category;
        // the guest's list is gone.
        var buyerList = (await env.GetListsAsync(buyerId)).Should().ContainSingle().Subject;
        (buyerList.IsAnonymous, buyerList.OrganizationId).Should().Be((false, (string)null));
        buyerList.Items.Select(x => (x.ProductId, x.CategoryId)).Should().Equal((LaserJet200Id, LaserPrintersId));
        (await env.GetListsAsync(guestId)).Should().BeEmpty();
    }
}
