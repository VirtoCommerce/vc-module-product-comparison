using System.Threading.Tasks;
using GraphQL;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.ProductComparison.Core.Services;
using VirtoCommerce.ProductComparison.Xapi.Authorization;
using VirtoCommerce.ProductComparison.Xapi.Schemas;
using VirtoCommerce.Xapi.Core.Extensions;
using VirtoCommerce.Xapi.Core.Security.Authorization;

namespace VirtoCommerce.ProductComparison.Xapi.Commands;

public class MergeCompareListCommandBuilder(IAuthorizationService authorizationService)
    : CompareListCommandBuilder<MergeCompareListCommand, InputMergeCompareListType>(authorizationService)
{
    protected override string Name => "mergeCompareList";

    protected override async Task BeforeMediatorSend(IResolveFieldContext<object> context, MergeCompareListCommand request)
    {
        // Only a signed-in buyer takes a guest list over.
        if (!context.IsAuthenticated())
        {
            throw AuthorizationError.AnonymousAccessDenied();
        }

        await base.BeforeMediatorSend(context, request);

        var source = await context.RequestServices.GetRequiredService<ICompareListService>().GetNoCloneAsync(request.SecondListId);

        if (source != null)
        {
            await Authorize(context, source, new CanAccessCompareListAuthorizationRequirement());
        }
    }
}
