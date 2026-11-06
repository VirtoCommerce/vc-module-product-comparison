using GraphQL.Types;
using VirtoCommerce.ProductComparison.Xapi.Aggregates;
using VirtoCommerce.Xapi.Core.Schemas;

namespace VirtoCommerce.ProductComparison.Xapi.Schemas;

public class CompareListType : ExtendableGraphType<CompareListAggregate>
{
    public CompareListType()
    {
        Field<NonNullGraphType<StringGraphType>>("id")
            .Description("Compare list ID")
            .Resolve(context => context.Source.List.Id);

        Field<NonNullGraphType<StringGraphType>>("storeId")
            .Description("Store ID")
            .Resolve(context => context.Source.List.StoreId);

        Field<NonNullGraphType<IntGraphType>>("itemsCount")
            .Description("Number of compared products; each category counts up to the store's limit")
            .Resolve(context => context.Source.GetVisibleItems().Count);

        ExtendableField<NonNullGraphType<ListGraphType<NonNullGraphType<CompareListItemType>>>>(
            "items",
            "Compared products, by category, then by position",
            resolve: context => context.Source.GetVisibleItems());

        ExtendableField<NonNullGraphType<ListGraphType<NonNullGraphType<CompareListCategoryType>>>>(
            "categories",
            "Compared products grouped by category, in the order the categories were first added",
            resolve: context => context.Source.GetCategories());
    }
}
