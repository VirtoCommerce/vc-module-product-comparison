using System.Threading.Tasks;
using FluentValidation;
using GraphQL;
using GraphQL.Types;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using VirtoCommerce.ProductComparison.Xapi.Aggregates;
using VirtoCommerce.ProductComparison.Xapi.Authorization;
using VirtoCommerce.ProductComparison.Xapi.Extensions;
using VirtoCommerce.ProductComparison.Xapi.Schemas;
using VirtoCommerce.Xapi.Core.BaseQueries;

namespace VirtoCommerce.ProductComparison.Xapi.Commands;

public abstract class CompareListCommandBuilder<TCommand, TCommandGraphType>(IAuthorizationService authorizationService)
    : CommandBuilder<TCommand, CompareListAggregate, TCommandGraphType, CompareListType>(authorizationService)
    where TCommand : CompareListCommand
    where TCommandGraphType : IInputObjectGraphType
{
    protected override async Task BeforeMediatorSend(IResolveFieldContext<object> context, TCommand request)
    {
        await base.BeforeMediatorSend(context, request);

        await context.RequestServices.GetRequiredService<AbstractValidator<TCommand>>().ValidateAndThrowAsync(request);
        await context.PrepareCompareListRequestAsync(request);
        await Authorize(context, request.UserId, new CanAccessCompareListAuthorizationRequirement());
    }

    protected override async Task AfterMediatorSend(IResolveFieldContext<object> context, TCommand request, CompareListAggregate response)
    {
        await base.AfterMediatorSend(context, request, response);

        if (response != null)
        {
            context.SetCompareListContext(response);
        }
    }
}
