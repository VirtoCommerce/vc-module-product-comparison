using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using VirtoCommerce.CatalogModule.Core.Model;
using VirtoCommerce.CatalogModule.Core.Outlines;
using VirtoCommerce.CoreModule.Core.Currency;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.ProductComparison.Tests.ComponentTests.Infrastructure;
using VirtoCommerce.Seo.Core.Models;
using VirtoCommerce.StoreModule.Core.Model;
using VirtoCommerce.StoreModule.Core.Services;
using VirtoCommerce.XCatalog.Core.Models;
using VirtoCommerce.XCatalog.Core.Queries;
using ProductPrice = VirtoCommerce.Xapi.Core.Models.ProductPrice;

namespace VirtoCommerce.ProductComparison.Tests.StorefrontE2E.Infrastructure;

/// <summary>
/// The catalog the storefront shows: a few products in a two-level category tree, held in memory and served at the
/// X-Catalog query level. It answers the two queries every product and category on these pages goes through:
/// <see cref="LoadProductsQuery"/> (the product page's <c>product</c> field and the compare list's <c>product</c>
/// field, which the module batches) and <see cref="LoadCategoryQuery"/> (the compare list's category tabs). In
/// production X-Catalog answers them from the search index; here the same models come from memory, and X-Catalog's
/// own graph types turn them into what the storefront reads: names, prices, availability, breadcrumbs.
/// </summary>
/// <remarks>
/// Like the index, it only returns what the store's catalog holds and what it was asked for: an id it doesn't know
/// (a product deleted from the catalog) is simply missing from the response, which is how a compared product becomes
/// "no longer available". A query for a store that doesn't exist fails, so a wrong store id in a module's query shows.
/// </remarks>
internal sealed class StorefrontCatalog(IStoreService storeService, ICurrencyService currencyService) :
    IRequestHandler<LoadProductsQuery, LoadProductResponse>,
    IRequestHandler<LoadCategoryQuery, LoadCategoryResponse>
{
    /// <summary>The catalog every harness store reports (<see cref="TestStoreService"/>).</summary>
    public const string CatalogId = "catalog";

    private readonly ConcurrentDictionary<string, Category> _categories = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, CatalogProduct> _products = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, decimal> _prices = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// A category with an SEO entry for the store, as the catalog module saves one: X-Catalog builds a product's
    /// breadcrumbs only from categories that have a semantic URL, and the plugin takes a product's compare category from
    /// those breadcrumbs.
    /// </summary>
    public Category AddCategory(string id, string name, Category parent = null)
    {
        var category = new Category
        {
            Id = id,
            Name = name,
            CatalogId = CatalogId,
            ParentId = parent?.Id,
            IsActive = true,
            SeoInfos = [Seo(id, name, "Category")],
            Properties = [],
        };
        category.Outlines = [new Outline { Items = [CatalogItem(), .. CategoryItems(category)] }];

        _categories[id] = category;

        return category;
    }

    /// <summary>
    /// A plain product (no variations, no configuration, no inventory tracking) in <paramref name="category"/>, with a
    /// list price in every currency the store asks for. It has no SEO entry of its own, so its links are
    /// <c>/product/{id}</c>, as for any product without a semantic URL.
    /// </summary>
    public CatalogProduct AddProduct(string id, string name, Category category, decimal price)
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
            Properties = [],
            Images = [],
            Assets = [],
            Reviews = [],
            SeoInfos = [],
        };

        // The catalog module's outline for a product: the catalog, the category chain, then the product itself.
        product.Outlines = [new Outline { Items = [CatalogItem(), .. CategoryItems(category), new OutlineItem { Id = id, SeoObjectType = "CatalogProduct", Name = name }] }];

        _products[id] = product;
        _prices[id] = price;

        return product;
    }

    public async Task<LoadProductResponse> Handle(LoadProductsQuery request, CancellationToken cancellationToken)
    {
        var store = await GetStoreAsync(request.StoreId);
        var currencyCode = request.CurrencyCode ?? store.DefaultCurrency;
        var currency = (await currencyService.GetAllCurrenciesAsync()).Single(x => x.Code.EqualsIgnoreCase(currencyCode));

        var products = request.ObjectIds
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Select(id => _products.GetValueOrDefault(id))
            .Where(product => product != null && product.CatalogId.EqualsIgnoreCase(store.Catalog))
            .Select(product => ToExpProduct(product, _prices[product.Id], currency))
            .ToList();

        return new LoadProductResponse(products);
    }

    public async Task<LoadCategoryResponse> Handle(LoadCategoryQuery request, CancellationToken cancellationToken)
    {
        var store = await GetStoreAsync(request.StoreId);

        var categories = request.ObjectIds
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Select(id => _categories.GetValueOrDefault(id))
            .Where(category => category != null && category.CatalogId.EqualsIgnoreCase(store.Catalog))
            .Select(category => new ExpCategory { Category = (Category)category.Clone() })
            .ToList();

        return new LoadCategoryResponse(categories);
    }

    private async Task<Store> GetStoreAsync(string storeId)
        => await storeService.GetByIdAsync(storeId) ?? throw new InvalidOperationException($"A catalog query for a store that doesn't exist: '{storeId}'.");

    // A copy per request, as the index deserializes one: X-Catalog's resolvers may cache on the instance.
    private static ExpProduct ToExpProduct(CatalogProduct product, decimal price, Currency currency)
    {
        var result = AbstractTypeFactory<ExpProduct>.TryCreateInstance();
        result.IndexedProduct = (CatalogProduct)product.Clone();
        result.AllPrices =
        [
            new ProductPrice(currency)
            {
                ProductId = product.Id,
                ListPrice = new Money(price, currency),
                SalePrice = new Money(price, currency),
            },
        ];

        return result;
    }

    private IEnumerable<OutlineItem> CategoryItems(Category category)
    {
        var chain = new List<Category>();
        for (var current = category; current != null; current = current.ParentId == null ? null : _categories[current.ParentId])
        {
            chain.Insert(0, current);
        }

        return chain.Select(x => new OutlineItem { Id = x.Id, SeoObjectType = "Category", Name = x.Name, SeoInfos = x.SeoInfos });
    }

    private static OutlineItem CatalogItem() => new() { Id = CatalogId, SeoObjectType = "Catalog", Name = "Catalog" };

    private static SeoInfo Seo(string id, string name, string objectType) => new()
    {
        SemanticUrl = id,
        Name = name,
        ObjectId = id,
        ObjectType = objectType,
        StoreId = ProductComparisonTestContext.StoreId,
        LanguageCode = StorefrontServices.DefaultCulture,
        IsActive = true,
    };
}
