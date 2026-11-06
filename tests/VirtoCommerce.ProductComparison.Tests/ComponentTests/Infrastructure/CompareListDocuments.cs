using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace VirtoCommerce.ProductComparison.Tests.ComponentTests.Infrastructure;

/// <summary>
/// The compare list's GraphQL documents, written as the storefront writes them, and readers for their responses.
/// They select the list's own fields only: <c>product</c>, <c>category</c> and <c>configurationSection</c> resolve
/// through X-Catalog and X-Cart query handlers this harness doesn't host.
/// </summary>
internal static class CompareListDocuments
{
    public const string ListFields = """
        id storeId itemsCount
        items { id productId categoryId position createdDate configurationSections { sectionId type productId quantity customText } }
        categories { id items { productId position } }
        """;

    /// <summary><c>compareList(storeId, userId)</c>.</summary>
    public static string Query(string userId, string storeId = ProductComparisonTestContext.StoreId)
    {
        return $$"""{ compareList(storeId: "{{storeId}}", userId: "{{userId}}") { {{ListFields}} } }""";
    }

    /// <summary>A mutation's <c>command</c>: the store and user every command carries, plus <paramref name="input"/>.</summary>
    public static string Mutation(string name, string userId, string input = "", string storeId = ProductComparisonTestContext.StoreId)
    {
        return $$"""mutation { {{name}}(command: { storeId: "{{storeId}}", userId: "{{userId}}" {{input}} }) { {{ListFields}} } }""";
    }

    /// <summary><c>saveProductsToCompareList</c> with plain products, each given as "productId@categoryId" (no "@" for none).</summary>
    public static string SaveProducts(string userId, params string[] products)
    {
        return SaveProductsInStore(ProductComparisonTestContext.StoreId, userId, products);
    }

    public static string SaveProductsInStore(string storeId, string userId, params string[] products)
    {
        var input = string.Join(" ", products.Select(product =>
        {
            var parts = product.Split('@');
            var category = parts.Length > 1 ? $"""categoryId: "{parts[1]}" """ : string.Empty;

            return $$"""{ productId: "{{parts[0]}}" {{category}}}""";
        }));

        return Mutation("saveProductsToCompareList", userId, $"products: [{input}]", storeId);
    }

    /// <summary>The list's items, in the order the list returns them, as "productId@categoryId#position".</summary>
    public static IList<string> Items(JsonElement list)
    {
        return list.GetProperty("items").EnumerateArray()
            .Select(x => $"{x.GetProperty("productId").GetString()}@{x.GetProperty("categoryId").GetString()}#{x.GetProperty("position").GetInt32()}")
            .ToList();
    }

    /// <summary>The list's category groups, in order, as "categoryId: productId productId".</summary>
    public static IList<string> Categories(JsonElement list)
    {
        return list.GetProperty("categories").EnumerateArray()
            .Select(x => $"{x.GetProperty("id").GetString()}: {string.Join(' ', x.GetProperty("items").EnumerateArray().Select(item => item.GetProperty("productId").GetString()))}")
            .ToList();
    }

    public static string ItemId(JsonElement list, string productId)
    {
        return list.GetProperty("items").EnumerateArray()
            .Single(x => x.GetProperty("productId").GetString() == productId)
            .GetProperty("id").GetString();
    }

    public static int ItemsCount(JsonElement list)
    {
        return list.GetProperty("itemsCount").GetInt32();
    }
}
