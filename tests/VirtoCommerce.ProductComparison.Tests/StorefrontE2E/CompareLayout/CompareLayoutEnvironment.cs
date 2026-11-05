using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VirtoCommerce.CatalogModule.Core.Model;
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

    /// <summary>The guest whose list the tests that only look open.</summary>
    public string GuestId { get; } = NewGuestId();

    /// <summary>One product of each other category, for a list of a test's own: "productId@categoryId".</summary>
    public static IReadOnlyList<string> OtherCategoryItems { get; } = _otherCategories.Select((_, i) => $"e2e-layout-item-{i}@e2e-layout-category-{i}").ToList();

    /// <summary>The stress category's products, in the order the page shows them, with the one the catalog no longer has last.</summary>
    public static IReadOnlyList<string> StressItems { get; } =
        [.. new[] { "long", "north", "south", "unbroken" }.Select(x => $"e2e-layout-{x}@{LongCategoryId}"), $"{DiscontinuedId}@{LongCategoryId}"];

    private protected override async Task SeedAsync(StorefrontCatalog catalog)
    {
        var top = catalog.AddCategory("e2e-layout-office", "Office equipment");
        var category = catalog.AddCategory(LongCategoryId, LongCategoryName, top);
        for (var i = 0; i < _otherCategories.Length; i++)
        {
            catalog.AddCategory($"e2e-layout-category-{i}", _otherCategories[i], top);
        }

        AddStressProduct(catalog, "e2e-layout-long", LongName, category, 12_345_678.99m, sku: null, model: "MFP-4100", notes: LongNotes);
        AddStressProduct(catalog, "e2e-layout-north", NorthName, category, 9_876_543.21m, sku: UnbrokenSku, model: UnbrokenModel, notes: "Ships from the north warehouse.");
        AddStressProduct(catalog, "e2e-layout-south", SouthName, category, 9_876_543.29m, sku: null, model: "MFP-4100S", notes: LongNotes);
        AddStressProduct(catalog, "e2e-layout-unbroken", UnbrokenName, category, 1_000_000m, sku: null, model: "MFP-4100U", notes: "Ships from the south warehouse.");

        await SaveAsGuestAsync(GuestId, [.. StressItems, .. OtherCategoryItems]);
    }

    // The table shows a row per property: the long ones, plus ten more so that the page scrolls at every width.
    private static void AddStressProduct(StorefrontCatalog catalog, string id, string name, Category category, decimal price, string sku, string model, string notes)
    {
        var product = catalog.AddProduct(id, name, category, price);
        if (sku != null)
        {
            product.Code = sku;
        }

        product.Properties =
        [
            TextProperty(NotesLabel, notes),
            TextProperty("Model number", model),
            TextProperty(LongLabel, $"{id.Length * 1_000:N0} pages"),
            TextProperty("Print speed", $"{id.Length + 30} pages per minute"),
            TextProperty("Resolution", "1200 x 1200 dpi"),
            TextProperty("Connectivity", "USB 2.0, Ethernet, Wi-Fi Direct"),
            TextProperty("Paper capacity", $"{id.Length * 10} sheets"),
            TextProperty("Duplex printing", "Automatic"),
            TextProperty("Weight", $"{id.Length + 10} kg"),
            TextProperty("Dimensions", "420 x 390 x 330 mm"),
            TextProperty("Warranty", $"{id.Length % 3 + 1} years"),
            TextProperty("Color", "Black and white"),
        ];
    }

    private static Property TextProperty(string name, string value) => new()
    {
        Id = $"e2e-layout-property-{name.ToLowerInvariant().Replace(' ', '-')}",
        Name = name,
        Type = PropertyType.Product,
        ValueType = PropertyValueType.ShortText,
        Values = [new PropertyValue { PropertyName = name, Value = value, ValueType = PropertyValueType.ShortText }],
    };
}
