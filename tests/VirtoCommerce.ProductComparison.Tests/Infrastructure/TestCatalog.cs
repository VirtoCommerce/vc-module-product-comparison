using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using VirtoCommerce.CatalogModule.Core.Model;
using VirtoCommerce.CatalogModule.Core.Model.Configuration;
using VirtoCommerce.CatalogModule.Core.Services;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.PricingModule.Core.Model;
using VirtoCommerce.PricingModule.Core.Services;
using VirtoCommerce.ProductComparison.Tests.ComponentTests.Infrastructure;
using VirtoCommerce.SearchModule.Core.Model;
using VirtoCommerce.SearchModule.Core.Services;
using VirtoCommerce.Seo.Core.Models;

namespace VirtoCommerce.ProductComparison.Tests.Infrastructure;

/// <summary>
/// A catalog, set up as an administrator sets one up: the store's catalog, a pricelist assigned to the store,
/// categories, products and product configurations saved through the catalog module's services, prices through the
/// pricing module's, then the search index built by the search module's indexing manager. X-Catalog and X-Cart read all
/// of it the way they do in production: products and categories from the index, the rest from the modules.
/// </summary>
internal sealed class TestCatalog(IServiceProvider services)
{
    /// <summary>The catalog of the harness's stores.</summary>
    public const string CatalogId = "catalog";

    public const string PricelistId = "e2e-prices";

    private const string Currency = ProductComparisonTestContext.CurrencyCode;

    /// <summary>The catalog itself, and the store's pricelist: what has to exist before a category or a price.</summary>
    public async Task InitializeAsync()
    {
        await RunAsync<ICatalogService>(catalogs => catalogs.SaveChangesAsync(
        [
            new Catalog
            {
                Id = CatalogId,
                Name = "E2E catalog",
                Languages = [new CatalogLanguage { LanguageCode = ProductComparisonTestContext.CultureName, IsDefault = true }],
            },
        ]));

        await RunAsync<IPricelistService>(pricelists => pricelists.SaveChangesAsync([new Pricelist { Id = PricelistId, Name = "E2E prices", Currency = Currency }]));
        await RunAsync<IPricelistAssignmentService>(assignments => assignments.SaveChangesAsync(
        [
            new PricelistAssignment
            {
                Id = $"{PricelistId}-assignment",
                Name = "E2E prices for the store",
                PricelistId = PricelistId,
                StoreId = ProductComparisonTestContext.StoreId,
                Priority = 1,
            },
        ]));
    }

    /// <summary>
    /// A category with an SEO entry for the store, as an administrator gives one: X-Catalog builds a product's
    /// breadcrumbs only from categories that have a semantic URL, and the plugin takes a product's compare category
    /// from those breadcrumbs.
    /// </summary>
    public async Task<Category> AddCategoryAsync(string id, string name, Category parent = null)
    {
        var category = new Category
        {
            Id = id,
            Code = id,
            Name = name,
            CatalogId = CatalogId,
            ParentId = parent?.Id,
            IsActive = true,
            SeoInfos = [Seo(id, name, "Category")],
        };

        await RunAsync<ICategoryService>(categories => categories.SaveChangesAsync([category]));

        return category;
    }

    /// <summary>
    /// A product property of the catalog, short text, as the layout group's stress rows need: a code-like name, as the
    /// catalog requires, and <paramref name="label"/> as its display name, which the storefront shows.
    /// </summary>
    public async Task<Property> AddPropertyAsync(string label)
    {
        var name = Regex.Replace(label, "[^A-Za-z0-9]+", "_").Trim('_');
        var property = new Property
        {
            Id = $"e2e-property-{name.ToLowerInvariant()}",
            Name = name,
            CatalogId = CatalogId,
            Type = PropertyType.Product,
            ValueType = PropertyValueType.ShortText,
            DisplayNames = [new PropertyDisplayName { LanguageCode = ProductComparisonTestContext.CultureName, Name = label }],
        };

        await RunAsync<IPropertyService>(properties => properties.SaveChangesAsync([property]));

        return property;
    }

    /// <summary>
    /// A plain product (no variations, no configuration, no inventory tracking) in <paramref name="category"/>, with a
    /// list price on the store's pricelist. It has no SEO entry of its own, so its links are <c>/product/{id}</c>, as
    /// for any product without a semantic URL. <paramref name="configure"/> sets more before the save: a code, property
    /// values.
    /// </summary>
    public async Task<CatalogProduct> AddProductAsync(string id, string name, Category category, decimal price, Action<CatalogProduct> configure = null)
    {
        var product = new CatalogProduct
        {
            Id = id,
            Code = $"SKU-{id}",
            Name = name,
            CatalogId = CatalogId,
            CategoryId = category.Id,
            ProductType = "Physical",
            IsActive = true,
            IsBuyable = true,
            TrackInventory = false,
            MinQuantity = 1,
            PackSize = 1,
        };
        configure?.Invoke(product);

        await RunAsync<IItemService>(items => items.SaveChangesAsync([product]));
        await RunAsync<IPriceService>(prices => prices.SaveChangesAsync([new Price { PricelistId = PricelistId, ProductId = id, List = price, MinQuantity = 1, Currency = Currency }]));

        return product;
    }

    /// <summary>
    /// The product's configuration, active, with text sections of the given ids and names, as an administrator builds
    /// one on a configurable product: X-Cart's product configuration query reads it.
    /// </summary>
    public Task AddConfigurationAsync(string productId, params (string Id, string Name)[] sections)
        => RunAsync<IProductConfigurationService>(configurations => configurations.SaveChangesAsync(
        [
            new ProductConfiguration
            {
                ProductId = productId,
                IsActive = true,
                Sections = sections
                    .Select((section, i) => new ProductConfigurationSection { Id = section.Id, Name = section.Name, Type = "Text", AllowCustomText = true, DisplayOrder = i })
                    .ToList(),
            },
        ]));

    /// <summary>A value of <paramref name="property"/> for a product, as the product's properties carry it.</summary>
    public static Property WithValue(Property property, string value)
    {
        var withValue = property.CloneTyped();
        withValue.Values = [new PropertyValue { PropertyId = property.Id, PropertyName = property.Name, Value = value, ValueType = property.ValueType }];

        return withValue;
    }

    /// <summary>
    /// Builds the products' and the categories' index from what is saved, as a full rebuild from the admin does:
    /// what the storefront searches and loads products and categories from.
    /// </summary>
    public async Task IndexAsync()
    {
        foreach (var documentType in new[] { KnownDocumentTypes.Category, KnownDocumentTypes.Product })
        {
            await RunAsync<IIndexingManager>(indexing => indexing.IndexAsync(
                new IndexingOptions { DocumentType = documentType, DeleteExistingIndex = true, BatchSize = 50 },
                _ => { },
                CancellationToken.None));
        }
    }

    // Each call in a scope of its own, as a request runs: the modules' DbContexts are scoped.
    private async Task RunAsync<TService>(Func<TService, Task> work) where TService : notnull
    {
        using var scope = services.CreateScope();
        await work(scope.ServiceProvider.GetRequiredService<TService>());
    }

    private static SeoInfo Seo(string id, string name, string objectType) => new()
    {
        SemanticUrl = id,
        Name = name,
        ObjectId = id,
        ObjectType = objectType,
        StoreId = ProductComparisonTestContext.StoreId,
        LanguageCode = ProductComparisonTestContext.CultureName,
        IsActive = true,
    };
}
