using System.Threading.Tasks;
using FluentValidation;
using GraphQL;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using VirtoCommerce.ProductComparison.Xapi.Aggregates;
using VirtoCommerce.ProductComparison.Xapi.Authorization;
using VirtoCommerce.ProductComparison.Xapi.Extensions;
using VirtoCommerce.ProductComparison.Xapi.Schemas;
using VirtoCommerce.Xapi.Core.BaseQueries;

namespace VirtoCommerce.ProductComparison.Xapi.Queries;

public class CompareListQueryBuilder(IAuthorizationService authorizationService)
    : QueryBuilder<GetCompareListQuery, CompareListAggregate, CompareListType>(authorizationService)
{
    protected override string Name => "compareList";

    protected override async Task BeforeMediatorSend(IResolveFieldContext<object> context, GetCompareListQuery request)
    {
        await base.BeforeMediatorSend(context, request);

        await context.RequestServices.GetRequiredService<AbstractValidator<GetCompareListQuery>>().ValidateAndThrowAsync(request);
        await context.PrepareCompareListRequestAsync(request);
        await Authorize(context, request.UserId, new CanAccessCompareListAuthorizationRequirement());
    }

    protected override async Task AfterMediatorSend(IResolveFieldContext<object> context, GetCompareListQuery request, CompareListAggregate response)
    {
        await base.AfterMediatorSend(context, request, response);

        if (response != null)
        {
            context.SetCompareListContext(response);
        }
    }
}
