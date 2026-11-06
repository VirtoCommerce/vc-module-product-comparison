using GraphQL.DataLoader;
using GraphQL.Resolvers;
using GraphQL.Types;
using VirtoCommerce.CoreModule.Core.Currency;
using VirtoCommerce.ProductComparison.Core.Models;
using VirtoCommerce.ProductComparison.Xapi.Extensions;
using VirtoCommerce.Xapi.Core.Helpers;
using VirtoCommerce.Xapi.Core.Schemas;
using VirtoCommerce.XCatalog.Core.Models;
using VirtoCommerce.XCatalog.Core.Schemas;

namespace VirtoCommerce.ProductComparison.Xapi.Schemas;

public class CompareListItemType : ExtendableGraphType<CompareListItem>
{
    public CompareListItemType(IDataLoaderContextAccessor dataLoader, ICurrencyService currencyService)
    {
        Field(x => x.Id, nullable: false).Description("Compare list item ID");
        Field(x => x.ProductId, nullable: false).Description("Product ID");
        Field(x => x.CategoryId, nullable: true).Description("Category the product is compared in; null for products without a category");
        Field(x => x.Position, nullable: false).Description("Position within the category");
        Field(x => x.CreatedDate, nullable: false).Description("When the product was added");

        ExtendableField<NonNullGraphType<ListGraphType<NonNullGraphType<CompareListConfigurationSectionType>>>>(
            "configurationSections",
            "The chosen configuration of a configurable product",
            resolve: context => context.Source.Sections);

        var productField = new FieldType
        {
            Name = "product",
            Description = "The product; null when it no longer exists or isn't visible in the store",
            Type = GraphTypeExtensionHelper.GetActualType<ProductType>(),
            Resolver = new FuncFieldResolver<CompareListItem, IDataLoaderResult<ExpProduct>>(context => dataLoader.LoadCompareListProduct(context, currencyService)),
        };
        AddField(productField);
    }
}
