using GraphQL.Types;

namespace VirtoCommerce.ProductComparison.Tests.StorefrontE2E.Infrastructure;

internal sealed class SearchHistoryResultType : ObjectGraphType<SearchHistoryResult>
{
    public SearchHistoryResultType()
    {
        Name = "SearchHistoryResultType";
        Field<ListGraphType<StringGraphType>>("queries").Resolve(context => context.Source.Queries);
    }
}
