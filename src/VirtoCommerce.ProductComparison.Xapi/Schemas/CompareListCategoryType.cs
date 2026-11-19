using GraphQL.DataLoader;
using GraphQL.Resolvers;
using GraphQL.Types;
using VirtoCommerce.ProductComparison.Xapi.Extensions;
using VirtoCommerce.ProductComparison.Xapi.Models;
using VirtoCommerce.Xapi.Core.Helpers;
using VirtoCommerce.Xapi.Core.Schemas;
using VirtoCommerce.XCatalog.Core.Models;
using VirtoCommerce.XCatalog.Core.Schemas;

namespace VirtoCommerce.ProductComparison.Xapi.Schemas;

public class CompareListCategoryType : ExtendableGraphType<CompareListCategory>
{
    public CompareListCategoryType(IDataLoaderContextAccessor dataLoader)
    {
        Field<StringGraphType>("id")
            .Description("Category ID; null for products without a category")
            .Resolve(context => context.Source.CategoryId);

        var categoryField = new FieldType
        {
            Name = "category",
            Description = "The category",
            Type = GraphTypeExtensionHelper.GetActualType<CategoryType>(),
            Resolver = new FuncFieldResolver<CompareListCategory, IDataLoaderResult<ExpCategory>>(context => dataLoader.LoadCompareListCategory(context)),
        };
        AddField(categoryField);

        ExtendableField<NonNullGraphType<ListGraphType<NonNullGraphType<CompareListItemType>>>>(
            "items",
            "The category's compared products, by position, at most the store's limit",
            resolve: context => context.Source.Items);
    }
}
