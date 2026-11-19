using System.Threading.Tasks;
using VirtoCommerce.ProductComparison.Tests.Infrastructure;

namespace VirtoCommerce.ProductComparison.Tests.StorefrontE2E.Infrastructure;

/// <summary>
/// The products every group's storefront shows: two second-level categories under two top ones, so the plugin's
/// compare category (the product's category at depth 2) is a real choice, and three plain products. A product id the
/// catalog never had (<see cref="DiscontinuedProductId"/>) stands for a product deleted from the catalog.
/// </summary>
internal static class E2ECatalog
{
    public const string PrintersId = "e2e-printers";
    public const string LaserPrintersId = "e2e-laser-printers";
    public const string LaserPrintersName = "Laser printers";
    public const string PaperId = "e2e-paper";
    public const string CopyPaperId = "e2e-copy-paper";
    public const string CopyPaperName = "Copy paper";

    public const string LaserJet100Id = "e2e-laserjet-100";
    public const string LaserJet100Name = "LaserJet 100";
    public const string LaserJet200Id = "e2e-laserjet-200";
    public const string LaserJet200Name = "LaserJet 200";
    public const string CopyPaperA4Id = "e2e-copy-paper-a4";
    public const string CopyPaperA4Name = "Copy paper A4";

    public const string DiscontinuedProductId = "e2e-discontinued";

    public static async Task SeedAsync(TestCatalog catalog)
    {
        var printers = await catalog.AddCategoryAsync(PrintersId, "Printers");
        var laserPrinters = await catalog.AddCategoryAsync(LaserPrintersId, LaserPrintersName, printers);
        var paper = await catalog.AddCategoryAsync(PaperId, "Paper");
        var copyPaper = await catalog.AddCategoryAsync(CopyPaperId, CopyPaperName, paper);

        await catalog.AddProductAsync(LaserJet100Id, LaserJet100Name, laserPrinters, 199m);
        await catalog.AddProductAsync(LaserJet200Id, LaserJet200Name, laserPrinters, 249m);
        await catalog.AddProductAsync(CopyPaperA4Id, CopyPaperA4Name, copyPaper, 9.99m);
    }

    /// <summary>A product's page: <c>/product/{id}</c>, the link the storefront makes for a product without a semantic URL.</summary>
    public static string ProductPage(string productId) => $"/product/{productId}";

    /// <summary>A category's page: <c>/category/{id}</c>, the storefront's route for a category by id, with its product cards.</summary>
    public static string CategoryPage(string categoryId) => $"/category/{categoryId}";
}
