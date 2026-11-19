import { useQuery } from "@vue/apollo-composable";
import {
  apolloClient,
  getPropertiesGroupedByName,
  globals,
  Logger,
  SUPPRESS_ERROR_NOTIFICATIONS_CONTEXT,
  useAnalytics,
  useModuleSettings,
} from "@vc-frontend/core";
import { computed, ref, shallowRef, watch } from "vue";
import { useI18n } from "vue-i18n";
import { useRoute } from "vue-router";
import { CompareProductsDocument, PropertyValueTypes } from "../api/graphql/types";
import { CreateConfiguredLineItemDocument } from "../api/graphql/storefront/types";
import {
  AVAILABILITY_ROW_KEY,
  CATEGORY_PARAM,
  CONFIG_PROPERTY_ROW_KEY_PREFIX,
  CUSTOMER_REVIEWS_ENABLED_KEY,
  CUSTOMER_REVIEWS_MODULE_ID,
  DIGITAL_PRODUCT_TYPE,
  EMPTY_VALUE_PLACEHOLDER,
  MAX_DISPLAY_IN_STOCK_QUANTITY,
  MIN_ORDER_QTY_ROW_KEY,
  PRICE_ROW_KEY,
  PROPERTY_ROW_KEY_PREFIX,
  RATING_ROW_KEY,
  SKU_ROW_KEY,
} from "../constants";
import { getCategoryKey, getDisplayPrice, toConfigurationSectionInputs } from "../utils";
import { whenSettled } from "./useCompareList";
import type { CompareProductFragment } from "../api/graphql/types";
import type { StorefrontMoneyFragment } from "../api/graphql/storefront/types";
import type {
  ComparePageItemType,
  ICompareCategoryTab,
  ICompareColumn,
  ICompareTableRow,
  CompareRowKindType,
} from "../types";
import type { ProductActionPropsType } from "@vc-frontend/core";

type ConfiguredPriceType = { listPrice?: StorefrontMoneyFragment; salePrice?: StorefrontMoneyFragment };
type MoneyType = CompareProductFragment["price"]["actual"];
type SectionType = ComparePageItemType["configurationSections"][number];

// The analytics mapper reads only what the compare query selects: code, name, vendor, price, properties, breadcrumbs.
type AnalyticsProductType = ProductActionPropsType["product"];

const COMPARE_LIST_ID = "compare_products";

type HostPropertiesType = Parameters<typeof getPropertiesGroupedByName>[0];

function byLowercaseName<T extends { name: string }>(items: T[]): Map<string, T> {
  const result = new Map<string, T>();
  items.forEach((item) => {
    const name = item.name.toLowerCase();
    if (!result.has(name)) {
      result.set(name, item);
    }
  });
  return result;
}

// A product's properties as rows read them, built once per product: as the product page shows them, and the raw
// booleans, since the shown ones hold display text ("Yes"/"No").
function indexProperties(product: CompareProductFragment) {
  return {
    shown: byLowercaseName(
      Object.values(getPropertiesGroupedByName(product.properties as unknown as HostPropertiesType)),
    ),
    booleans: byLowercaseName(
      product.properties.filter((prop) => !prop.hidden && prop.propertyValueType === PropertyValueTypes.Boolean),
    ),
  };
}

function getAvailabilitySignature(product: CompareProductFragment): string {
  if (product.productType === DIGITAL_PRODUCT_TYPE) {
    return "digital";
  }

  if (!product.availabilityData.isInStock) {
    return `out-of-stock:${product.availabilityData.isAvailable}`;
  }

  const quantity = product.availabilityData.availableQuantity;
  const shownQuantity =
    quantity > MAX_DISPLAY_IN_STOCK_QUANTITY ? `${MAX_DISPLAY_IN_STOCK_QUANTITY}+` : String(quantity);
  return `in-stock:${shownQuantity}`;
}

function getSectionValue(section: SectionType): string | undefined {
  return section.productId
    ? section.configurationSection?.options?.find((option) => option?.product?.id === section.productId)?.product?.name
    : section.customText;
}

function applyPrice(price: MoneyType, override?: StorefrontMoneyFragment): MoneyType {
  return override ? { ...price, ...override } : price;
}

// A configured item's price depends on its options, so the base product price is not enough.
function withConfiguredPrice(
  product: CompareProductFragment,
  configured?: ConfiguredPriceType,
): CompareProductFragment {
  if (!configured) {
    return product;
  }

  return {
    ...product,
    price: {
      ...product.price,
      actual: applyPrice(product.price.actual, configured.salePrice),
      list: applyPrice(product.price.list, configured.listPrice),
    },
  };
}

export function useComparePage() {
  const { t, n } = useI18n();
  const route = useRoute();
  const { analytics } = useAnalytics();
  const customerRatingEnabled = useModuleSettings(CUSTOMER_REVIEWS_MODULE_ID).isEnabled(CUSTOMER_REVIEWS_ENABLED_KEY);
  const { storeId, userId, cultureName, currencyCode } = globals;

  // A save or the sign-in merge still on its way would leave the page on the list before it, while the header
  // already counts the new one: the page asks once they are done.
  const settled = ref(false);
  void whenSettled().then(() => {
    settled.value = true;
  });

  const { result, loading, error, refetch } = useQuery(
    CompareProductsDocument,
    { storeId, userId, cultureName, currencyCode },
    () => ({ fetchPolicy: "no-cache", context: SUPPRESS_ERROR_NOTIFICATIONS_CONTEXT, enabled: settled.value }),
  );

  const categories = computed(() => result.value?.compareList?.categories ?? []);
  const totalCount = computed(() => categories.value.reduce((count, category) => count + category.items.length, 0));
  const isEmpty = computed(() => !!result.value && !totalCount.value);

  const categoryTabs = computed<ICompareCategoryTab[]>(() =>
    categories.value.map((category) => ({
      key: getCategoryKey(category.id),
      label:
        category.category?.name ||
        t(category.id ? "product_comparison.table.unresolved_category" : "product_comparison.table.uncategorized"),
      count: category.items.length,
    })),
  );

  // The "added to compare" notification opens the page on the product's category.
  const selectedKey = ref(typeof route.query[CATEGORY_PARAM] === "string" ? route.query[CATEGORY_PARAM] : undefined);
  const selectedCategory = computed(
    () => categories.value.find((category) => getCategoryKey(category.id) === selectedKey.value) ?? categories.value[0],
  );
  const selectedCategoryKey = computed(() => getCategoryKey(selectedCategory.value?.id));
  const selectedTab = computed(() => categoryTabs.value.find((tab) => tab.key === selectedCategoryKey.value));

  const configuredItems = computed(() =>
    categories.value.flatMap((category) =>
      category.items.filter((item) => item.product && item.configurationSections.length),
    ),
  );
  const configuredPrices = shallowRef<Record<string, ConfiguredPriceType>>({});

  // Keyed by item id, which a configuration never changes: only items not priced yet are asked for.
  watch(
    () => configuredItems.value.map((item) => item.id).join(),
    async (_signature, _previous, onCleanup) => {
      let cancelled = false;
      onCleanup(() => {
        cancelled = true;
      });

      const items = configuredItems.value.filter((item) => !(item.id in configuredPrices.value));
      const results = await Promise.allSettled(
        items.map((item) =>
          apolloClient.mutate({
            mutation: CreateConfiguredLineItemDocument,
            variables: {
              command: {
                configurableProductId: item.productId,
                configurationSections: toConfigurationSectionInputs(item),
                storeId,
                currencyCode,
                cultureName,
              },
            },
            fetchPolicy: "no-cache",
          }),
        ),
      );

      if (cancelled) {
        return;
      }

      const prices = { ...configuredPrices.value };
      items.forEach((item, index) => {
        const itemResult = results[index];
        if (itemResult.status === "fulfilled") {
          prices[item.id] = itemResult.value.data?.createConfiguredLineItem ?? {};
        } else {
          Logger.error("[product-comparison] configured price", itemResult.reason);
        }
      });
      configuredPrices.value = prices;
    },
    { immediate: true },
  );

  // One column per item: the same product can be compared in several configurations.
  const columns = computed<ICompareColumn[]>(() =>
    (selectedCategory.value?.items ?? []).map((item) => ({
      item,
      product: item.product && withConfiguredPrice(item.product, configuredPrices.value[item.id]),
    })),
  );

  const availableIndexes = computed(() => columns.value.flatMap((column, index) => (column.product ? [index] : [])));

  function differs(values: string[]): boolean {
    return new Set(availableIndexes.value.map((index) => values[index])).size > 1;
  }

  function makeRow(
    key: string,
    label: string,
    kind: CompareRowKindType,
    getValue: (product: CompareProductFragment, item: ComparePageItemType) => string,
    description?: string,
  ): ICompareTableRow {
    const values = columns.value.map(({ item, product }) =>
      product ? getValue(product, item) : EMPTY_VALUE_PLACEHOLDER,
    );
    return { key, label, kind, values, differs: differs(values), description };
  }

  const availableProducts = computed(() => columns.value.flatMap(({ product }) => (product ? [product] : [])));

  const fieldRows = computed<ICompareTableRow[]>(() => {
    if (!availableProducts.value.length) {
      return [];
    }

    const rows = [
      makeRow(
        PRICE_ROW_KEY,
        t("product_comparison.table.fields.price_per_unit"),
        "price",
        (product) => getDisplayPrice(product).actual.formattedAmount,
        t("product_comparison.table.fields.price_per_unit_tooltip"),
      ),
    ];

    if (customerRatingEnabled && availableProducts.value.some((product) => product.rating)) {
      rows.push(
        makeRow(RATING_ROW_KEY, t("product_comparison.table.fields.customer_rating"), "rating", (product) =>
          product.rating
            ? n(product.rating.value, { minimumFractionDigits: 1, maximumFractionDigits: 1 })
            : EMPTY_VALUE_PLACEHOLDER,
        ),
      );
    }

    rows.push(
      makeRow(AVAILABILITY_ROW_KEY, t("product_comparison.table.fields.availability"), "availability", (product) =>
        getAvailabilitySignature(product),
      ),
      makeRow(SKU_ROW_KEY, t("product_comparison.table.fields.sku"), "text", (product) => product.code),
      makeRow(
        MIN_ORDER_QTY_ROW_KEY,
        t("product_comparison.table.fields.min_order_qty"),
        "text",
        (product) => (product.minQuantity != null ? n(product.minQuantity) : EMPTY_VALUE_PLACEHOLDER),
        t("product_comparison.table.fields.min_order_qty_tooltip"),
      ),
    );

    return rows;
  });

  const propertyRows = computed<ICompareTableRow[]>(() => {
    const properties = new Map(availableProducts.value.map((product) => [product, indexProperties(product)]));

    // Lowercased name -> the label of its first occurrence.
    const labelsByName = new Map<string, string>();
    properties.forEach(({ shown }) =>
      shown.forEach((prop, name) => {
        if (!labelsByName.has(name)) {
          labelsByName.set(name, prop.label);
        }
      }),
    );

    return [...labelsByName]
      .map(([name, label]) => {
        const isBoolean = [...properties.values()].some(({ booleans }) => booleans.has(name));
        const row = makeRow(`${PROPERTY_ROW_KEY_PREFIX}${name}`, label, isBoolean ? "boolean" : "text", (product) => {
          const value = properties.get(product)?.shown.get(name)?.value;
          return value != null ? String(value) : EMPTY_VALUE_PLACEHOLDER;
        });

        if (isBoolean) {
          row.boolValues = columns.value.map(({ product }) => {
            const value = product && properties.get(product)?.booleans.get(name)?.value;
            return value === undefined ? undefined : Boolean(value);
          });
        }

        return row;
      })
      .filter((row) => row.values.some((value) => value !== EMPTY_VALUE_PLACEHOLDER));
  });

  // The labels come live from the catalog: a section that no longer exists has no row.
  const configurationRows = computed<ICompareTableRow[]>(() => {
    const labels = new Set(
      columns.value.flatMap(({ item }) =>
        item.configurationSections.flatMap((section) => section.configurationSection?.name ?? []),
      ),
    );

    return [...labels]
      .map((label) =>
        makeRow(`${CONFIG_PROPERTY_ROW_KEY_PREFIX}${label}`, label, "text", (_product, item) => {
          const section = item.configurationSections.find((x) => x.configurationSection?.name === label);
          return (section && getSectionValue(section)) || EMPTY_VALUE_PLACEHOLDER;
        }),
      )
      .filter((row) => row.values.some((value) => value !== EMPTY_VALUE_PLACEHOLDER));
  });

  const tableRows = computed(() => [...fieldRows.value, ...propertyRows.value, ...configurationRows.value]);

  const listProperties = computed(() => ({
    item_list_id: COMPARE_LIST_ID,
    item_list_name: t("product_comparison.page.title"),
  }));

  function selectItemEvent(product: CompareProductFragment, index?: number) {
    analytics("selectItem", product as unknown as AnalyticsProductType, { ...listProperties.value, index });
  }

  // A configured price arriving re-creates the columns; the list is viewed again only when what it shows changes.
  let lastViewedSignature: string | undefined;
  watch(
    availableProducts,
    (products) => {
      const signature = `${selectedCategoryKey.value}:${columns.value.map(({ item }) => item.id).join()}`;
      if (!products.length || signature === lastViewedSignature) {
        return;
      }

      lastViewedSignature = signature;
      analytics("viewItemList", products, listProperties.value);
    },
    { immediate: true },
  );

  function selectCategory(key: string) {
    selectedKey.value = key;
  }

  return {
    loading,
    failed: computed(() => !!error.value),
    isEmpty,
    totalCount,
    categoryTabs,
    selectedCategory,
    selectedCategoryKey,
    selectedCategoryLabel: computed(() => selectedTab.value?.label ?? ""),
    selectedCategoryCount: computed(() => selectedTab.value?.count ?? 0),
    columns,
    tableRows,
    refetch,
    selectCategory,
    selectItemEvent,
  };
}
