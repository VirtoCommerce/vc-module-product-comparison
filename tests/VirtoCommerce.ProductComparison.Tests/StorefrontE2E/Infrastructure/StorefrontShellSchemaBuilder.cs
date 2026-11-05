using System;
using System.Threading.Tasks;
using GraphQL;
using GraphQL.Resolvers;
using GraphQL.Types;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using VirtoCommerce.Platform.Core.Security;
using VirtoCommerce.ProductComparison.Tests.ComponentTests.Infrastructure;
using VirtoCommerce.ProfileExperienceApiModule.Data.Models;
using VirtoCommerce.ProfileExperienceApiModule.Data.Queries.AddressesQuery;
using VirtoCommerce.ProfileExperienceApiModule.Data.Schemas;
using VirtoCommerce.Xapi.Core.Extensions;
using VirtoCommerce.Xapi.Core.Infrastructure;
using VirtoCommerce.Xapi.Core.Models;
using VirtoCommerce.Xapi.Core.Queries;
using VirtoCommerce.XCart.Core.Queries;
using VirtoCommerce.XCart.Core.Schemas;
using VirtoCommerce.XCatalog.Core.Models;
using VirtoCommerce.XCatalog.Core.Queries;
using VirtoCommerce.XCatalog.Core.Schemas;
using ProfileUserType = VirtoCommerce.ProfileExperienceApiModule.Data.Schemas.UserType;

namespace VirtoCommerce.ProductComparison.Tests.StorefrontE2E.Infrastructure;

/// <summary>
/// The root fields the storefront shell asks for around the product and compare pages (captured from the real
/// storefront on 2026-11-04, and on 2026-11-05 for the address book that replaced `me`'s addresses): pageContext, me,
/// currentCustomerAddresses, childCategories, cart, organization, menu, searchHistory. Schema-complete,
/// so every selection set the storefront sends validates against the real response types; data-empty wherever the
/// data is not under test. A root field that is simply missing fails the whole operation with a validation error.
/// The store, the product and the compare list come from their real builders.
/// </summary>
internal sealed class StorefrontShellSchemaBuilder : ISchemaBuilder
{
    public void Build(ISchema schema)
    {
        // What X-Frontend's PageContextQueryBuilder composes: user + store + slugInfo (white-labeling is gated off
        // client-side because the module is not in the store's module list).
        schema.Query.AddField(new FieldType
        {
            Name = "pageContext",
            Type = typeof(PageContextResponseType),
            Arguments = new QueryArguments(
                new QueryArgument<StringGraphType> { Name = "userId" },
                new QueryArgument<StringGraphType> { Name = "organizationId" },
                new QueryArgument<StringGraphType> { Name = "domain" },
                new QueryArgument<StringGraphType> { Name = "storeId" },
                new QueryArgument<StringGraphType> { Name = "permalink" },
                new QueryArgument<StringGraphType> { Name = "cultureName" }),
            Resolver = new FuncFieldResolver<object>(ResolvePageContextAsync),
        });

        // X-Profile's `me`: the same user.
        schema.Query.AddField(new FieldType
        {
            Name = "me",
            Type = typeof(ProfileUserType),
            Arguments = new QueryArguments(new QueryArgument<StringGraphType> { Name = "userId" }),
            Resolver = new FuncFieldResolver<object>(ResolveUser),
        });

        // X-Profile's address book, which a signed-in storefront reads on every load for its ship-to selector. The
        // real field is a connection: `first` and `after` come with it, not with the query's own arguments.
        var addressArguments = Arguments<CurrentCustomerAddressesQuery>();
        addressArguments.Add(new QueryArgument<IntGraphType> { Name = "first" });
        addressArguments.Add(new QueryArgument<StringGraphType> { Name = "after" });
        AddEmpty(schema, "currentCustomerAddresses", typeof(MemberAddressConnectionType<MemberAddressType>), addressArguments,
            _ => new MemberAddressConnection<MemberAddress>([], 0, 0, 0));

        // The arguments come from the real query classes, so they match what the storefront's codegen saw.
        AddEmpty(schema, "childCategories", typeof(ChildCategoriesQueryResponseType), Arguments<ChildCategoriesQuery>(),
            _ => new ChildCategoriesQueryResponse { ChildCategories = [] });
        AddEmpty(schema, "cart", typeof(CartType), Arguments<GetCartQuery>(), _ => null);
        AddEmpty(schema, "organization", typeof(OrganizationType),
            new QueryArguments(
                new QueryArgument<NonNullGraphType<StringGraphType>> { Name = "id" },
                new QueryArgument<StringGraphType> { Name = "userId" }),
            _ => null);

        AddEmpty(schema, "menu", typeof(MenuLinkListType),
            new QueryArguments(
                new QueryArgument<NonNullGraphType<StringGraphType>> { Name = "storeId" },
                new QueryArgument<NonNullGraphType<StringGraphType>> { Name = "cultureName" },
                new QueryArgument<NonNullGraphType<StringGraphType>> { Name = "name" }),
            context => new MenuLinkList { Name = context.GetArgument<string>("name"), Items = [] });

        AddEmpty(schema, "searchHistory", typeof(SearchHistoryResultType),
            new QueryArguments(
                new QueryArgument<NonNullGraphType<StringGraphType>> { Name = "storeId" },
                new QueryArgument<NonNullGraphType<IntGraphType>> { Name = "maxCount" }),
            _ => new SearchHistoryResult { Queries = [] });
    }

    private static async ValueTask<object> ResolvePageContextAsync(IResolveFieldContext context)
    {
        var store = await context.RequestServices!.GetRequiredService<IMediator>().Send(new GetStoreQuery
        {
            Domain = context.GetArgument<string>("domain"),
            StoreId = context.GetArgument<string>("storeId"),
            CultureName = context.GetArgument<string>("cultureName"),
        });

        return new PageContextResponse { Store = store, User = ResolveUser(context), SlugInfo = new SlugInfoResponse() };
    }

    /// <summary>
    /// As X-Frontend's page context does: a signed-in caller gets their account; a guest gets an anonymous user with the
    /// id the storefront keeps for them (localStorage user-id), or a new one, and never a registered account's id.
    /// That id is what the storefront then sends as the guest's userId, the compare list's included.
    /// </summary>
    private static ApplicationUser ResolveUser(IResolveFieldContext context)
    {
        var users = context.RequestServices!.GetRequiredService<TestUsers>();

        if (context.IsAuthenticated())
        {
            return users.Find(context.GetCurrentUserId());
        }

        var user = AnonymousUser.Instance;
        var userId = context.GetArgument<string>("userId");
        if (!string.IsNullOrEmpty(userId) && users.Find(userId) == null)
        {
            user.Id = userId;
        }

        return user;
    }

    private static QueryArguments Arguments<TQuery>() where TQuery : IHasArguments, new()
        => new(new TQuery().GetArguments());

    private static void AddEmpty(ISchema schema, string name, Type graphType, QueryArguments arguments, Func<IResolveFieldContext, object> resolve)
    {
        schema.Query.AddField(new FieldType
        {
            Name = name,
            Type = graphType,
            Arguments = arguments,
            Resolver = new FuncFieldResolver<object>(context => new ValueTask<object>(resolve(context))),
        });
    }
}
