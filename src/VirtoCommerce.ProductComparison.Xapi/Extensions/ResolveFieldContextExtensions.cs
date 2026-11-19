using System.Threading.Tasks;
using GraphQL;
using Microsoft.Extensions.DependencyInjection;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.ProductComparison.Xapi.Aggregates;
using VirtoCommerce.ProductComparison.Xapi.Models;
using VirtoCommerce.Xapi.Core.Extensions;
using VirtoCommerce.Xapi.Core.Services;

namespace VirtoCommerce.ProductComparison.Xapi.Extensions;

public static class ResolveFieldContextExtensions
{
    // From the token, never from input: the organization picks the list, and the guest flag picks the access rule.
    // The caller's own id takes the token's spelling, because the providers compare ids with different case rules.
    public static async Task PrepareCompareListRequestAsync(this IResolveFieldContext context, ICompareListRequest request)
    {
        await context.RequestServices.GetRequiredService<IUserManagerCore>().CheckCurrentUserState(context, allowAnonymous: true);

        request.OrganizationId = context.GetCurrentOrganizationId();
        request.IsAnonymous = !context.IsAuthenticated();

        var currentUserId = context.GetCurrentUserId();
        if (!request.IsAnonymous && request.UserId.EqualsIgnoreCase(currentUserId))
        {
            request.UserId = currentUserId;
        }
    }

    // The catalog's own fields under a compared product (prices, categories) read these from the user context, and
    // the compare list's nested types find their aggregate through the expanded object graph.
    public static void SetCompareListContext(this IResolveFieldContext context, CompareListAggregate aggregate)
    {
        context.UserContext["storeId"] = aggregate.Store.Id;
        context.UserContext["store"] = aggregate.Store;
        context.UserContext["userId"] = aggregate.List.UserId;
        context.UserContext["cultureName"] = aggregate.CultureName;
        context.UserContext["currencyCode"] = aggregate.CurrencyCode;

        context.SetExpandedObjectGraph(aggregate);
    }
}
