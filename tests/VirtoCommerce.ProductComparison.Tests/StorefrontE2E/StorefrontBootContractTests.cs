using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;
using FluentAssertions;
using VirtoCommerce.ProductComparison.Tests.StorefrontE2E.Infrastructure;
using Xunit;

namespace VirtoCommerce.ProductComparison.Tests.StorefrontE2E;

/// <summary>
/// The storefront's boot and the compare plugin's documents, without a browser: the in-process backend answers the
/// storefront's OWN operations (loaded from the clone, fragments inlined) and the plugin's OWN operations (loaded
/// from <c>StorefrontApp</c>) for a guest and a signed-in buyer. A failure here is a backend-side gap (a missing root
/// field, a package skew, a catalog the product page can't read), and reads as a GraphQL error in seconds rather than
/// as a blank page in the browser tests.
/// </summary>
[Trait("Category", "StorefrontE2E")]
[Trait("Category", "IntegrationTest")]
public sealed class StorefrontBootContractTests
{
    /// <summary>What the in-process backend reports as installed; the storefront gates documents on this list.</summary>
    private static readonly string[] InstalledModules = [StorefrontPlugin.ModuleId, "VirtoCommerce.XFrontend"];

    [Fact(Timeout = 300_000)]
    public async Task Boot_AnswersTheStorefrontsAndThePluginsDocuments()
    {
        // The documents are files of a clone; a build carries them only inside its bundle.
        if (StorefrontLocation.Current.SourceDirectory == null)
        {
            throw new InvalidOperationException(
                $"This test reads the storefront's .graphql files, which a build does not ship: set {StorefrontLocation.SourceVariable} to the clone the build came from.");
        }

        await using var backend = await InProcessBackend.StartAsync();
        await E2ECatalog.SeedAsync(backend.Catalog);
        await backend.Catalog.IndexAsync();
        var guestId = Guid.NewGuid().ToString();

        using var guest = new HttpClient { BaseAddress = new Uri(backend.Url) };

        // 1. InitializeApplication: the store, with the module's public settings the plugin and its "when" read.
        var initialize = await PostAsync(guest, "InitializeApplication", Storefront("store/queries/initializeApplication/initializeApplication.graphql"),
            new { domain = "127.0.0.1" });
        var settings = initialize.GetProperty("data").GetProperty("store").GetProperty("settings").GetProperty("modules").EnumerateArray()
            .Single(x => x.GetProperty("moduleId").GetString() == StorefrontPlugin.ModuleId)
            .GetProperty("settings").EnumerateArray()
            .ToDictionary(x => x.GetProperty("name").GetString(), x => x.GetProperty("value"));
        settings["ProductComparison.Enabled"].GetBoolean().Should().BeTrue();
        settings["ProductComparison.MaxItems"].GetInt32().Should().Be(5);
        settings["ProductComparison.AnonymousListEnabled"].GetBoolean().Should().BeTrue();

        // 2. GetStorePlugins: the module's plugin, found by the platform's own discovery, with its declared contributions.
        var plugins = await PostAsync(guest, "GetStorePlugins", Storefront("store/queries/getStorePlugins/getStorePlugins.graphql"),
            new { domain = "127.0.0.1" });
        var plugin = plugins.GetProperty("data").GetProperty("store").GetProperty("plugins").EnumerateArray().Single();
        plugin.GetProperty("id").GetString().Should().Be("product-comparison");
        plugin.GetProperty("contributions").ToString().Should().Contain("ProductComparison.Enabled");

        // 3. GetPageContext: a guest keeps the id the storefront sent; that id is the compare list's userId.
        var pageContext = await PostAsync(guest, "GetPageContext", Storefront("pageContext/queries/getPageContext/getPageContextQuery.graphql"),
            new { userId = guestId, domain = "127.0.0.1", permalink = "compare", cultureName = "en-US" });
        var user = pageContext.GetProperty("data").GetProperty("pageContext").GetProperty("user");
        user.GetProperty("id").GetString().Should().Be(guestId);
        user.GetProperty("userName").GetString().Should().Be("Anonymous");

        // 4. GetProduct: the product page's query, with the category breadcrumbs the plugin takes the compare category from.
        var product = (await PostAsync(guest, "GetProduct", Storefront("catalog/queries/getProduct/getProductQuery.graphql"),
            new { storeId = InProcessBackend.StoreId, currencyCode = "USD", cultureName = "en-US", id = E2ECatalog.LaserJet100Id }))
            .GetProperty("data").GetProperty("product");
        product.GetProperty("name").GetString().Should().Be(E2ECatalog.LaserJet100Name);
        product.GetProperty("breadcrumbs").EnumerateArray()
            .Where(x => x.GetProperty("typeName").GetString() == "Category")
            .Select(x => x.GetProperty("itemId").GetString())
            .Should().Equal(E2ECatalog.PrintersId, E2ECatalog.LaserPrintersId);
        product.GetProperty("availabilityData").GetProperty("isBuyable").GetBoolean().Should().BeTrue();

        // 5. The plugin's own operations: the guest saves a product, and the compare page reads it back with its
        //    category's name and the product's details.
        var saved = await PostAsync(guest, "SaveProductsToCompareList", Plugin("mutations/saveProductsToCompareList/saveProductsToCompareListMutation.graphql"),
            new
            {
                command = new
                {
                    storeId = InProcessBackend.StoreId,
                    userId = guestId,
                    products = new[] { new { productId = E2ECatalog.LaserJet100Id, categoryId = E2ECatalog.LaserPrintersId } },
                },
            });
        saved.GetProperty("data").GetProperty("saveProductsToCompareList").GetProperty("itemsCount").GetInt32().Should().Be(1);

        var compare = (await PostAsync(guest, "CompareProducts", Plugin("queries/compareProducts/compareProductsQuery.graphql"),
            new { storeId = InProcessBackend.StoreId, userId = guestId, cultureName = "en-US", currencyCode = "USD" }))
            .GetProperty("data").GetProperty("compareList");
        var category = compare.GetProperty("categories").EnumerateArray().Single();
        category.GetProperty("category").GetProperty("name").GetString().Should().Be(E2ECatalog.LaserPrintersName);
        var item = category.GetProperty("items").EnumerateArray().Single().GetProperty("product");
        item.GetProperty("name").GetString().Should().Be(E2ECatalog.LaserJet100Name);
        item.GetProperty("price").GetProperty("actual").GetProperty("amount").GetDecimal().Should().Be(199m);

        // 6. A signed-in buyer's page context: their account, which is what turns the storefront "authenticated".
        var buyerId = "e2e-boot-buyer";
        await backend.Context.AddBuyerAsync(buyerId);
        using var buyer = new HttpClient { BaseAddress = new Uri(backend.Url) };
        buyer.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", (await backend.Tokens.IssueAsync(buyerId)).AccessToken);

        var buyerContext = await PostAsync(buyer, "GetPageContext", Storefront("pageContext/queries/getPageContext/getPageContextQuery.graphql"),
            new { userId = buyerId, domain = "127.0.0.1", permalink = "compare", cultureName = "en-US" });
        var buyerUser = buyerContext.GetProperty("data").GetProperty("pageContext").GetProperty("user");
        buyerUser.GetProperty("id").GetString().Should().Be(buyerId);
        buyerUser.GetProperty("userName").GetString().Should().Be($"{buyerId}@test.com");

        // A signed-in storefront also loads the account's address book on every page, for its ship-to selector.
        await PostAsync(buyer, "GetCurrentUserAddresses", Storefront("account/queries/getCurrentCustomerAddresses/getCurrentCustomerAddresses.graphql"), new { first = 10 });

        // 7. The shell's other boot operations, which must at least validate and answer.
        await PostAsync(guest, "GetMenu", Storefront("common/queries/getMenu/getMenu.graphql"),
            new { storeId = InProcessBackend.StoreId, cultureName = "en-US", name = "footer-links" });
        await PostAsync(guest, "GetShortCart", Storefront("cart/queries/getShortCart/getShortCartQuery.graphql"),
            new { storeId = InProcessBackend.StoreId, userId = guestId, currencyCode = "USD", cultureName = "en-US" });
    }

    private static string Storefront(string path) => StorefrontDocuments.LoadStorefront(path, InstalledModules);

    private static string Plugin(string path) => StorefrontDocuments.LoadPlugin(path);

    private static async Task<JsonElement> PostAsync(HttpClient client, string operationName, string document, object variables)
    {
        var response = await GraphQlHttp.PostAsync(client, new { operationName, query = document, variables });
        var json = await response.Content.ReadAsStringAsync();

        response.IsSuccessStatusCode.Should().BeTrue($"{operationName} must be accepted: {json}");
        json.Should().NotContain("\"errors\"", $"{operationName} must answer without errors");

        return JsonDocument.Parse(json).RootElement.Clone();
    }
}
