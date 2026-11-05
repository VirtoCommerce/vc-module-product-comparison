using GraphQL;

namespace VirtoCommerce.ProductComparison.Xapi;

public static class CompareListErrors
{
    public const string StoreNotFoundCode = "STORE_NOT_FOUND";
    public const string DisabledCode = "COMPARE_LIST_DISABLED";
    public const string AnonymousDisabledCode = "ANONYMOUS_COMPARE_LIST_DISABLED";
    public const string LimitReachedCode = "COMPARE_LIST_LIMIT_REACHED";
    public const string FullCode = "COMPARE_LIST_FULL";

    public static ExecutionError StoreNotFound(string storeId)
    {
        return new ExecutionError($"Store {storeId} not found") { Code = StoreNotFoundCode };
    }

    public static ExecutionError Disabled()
    {
        return new ExecutionError("Product comparison is disabled for this store") { Code = DisabledCode };
    }

    public static ExecutionError AnonymousDisabled()
    {
        return new ExecutionError("Sign in to compare products") { Code = AnonymousDisabledCode };
    }

    public static ExecutionError LimitReached(int maxItems)
    {
        return new ExecutionError($"A category can hold at most {maxItems} compared products") { Code = LimitReachedCode };
    }

    public static ExecutionError Full(int maxListItems, int maxListConfigurationSections)
    {
        return new ExecutionError($"A compare list can hold at most {maxListItems} products and {maxListConfigurationSections} configuration sections") { Code = FullCode };
    }
}
