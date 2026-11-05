using System.Text.RegularExpressions;
using Microsoft.Playwright;

namespace VirtoCommerce.ProductComparison.Tests.StorefrontE2E.Infrastructure;

/// <summary>
/// The compare plugin's controls and the storefront's around them, found by role and label as a buyer finds them, in
/// the English the plugin's and the storefront's en locales give them.
/// </summary>
internal static class CompareControls
{
    /// <summary>The plugin's page.</summary>
    public const string ComparePage = "/compare";

    /// <summary>Where the plugin remembers a guest's list in the test store, for the merge after sign-in.</summary>
    public const string GuestListIdKey = $"vc-product-comparison:guest-list-id:{InProcessBackend.StoreId}";

    /// <summary>The product page's price block, in the page's sidebar: its action row holds the compare toggle.</summary>
    public static ILocator PriceBlock(IPage page) => page.GetByRole(AriaRole.Complementary, new() { Name = "Sidebar" });

    /// <summary>The compare toggle in the price block while the product is not in the list.</summary>
    public static ILocator AddToCompare(IPage page) => PriceBlock(page).GetByRole(AriaRole.Button, new() { Name = "Add to Compare", Exact = true });

    /// <summary>The same toggle once the product is in the list.</summary>
    public static ILocator RemoveFromCompare(IPage page) => PriceBlock(page).GetByRole(AriaRole.Button, new() { Name = "Remove from Compare", Exact = true });

    /// <summary>The price block's Share button, in the same action row as the compare toggle: the row has rendered.</summary>
    public static ILocator Share(IPage page) => PriceBlock(page).GetByRole(AriaRole.Button, new() { Name = "Share", Exact = true });

    /// <summary>The header's Compare link, whatever its count.</summary>
    public static ILocator HeaderCompareLink(IPage page) => MainMenu(page).GetByRole(AriaRole.Link, new() { NameRegex = new Regex(@"^(\d+\s*)?Compare$") });

    /// <summary>The header's Compare link showing <paramref name="count"/> on its badge; no badge for 0.</summary>
    public static ILocator HeaderCompareLink(IPage page, int count) => MainMenu(page).GetByRole(AriaRole.Link, new()
    {
        NameRegex = count == 0 ? new Regex("^Compare$") : new Regex($@"^{count}\s*Compare$"),
    });

    /// <summary>The storefront's toasts.</summary>
    public static ILocator Toasts(IPage page) => page.GetByRole(AriaRole.Region, new() { Name = "Notifications" });

    public static ILocator CompareTitle(IPage page) => page.GetByRole(AriaRole.Heading, new() { Level = 1, Name = "Compare products", Exact = true });

    /// <summary>A category tab of the compare page: the category's name and its count.</summary>
    public static ILocator CategoryTab(IPage page, string name, int count)
        => page.GetByRole(AriaRole.Button, new() { NameRegex = new Regex($@"^{Regex.Escape(name)}\s*{count}$") });

    /// <summary>A product's column of the compare table, by the product's name.</summary>
    public static ILocator ProductColumn(IPage page, string name) => page.GetByRole(AriaRole.Columnheader, new() { Name = name, Exact = true });

    /// <summary>The column of an item whose product the catalog no longer has.</summary>
    public static ILocator UnavailableColumn(IPage page) => ProductColumn(page, "No longer available");

    /// <summary>A column's remove button.</summary>
    public static ILocator RemoveButton(ILocator column) => column.GetByRole(AriaRole.Button, new() { Name = "Remove from compare", Exact = true });

    public static ILocator EmptyState(IPage page) => page.GetByRole(AriaRole.Heading, new() { Level = 3, Name = "Your compare list is empty", Exact = true });

    public static ILocator ClearAll(IPage page) => page.GetByRole(AriaRole.Button, new() { Name = "Clear all", Exact = true });

    public static ILocator RestoreProducts(IPage page) => page.GetByRole(AriaRole.Button, new() { Name = "Restore products", Exact = true });

    public static ILocator Dialog(IPage page, string title) => page.GetByRole(AriaRole.Dialog, new() { Name = title });

    public static ILocator OkButton(ILocator dialog) => dialog.GetByRole(AriaRole.Button, new() { Name = "OK", Exact = true });

    private static ILocator MainMenu(IPage page) => page.GetByRole(AriaRole.Navigation, new() { Name = "Main menu" });
}
