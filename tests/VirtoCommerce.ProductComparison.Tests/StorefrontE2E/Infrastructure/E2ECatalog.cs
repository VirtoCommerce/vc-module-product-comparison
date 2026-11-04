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

    public static void Seed(StorefrontCatalog catalog)
    {
        var printers = catalog.AddCategory(PrintersId, "Printers");
        var laserPrinters = catalog.AddCategory(LaserPrintersId, LaserPrintersName, printers);
        var paper = catalog.AddCategory(PaperId, "Paper");
        var copyPaper = catalog.AddCategory(CopyPaperId, CopyPaperName, paper);

        catalog.AddProduct(LaserJet100Id, LaserJet100Name, laserPrinters, 199m);
        catalog.AddProduct(LaserJet200Id, LaserJet200Name, laserPrinters, 249m);
        catalog.AddProduct(CopyPaperA4Id, CopyPaperA4Name, copyPaper, 9.99m);
    }

    /// <summary>A product's page: <c>/product/{id}</c>, the link the storefront makes for a product without a semantic URL.</summary>
    public static string ProductPage(string productId) => $"/product/{productId}";
}
