using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VirtoCommerce.CatalogModule.Core.Model;
using VirtoCommerce.ProductComparison.Tests.Infrastructure;
using VirtoCommerce.ProductComparison.Tests.StorefrontE2E.Infrastructure;

namespace VirtoCommerce.ProductComparison.Tests.StorefrontE2E.CompareLayout;

/// <summary>
/// Group 4: the compare page's layout. One guest's list carries the stress content of QA's overflow audit in the
/// category the page opens on, whose name is 80 characters long: a 100-character product name, two names that differ
/// only at their ends, a name with no break in it, an unbroken SKU and model number, long notes, big amounts, a
/// product the catalog no longer has, and enough property rows to scroll the page. Seven more categories, one with no
/// break in its name, fill the tab row. The tests that only look share the guest; a test that changes the list brings
/// a guest of its own.
/// </summary>
public sealed class CompareLayoutEnvironment : StorefrontEnvironment
{
    public const string LongCategoryId = "e2e-layout-long-category";
    public const string LongCategoryName = "Multifunction Laser Printers, Copiers and Scanners for Small and Midsize Offices";
    public const string UnbrokenCategoryName = "CategoryWithAVeryLongUnbrokenNameABCDEFGHIJKLMNOPQRSTUVWXYZ012345678";

    public const string LongName = "Northwind Professional Color Laser Multifunction Printer with Duplex Scanning and Wireless Print Kit";
    public const string NorthName = "AGENT-TEST-Product-AcmeCorp-Regional-Laser-Printer-Model-North";
    public const string SouthName = "AGENT-TEST-Product-AcmeCorp-Regional-Laser-Printer-Model-South";
    public const string UnbrokenName = "ProductWithAVeryLongUnbrokenNameABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

    public const string UnbrokenSku = "SKU100200300400500600700800900";
    public const string UnbrokenModel = "MODEL-1234567890ABCDEFGHIJKLMNOPQRSTUVWXYZ0987654321";
    public const string LongLabel = "Maximum monthly duty cycle at the recommended print volume";
    public const string NotesLabel = "Notes";

    public const string LongNotes = "Ships with the starter toner, the duplex unit and the wireless kit installed; the extra paper tray, " +
        "the stand and the finisher are ordered separately and arrive in a second delivery within ten working days.";

    private const string DiscontinuedId = "e2e-layout-discontinued";

    private static readonly string[] _otherCategories =
    [
        UnbrokenCategoryName, "Toner and ink cartridges", "Label printers", "Document scanners", "Paper shredders", "Laminators", "Projectors",
    ];

    private static readonly string[] _propertyNames =
    [
        NotesLabel, "Model number", LongLabel, "Print speed", "Resolution", "Connectivity", "Paper capacity", "Duplex printing", "Weight", "Dimensions", "Warranty", "Color",
    ];

    /// <summary>The guest whose list the tests that only look open.</summary>
    public string GuestId { get; } = NewGuestId();

    /// <summary>One product of each other category, for a list of a test's own: "productId@categoryId".</summary>
    public static IReadOnlyList<string> OtherCategoryItems { get; } = _otherCategories.Select((_, i) => $"e2e-layout-item-{i}@e2e-layout-category-{i}").ToList();

    /// <summary>The stress category's products, in the order the page shows them, with the one the catalog no longer has last.</summary>
    public static IReadOnlyList<string> StressItems { get; } =
        [.. new[] { "long", "north", "south", "unbroken" }.Select(x => $"e2e-layout-{x}@{LongCategoryId}"), $"{DiscontinuedId}@{LongCategoryId}"];

    private protected override async Task SeedAsync(TestCatalog catalog)
    {
        var top = await catalog.AddCategoryAsync("e2e-layout-office", "Office equipment");
        var category = await catalog.AddCategoryAsync(LongCategoryId, LongCategoryName, top);
        for (var i = 0; i < _otherCategories.Length; i++)
        {
            await catalog.AddCategoryAsync($"e2e-layout-category-{i}", _otherCategories[i], top);
        }

        // The table shows a row per property: the long ones, plus ten more so that the page scrolls at every width.
        var properties = new Dictionary<string, Property>();
        foreach (var name in _propertyNames)
        {
            properties[name] = await catalog.AddPropertyAsync(name);
        }

        await AddStressProductAsync(catalog, properties, "e2e-layout-long", LongName, category, 12_345_678.99m, sku: null, model: "MFP-4100", notes: LongNotes);
        await AddStressProductAsync(catalog, properties, "e2e-layout-north", NorthName, category, 9_876_543.21m, sku: UnbrokenSku, model: UnbrokenModel, notes: "Ships from the north warehouse.");
        await AddStressProductAsync(catalog, properties, "e2e-layout-south", SouthName, category, 9_876_543.29m, sku: null, model: "MFP-4100S", notes: LongNotes);
        await AddStressProductAsync(catalog, properties, "e2e-layout-unbroken", UnbrokenName, category, 1_000_000m, sku: null, model: "MFP-4100U", notes: "Ships from the south warehouse.");

        await SaveAsGuestAsync(GuestId, [.. StressItems, .. OtherCategoryItems]);
    }

    private static Task AddStressProductAsync(TestCatalog catalog, Dictionary<string, Property> properties, string id, string name, Category category, decimal price, string sku, string model, string notes)
        => catalog.AddProductAsync(id, name, category, price, product =>
        {
            if (sku != null)
            {
                product.Code = sku;
            }

            product.Properties =
            [
                TestCatalog.WithValue(properties[NotesLabel], notes),
                TestCatalog.WithValue(properties["Model number"], model),
                TestCatalog.WithValue(properties[LongLabel], $"{id.Length * 1_000:N0} pages"),
                TestCatalog.WithValue(properties["Print speed"], $"{id.Length + 30} pages per minute"),
                TestCatalog.WithValue(properties["Resolution"], "1200 x 1200 dpi"),
                TestCatalog.WithValue(properties["Connectivity"], "USB 2.0, Ethernet, Wi-Fi Direct"),
                TestCatalog.WithValue(properties["Paper capacity"], $"{id.Length * 10} sheets"),
                TestCatalog.WithValue(properties["Duplex printing"], "Automatic"),
                TestCatalog.WithValue(properties["Weight"], $"{id.Length + 10} kg"),
                TestCatalog.WithValue(properties["Dimensions"], "420 x 390 x 330 mm"),
                TestCatalog.WithValue(properties["Warranty"], $"{id.Length % 3 + 1} years"),
                TestCatalog.WithValue(properties["Color"], "Black and white"),
            ];
        });
}
