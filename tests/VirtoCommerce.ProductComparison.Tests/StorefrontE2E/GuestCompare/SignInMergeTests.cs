using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Playwright;
using VirtoCommerce.ProductComparison.Tests.StorefrontE2E.Infrastructure;
using Xunit;
using static Microsoft.Playwright.Assertions;
using static VirtoCommerce.ProductComparison.Tests.StorefrontE2E.Infrastructure.CompareControls;
using static VirtoCommerce.ProductComparison.Tests.StorefrontE2E.Infrastructure.E2ECatalog;

namespace VirtoCommerce.ProductComparison.Tests.StorefrontE2E.GuestCompare;

/// <summary>
/// AC 6 (D10): what a guest compared follows them into their account. The plugin remembers the guest's list id in
/// localStorage; the first load as a signed-in buyer merges that list into the buyer's (mergeCompareList) and forgets
/// the id. Each test brings its own guest and its own buyer.
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

        await SignInAsync(session, buyerId);
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

    /// <summary>
    /// Deep test C7.1, which a browser session couldn't time: a merge that fails on its way, aborted as when the network
    /// drops or answered by a gateway's 502 page, keeps the guest's list. The plugin tells such a failure from a refusal
    /// by the error's code: a refusal carries one and forgets the remembered id, a failure carries none and keeps it, so
    /// the next signed-in load merges the list after all. Only a real failed request through Apollo shows that a network
    /// error or a gateway's page carries no code; if one ever did, the guest's products would be lost without a sign.
    /// </summary>
    [Theory(Timeout = 300_000)]
    [InlineData("aborted")]
    [InlineData("bad gateway")]
    public async Task AMergeThatFailsOnItsWay_KeepsTheGuestsList_AndTheNextLoadMergesIt(string failure)
    {
        var name = $"{nameof(AMergeThatFailsOnItsWay_KeepsTheGuestsList_AndTheNextLoadMergesIt)}_{failure.Replace(' ', '_')}";
        var guestId = StorefrontEnvironment.NewGuestId();
        var buyerId = env.NewBuyer();
        var guestListId = await env.SaveAsGuestAsync(guestId, $"{LaserJet200Id}@{LaserPrintersId}");

        await using var session = await env.OpenAsGuestAsync(guestId, ProductPage(LaserJet200Id));

        await session.RunAsync(name, async page =>
        {
            // The guest's browser remembers the list, and the plugin loaded it.
            await Expect(RemoveFromCompare(page)).ToHaveAttributeAsync("aria-pressed", "true", new() { Timeout = 60_000 });
        });

        // The first merge fails; everything else goes through. Once it has failed, the plugin goes on to load the
        // buyer's own list: that request going out is the point where the plugin has dealt with the failure. (The
        // merge is counted before it fails, so the request that follows the failure always finds it counted.)
        var merges = 0;
        var failureHandled = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        await session.Context.RouteAsync("**/graphql", async route =>
        {
            var operation = StorefrontSession.GetOperationName(route.Request);
            if (operation == "MergeCompareList" && Interlocked.Increment(ref merges) == 1)
            {
                await FailAsync(route, failure);
                return;
            }

            if (operation == "CompareList" && Volatile.Read(ref merges) > 0)
            {
                failureHandled.TrySetResult();
            }

            await route.ContinueAsync();
        });

        await SignInAsync(session, buyerId);
        await session.OpenInNewPageAsync(ProductPage(LaserJet200Id));

        await session.RunAsync($"{name}_Failed", async page =>
        {
            await failureHandled.Task.WaitAsync(TimeSpan.FromSeconds(60));

            // The remembered id stays for the next load, and nothing came over: the header counts nothing.
            (await session.GetLocalStorageAsync(GuestListIdKey)).Should().Be(guestListId);
            await Expect(HeaderCompareLink(page, 0)).ToBeVisibleAsync();

            // A failure on the way is no GraphQL error.
            session.GraphQlErrors.Should().BeEmpty();
        });

        // The server never saw the merge: the guest's list is whole, and the buyer has none yet.
        (await env.GetListsAsync(guestId)).Should().ContainSingle().Which.Items.Select(x => x.ProductId).Should().Equal(LaserJet200Id);
        (await env.GetListsAsync(buyerId)).Should().BeEmpty();

        // The next load, with nothing failing any more, merges the list.
        await session.OpenInNewPageAsync(ProductPage(LaserJet200Id));

        await session.RunAsync($"{name}_Retried", async page =>
        {
            await Expect(RemoveFromCompare(page)).ToHaveAttributeAsync("aria-pressed", "true", new() { Timeout = 60_000 });
            await Expect(HeaderCompareLink(page, 1)).ToBeVisibleAsync();

            (await session.GetLocalStorageAsync(GuestListIdKey)).Should().BeNull();
            session.Operations.Count(x => x == "MergeCompareList").Should().Be(2);
            session.GraphQlErrors.Should().BeEmpty();
        });

        var buyerList = (await env.GetListsAsync(buyerId)).Should().ContainSingle().Subject;
        buyerList.Items.Select(x => (x.ProductId, x.CategoryId)).Should().Equal((LaserJet200Id, LaserPrintersId));
        (await env.GetListsAsync(guestId)).Should().BeEmpty();
    }

    /// <summary>
    /// Deep test C-F4, fixed in 5ce6bfc: a sign-in merge into an account list at the 500-product cap is refused with
    /// COMPARE_LIST_FULL, the server keeps both lists as they were, the plugin forgets the guest's id (a refusal is
    /// final), and the buyer is told why the guest's products didn't come over. The other refusals these tests see
    /// happen in the plugin before any request, so this is the one that travels through Apollo: it pins the real error's
    /// shape, the code the plugin reads, together with the toast.
    /// </summary>
    [Fact(Timeout = 300_000)]
    public async Task SigningIn_IntoAFullList_TellsTheBuyer_AndKeepsBothLists()
    {
        var guestId = StorefrontEnvironment.NewGuestId();
        var buyerId = env.NewBuyer();

        // The buyer's list at the cap, in one save: 100 made-up categories of 5 made-up products. 500 is also the most
        // one call may carry.
        var fullList = Enumerable.Range(1, 100)
            .SelectMany(category => Enumerable.Range(1, 5).Select(product => $"e2e-full-{category}-{product}@e2e-full-category-{category}"))
            .ToArray();
        await env.SaveAsBuyerAsync(buyerId, fullList);
        var guestListId = await env.SaveAsGuestAsync(guestId, $"{LaserJet100Id}@{LaserPrintersId}");

        await using var session = await env.OpenAsGuestAsync(guestId, ProductPage(LaserJet100Id));

        await session.RunAsync(nameof(SigningIn_IntoAFullList_TellsTheBuyer_AndKeepsBothLists), async page =>
        {
            await Expect(RemoveFromCompare(page)).ToHaveAttributeAsync("aria-pressed", "true", new() { Timeout = 60_000 });
        });

        await SignInAsync(session, buyerId);
        await session.OpenInNewPageAsync(ProductPage(LaserJet100Id));

        await session.RunAsync($"{nameof(SigningIn_IntoAFullList_TellsTheBuyer_AndKeepsBothLists)}_SignedIn", async page =>
        {
            await Expect(Toasts(page)).ToContainTextAsync("The compare list is full. Remove some products to add new ones.", new() { Timeout = 60_000 });

            // The buyer's own list, unchanged: the header counts its 500, and the guest's product isn't among them.
            await Expect(HeaderCompareLink(page, 500)).ToBeVisibleAsync();
            await Expect(AddToCompare(page)).ToHaveAttributeAsync("aria-pressed", "false");

            // Refused, so final: the id is forgotten after the one merge.
            (await session.GetLocalStorageAsync(GuestListIdKey)).Should().BeNull();
            session.Operations.Where(x => x == "MergeCompareList").Should().ContainSingle();

            // The refusal, as the server sent it: the merge's one error, with the code the plugin read.
            (await session.WaitForGraphQlErrorsAsync(1)).Should().ContainSingle()
                .Which.Should().StartWith("MergeCompareList").And.Contain("COMPARE_LIST_FULL");
        });

        (await env.GetListsAsync(buyerId)).Should().ContainSingle().Which.Items.Should().HaveCount(500);
        var guestList = (await env.GetListsAsync(guestId)).Should().ContainSingle().Subject;
        guestList.Id.Should().Be(guestListId);
        guestList.Items.Select(x => x.ProductId).Should().Equal(LaserJet100Id);
    }

    /// <summary>
    /// The buyer signs in in the same browser. The storefront's sign-in leaves it like this before it reloads: the token
    /// in localStorage.auth, the guest's user-id dropped. (The sign-in page itself is not driven: it posts the password
    /// grant, which this backend's token endpoint doesn't implement.) The next load is the buyer's first.
    /// </summary>
    private Task SignInAsync(StorefrontSession session, string buyerId)
        => session.Page.EvaluateAsync(
            "auth => { localStorage.setItem('auth', auth); localStorage.removeItem('user-id'); }",
            env.IssueToken(buyerId).ToLocalStorageJson());

    /// <summary>A request that fails on its way: aborted, as when the network drops, or answered by a gateway's error page.</summary>
    private static Task FailAsync(IRoute route, string failure) => failure switch
    {
        "aborted" => route.AbortAsync(),
        "bad gateway" => route.FulfillAsync(new RouteFulfillOptions
        {
            Status = 502,
            ContentType = "text/html",
            Body = "<html><head><title>502 Bad Gateway</title></head><body><h1>502 Bad Gateway</h1></body></html>",
        }),
        _ => throw new ArgumentOutOfRangeException(nameof(failure), failure, "Unknown failure."),
    };
}
