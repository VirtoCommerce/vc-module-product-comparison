export const MODULE_ID = "VirtoCommerce.ProductComparison";
export const ENABLED_KEY = "ProductComparison.Enabled";
export const MAX_ITEMS_KEY = "ProductComparison.MaxItems";
export const ANONYMOUS_LIST_ENABLED_KEY = "ProductComparison.AnonymousListEnabled";

export const COMPARE_ROUTE_NAME = "CompareProducts";
export const COMPARE_ROUTE_PATH = "/compare";

export const MENU_LINK_ID = "product-comparison";
export const PURCHASING_MENU_LINK_ID = "product-comparison-purchasing";
export const COMPARE_ACTION_NAME = "compare";

/** The item whose configuration the product page preselects ("Customize" in the compare table). */
export const COMPARE_ITEM_ID_PARAM = "compareItemId";
/** The category tab the compare page opens on. */
export const CATEGORY_PARAM = "category";

/** The guest's list, merged into the account's list after sign-in. */
export const GUEST_LIST_ID_STORAGE_KEY = "vc-product-comparison:guest-list-id";

/** How many category breadcrumb levels, from the root, group products for compare. */
export const COMPARE_CATEGORY_DEPTH = 2;

export const NOTIFICATIONS_GROUP = "compare-products";
export const NOTIFICATION_PRODUCT_NAME_MAX_LENGTH = 60;

export const CompareListErrorCode = {
  Disabled: "COMPARE_LIST_DISABLED",
  AnonymousListDisabled: "ANONYMOUS_COMPARE_LIST_DISABLED",
  LimitReached: "COMPARE_LIST_LIMIT_REACHED",
  Full: "COMPARE_LIST_FULL",
} as const;

// Each source of table rows gets its own key prefix, so a catalog or configuration property can never take a
// built-in row's key (a property literally named "price" or "sku").
const CUSTOM_FIELD_ROW_KEY_PREFIX = "field:";
export const PROPERTY_ROW_KEY_PREFIX = "property:";
export const CONFIG_PROPERTY_ROW_KEY_PREFIX = "config:";

/** The rows the "Differences" filter never hides. */
export const PRICE_ROW_KEY = `${CUSTOM_FIELD_ROW_KEY_PREFIX}price`;
export const AVAILABILITY_ROW_KEY = `${CUSTOM_FIELD_ROW_KEY_PREFIX}availability`;

export const RATING_ROW_KEY = `${CUSTOM_FIELD_ROW_KEY_PREFIX}rating`;
export const SKU_ROW_KEY = `${CUSTOM_FIELD_ROW_KEY_PREFIX}sku`;
export const MIN_ORDER_QTY_ROW_KEY = `${CUSTOM_FIELD_ROW_KEY_PREFIX}minOrderQty`;

export const EMPTY_VALUE_PLACEHOLDER = "–";

// Host values the plugin reads; the facade does not export them.
export const CUSTOMER_REVIEWS_MODULE_ID = "VirtoCommerce.CustomerReviews";
export const CUSTOMER_REVIEWS_ENABLED_KEY = "CustomerReviews.CustomerReviewsEnabled";
export const XAPI_MODULE_ID = "VirtoCommerce.Xapi";
export const CONTINUE_SHOPPING_LINK_KEY = "Frontend.ContinueShoppingLink";
export const XRECOMMEND_MODULE_ID = "VirtoCommerce.XRecommend";
export const XRECOMMEND_ENABLED_KEY = "XRecommend.RecommendationsEnabled";
export const MAX_DISPLAY_IN_STOCK_QUANTITY = 9999;
export const DIGITAL_PRODUCT_TYPE = "Digital";
export const CATALOG_PRODUCT_ERROR_OBJECT_TYPE = "CatalogProduct";
export const CATEGORY_BREADCRUMB_TYPE_NAME = "Category";
