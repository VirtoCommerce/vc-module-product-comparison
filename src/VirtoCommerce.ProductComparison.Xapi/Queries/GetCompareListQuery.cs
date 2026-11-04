using System.Collections.Generic;
using GraphQL;
using GraphQL.Types;
using VirtoCommerce.ProductComparison.Xapi.Aggregates;
using VirtoCommerce.ProductComparison.Xapi.Models;
using VirtoCommerce.Xapi.Core.BaseQueries;

namespace VirtoCommerce.ProductComparison.Xapi.Queries;

public class GetCompareListQuery : Query<CompareListAggregate>, ICompareListRequest
{
    public string StoreId { get; set; }

    public string UserId { get; set; }

    public string CultureName { get; set; }

    public string CurrencyCode { get; set; }

    public string OrganizationId { get; set; }

    public bool IsAnonymous { get; set; }

    public override IEnumerable<QueryArgument> GetArguments()
    {
        yield return Argument<NonNullGraphType<StringGraphType>>(nameof(StoreId), "Store ID");
        yield return Argument<NonNullGraphType<StringGraphType>>(nameof(UserId), "The signed-in user's ID, or the guest's anonymous ID");
        yield return Argument<StringGraphType>(nameof(CultureName), "Culture name (\"en-US\") for the products");
        yield return Argument<StringGraphType>(nameof(CurrencyCode), "Currency code (\"USD\") for the products");
    }

    public override void Map(IResolveFieldContext context)
    {
        StoreId = context.GetArgument<string>(nameof(StoreId));
        UserId = context.GetArgument<string>(nameof(UserId));
        CultureName = context.GetArgument<string>(nameof(CultureName));
        CurrencyCode = context.GetArgument<string>(nameof(CurrencyCode));
    }
}
