using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Playwright;
using VirtoCommerce.ProductComparison.Tests.StorefrontE2E.Infrastructure;
using Xunit;
using static Microsoft.Playwright.Assertions;
using static VirtoCommerce.ProductComparison.Tests.StorefrontE2E.Infrastructure.CompareControls;

namespace VirtoCommerce.ProductComparison.Tests.StorefrontE2E.CompareLayout;

/// <summary>
/// The compare page's layout at each of QA's widths, with the stress content of <see cref="CompareLayoutEnvironment"/>:
/// as it opens, with the differences shown and a row pinned, scrolled until the table's header sticks, with each of its
/// dialogs open, and emptied. A test fails on a sideways scroll or on text cut off without an ellipsis, and lists every
/// width and element that did it. These tests check what fits, not what works; the functional groups check that.
/// </summary>
[Collection(CompareLayoutCollection.Name)]
[Trait("Category", "StorefrontE2E")]
[Trait("Category", "IntegrationTest")]
[Trait("Kind", "Layout")]
public sealed class CompareLayoutTests(CompareLayoutEnvironment env)
{
    private const string PageRoot = ".compare-products";

    [Fact(Timeout = 300_000)]
    public async Task DefaultView_FitsAtEveryWidth()
    {
        await using var session = await env.OpenAsGuestAsync(env.GuestId, ComparePage);

        await session.RunAsync(nameof(DefaultView_FitsAtEveryWidth), async page =>
        {
            await ReadyAsync(page);

            await ExpectToFitAsync(page, nameof(DefaultView_FitsAtEveryWidth), [PageRoot]);
        });
    }

    [Fact(Timeout = 300_000)]
    public async Task DifferencesWithAPinnedRow_FitsAtEveryWidth()
    {
        await using var session = await env.OpenAsGuestAsync(env.GuestId, ComparePage);

        await session.RunAsync(nameof(DifferencesWithAPinnedRow_FitsAtEveryWidth), async page =>
        {
            await ReadyAsync(page);

            // Only the rows that differ, the longest of them pinned above the rest.
            await page.GetByRole(AriaRole.Button, new() { Name = "Differences", Exact = true }).ClickAsync();
            await page.GetByRole(AriaRole.Button, new() { Name = $"Pin “{CompareLayoutEnvironment.NotesLabel}”", Exact = true }).ClickAsync();
            await Expect(page.GetByRole(AriaRole.Button, new() { Name = $"Unpin “{CompareLayoutEnvironment.NotesLabel}”", Exact = true })).ToBeVisibleAsync();

            await ExpectToFitAsync(page, nameof(DifferencesWithAPinnedRow_FitsAtEveryWidth), [PageRoot]);
        });
    }

    [Fact(Timeout = 300_000)]
    public async Task ScrolledWithTheHeaderStuck_FitsAtEveryWidth()
    {
        await using var session = await env.OpenAsGuestAsync(env.GuestId, ComparePage);

        await session.RunAsync(nameof(ScrolledWithTheHeaderStuck_FitsAtEveryWidth), async page =>
        {
            await ReadyAsync(page);

            // From md up, the header row sticks under the site's header and turns compact. Below md it scrolls along.
            await ScrollIntoTheTableAsync(page);
            await Expect(page.Locator(".compare-table__header-row--stuck")).ToBeVisibleAsync();

            // A resize moves the table, so each width scrolls into it again.
            await ExpectToFitAsync(page, nameof(ScrolledWithTheHeaderStuck_FitsAtEveryWidth), [PageRoot], () => ScrollIntoTheTableAsync(page));
        });
    }

    [Fact(Timeout = 300_000)]
    public async Task ClearCategoryDialog_FitsAtEveryWidth()
    {
        await using var session = await env.OpenAsGuestAsync(env.GuestId, ComparePage);

        await session.RunAsync(nameof(ClearCategoryDialog_FitsAtEveryWidth), async page =>
        {
            await ReadyAsync(page);

            // The dialog names the 80-character category. Only looked at, never confirmed: the guest is shared.
            await OpenDialogAsync(page, ClearCategory(page), "Clear category");
            await Expect(Dialog(page, "Clear category")).ToContainTextAsync(CompareLayoutEnvironment.LongCategoryName);

            await ExpectToFitAsync(page, nameof(ClearCategoryDialog_FitsAtEveryWidth), ["[role=dialog]"], () => OpenDialogAsync(page, ClearCategory(page), "Clear category"));
        });
    }

    [Fact(Timeout = 300_000)]
    public async Task ClearAllDialog_FitsAtEveryWidth()
    {
        await using var session = await env.OpenAsGuestAsync(env.GuestId, ComparePage);

        await session.RunAsync(nameof(ClearAllDialog_FitsAtEveryWidth), async page =>
        {
            await ReadyAsync(page);

            await OpenDialogAsync(page, ClearAll(page), "Clear all");

            await ExpectToFitAsync(page, nameof(ClearAllDialog_FitsAtEveryWidth), ["[role=dialog]"], () => OpenDialogAsync(page, ClearAll(page), "Clear all"));
        });
    }

    [Fact(Timeout = 300_000)]
    public async Task EmptyState_FitsAtEveryWidth()
    {
        // Clearing changes the list, so the test brings a guest of its own.
        var guestId = StorefrontEnvironment.NewGuestId();
        await env.SaveAsGuestAsync(guestId, [.. CompareLayoutEnvironment.StressItems]);

        await using var session = await env.OpenAsGuestAsync(guestId, ComparePage);

        await session.RunAsync(nameof(EmptyState_FitsAtEveryWidth), async page =>
        {
            await ReadyAsync(page);

            // Emptied from the page, so the empty state also offers to restore what was cleared.
            await OpenDialogAsync(page, ClearAll(page), "Clear all");
            await OkButton(Dialog(page, "Clear all")).ClickAsync();
            await Expect(EmptyState(page)).ToBeVisibleAsync();
            await Expect(RestoreProducts(page)).ToBeVisibleAsync();

            await ExpectToFitAsync(page, nameof(EmptyState_FitsAtEveryWidth), [PageRoot]);
        });
    }

    /// <summary>The page shows the stress category, in its own font, with nothing moving.</summary>
    private static async Task ReadyAsync(IPage page)
    {
        await Expect(ProductColumn(page, CompareLayoutEnvironment.LongName)).ToBeVisibleAsync(new() { Timeout = 60_000 });
        await Expect(UnavailableColumn(page)).ToBeVisibleAsync();
        await LayoutAudit.PrepareAsync(page);
    }

    /// <summary>Opens the dialog the button opens, unless it is open already: a resize may close it.</summary>
    private static async Task OpenDialogAsync(IPage page, ILocator button, string title)
    {
        var dialog = Dialog(page, title);
        if (!await dialog.IsVisibleAsync())
        {
            await button.ClickAsync();
        }

        await Expect(dialog).ToBeVisibleAsync();
    }

    /// <summary>Scrolls the window 400 px past the top of the table's header row.</summary>
    private static Task ScrollIntoTheTableAsync(IPage page) => page.EvaluateAsync("""
        () => {
          const header = document.querySelector('.compare-table__header-row');
          window.scrollTo(0, header.getBoundingClientRect().top + window.scrollY + 400);
        }
        """);

    /// <summary>Fails with every width and element at once: a layout fix is checked against the whole list.</summary>
    private static async Task ExpectToFitAsync(IPage page, string name, IReadOnlyList<string> scopes, Func<Task> restoreAsync = null)
    {
        var problems = await LayoutAudit.SweepAsync(page, name, scopes, restoreAsync);
        if (problems.Count > 0)
        {
            Assert.Fail($"The page does not keep its content inside at every width:{Environment.NewLine}{string.Join(Environment.NewLine, problems)}");
        }
    }
}
