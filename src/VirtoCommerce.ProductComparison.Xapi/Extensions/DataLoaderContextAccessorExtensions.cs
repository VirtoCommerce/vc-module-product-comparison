using System.Collections.Generic;
using System.Linq;
using GraphQL;
using GraphQL.DataLoader;
using VirtoCommerce.CoreModule.Core.Currency;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.ProductComparison.Core.Models;
using VirtoCommerce.ProductComparison.Xapi.Aggregates;
using VirtoCommerce.ProductComparison.Xapi.Models;
using VirtoCommerce.Xapi.Core.Extensions;
using VirtoCommerce.XCart.Core.Models;
using VirtoCommerce.XCart.Core.Queries;
using VirtoCommerce.XCatalog.Core.Extensions;
using VirtoCommerce.XCatalog.Core.Models;
using VirtoCommerce.XCatalog.Core.Queries;

namespace VirtoCommerce.ProductComparison.Xapi.Extensions;

public static class DataLoaderContextAccessorExtensions
{
    public static IDataLoaderResult<ExpProduct> LoadCompareListProduct(
        this IDataLoaderContextAccessor dataLoader,
        IResolveFieldContext<CompareListItem> context,
        ICurrencyService currencyService)
    {
        var loader = dataLoader.Context.GetOrAddBatchLoader<string, ExpProduct>("compareList_products", async ids =>
        {
            var query = context.GetCatalogQuery<LoadProductsQuery>();
            query.ObjectIds = ids.ToList();
            query.IncludeFields = context.SubFields.Values.GetAllNodesPaths(context).ToArray();

            // The product's money fields format amounts with the currencies in the user context.
            var currencies = await currencyService.GetAllCurrenciesAsync();
            context.SetCurrencies(currencies, query.CultureName);

            var response = await context.GetMediator().Send(query);

            return response.Products.ToDictionary(x => x.Id);
        });

        return loader.LoadAsync(context.Source.ProductId);
    }

    public static IDataLoaderResult<ExpCategory> LoadCompareListCategory(
        this IDataLoaderContextAccessor dataLoader,
        IResolveFieldContext<CompareListCategory> context)
    {
        if (string.IsNullOrEmpty(context.Source.CategoryId))
        {
            return new DataLoaderResult<ExpCategory>((ExpCategory)null);
        }

        var loader = dataLoader.Context.GetOrAddBatchLoader<string, ExpCategory>("compareList_categories", async ids =>
        {
            var query = context.GetCatalogQuery<LoadCategoryQuery>();
            query.ObjectIds = ids.ToArray();
            query.IncludeFields = context.SubFields.Values.GetAllNodesPaths(context).ToArray();

            var response = await context.GetMediator().Send(query);

            return response.Categories.ToDictionary(x => x.Id);
        });

        return loader.LoadAsync(context.Source.CategoryId);
    }

    public static IDataLoaderResult<ExpProductConfigurationSection> LoadCompareListConfigurationSection(
        this IDataLoaderContextAccessor dataLoader,
        IResolveFieldContext<CompareListItemSection> context)
    {
        var aggregate = context.GetValueForSource<CompareListAggregate>();
        var item = aggregate.List.Items.First(x => x.Sections.Any(section => section.Id == context.Source.Id));

        var loader = dataLoader.Context.GetOrAddBatchLoader<(string ProductId, string SectionId), ExpProductConfigurationSection>("compareList_configurationSections", async keys =>
        {
            var includeFields = context.SubFields.Values.GetAllNodesPaths(context).ToArray();
            var result = new Dictionary<(string ProductId, string SectionId), ExpProductConfigurationSection>();

            // One query per configurable product, for only the sections its compared items use.
            foreach (var group in keys.GroupBy(x => x.ProductId))
            {
                var query = AbstractTypeFactory<GetProductConfigurationQuery>.TryCreateInstance();
                query.ConfigurableProductId = group.Key;
                query.SectionIds = group.Select(x => x.SectionId).Distinct().ToList();
                query.StoreId = aggregate.Store.Id;
                query.CurrencyCode = aggregate.CurrencyCode;
                query.CultureName = aggregate.CultureName;
                query.UserId = aggregate.List.UserId;
                query.OrganizationId = context.GetCurrentOrganizationId();
                query.IncludeFields = includeFields;

                var response = await context.GetMediator().Send(query);

                foreach (var section in response.ConfigurationSections)
                {
                    result[(group.Key, section.Id)] = section;
                }
            }

            return result;
        });

        return loader.LoadAsync((item.ProductId, context.Source.SectionId));
    }
}
