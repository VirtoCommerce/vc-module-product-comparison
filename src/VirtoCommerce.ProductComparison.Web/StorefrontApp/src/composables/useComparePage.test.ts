import { flushPromises, mount } from "@vue/test-utils";
import { createWrapperFactory } from "@vc-frontend/core/testing";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { defineComponent, ref } from "vue";
import { PropertyType, PropertyValueTypes } from "../api/graphql/types";
import { useComparePage } from "./useComparePage";
import type { CompareProductFragment, CompareProductsQuery } from "../api/graphql/types";
import type { CompareCategoryType, ComparePageItemType } from "../types";

// The page's one query, as the server returns it, and the facade calls the page makes around it.
const page = vi.hoisted(() => ({
  result: undefined as unknown as { value: CompareProductsQuery | undefined },
  query: { category: undefined as string | undefined },
  mutate: vi.fn(),
  analytics: vi.fn(),
}));

vi.mock("@vue/apollo-composable", () => ({
  useQuery: () => ({ result: page.result, loading: ref(false), error: ref(null), refetch: vi.fn() }),
}));

vi.mock("vue-router", () => ({ useRoute: () => ({ query: page.query }) }));

vi.mock("@vc-frontend/core", async (importOriginal) => ({
  ...(await importOriginal<Record<string, unknown>>()),
  apolloClient: { mutate: page.mutate },
  globals: {
    storeId: "B2B-store",
    userId: "user-1",
    cultureName: "en-US",
    currencyCode: "USD",
    i18n: { global: { t: (key: string) => key, d: String, n: String } },
  },
  useAnalytics: () => ({ analytics: page.analytics }),
}));

function money(amount: number) {
  return {
    amount,
    formattedAmount: `$${amount}`,
    formattedAmountWithoutCurrency: `${amount}`,
    currency: { code: "USD", symbol: "$" },
  };
}

function product(id: string, overrides: Partial<CompareProductFragment> = {}): CompareProductFragment {
  return {
    id,
    name: `Product ${id}`,
    code: `SKU-${id}`,
    isConfigurable: false,
    hasVariations: false,
    packSize: 1,
    variations: [],
    availabilityData: { isActive: true, isAvailable: true, isBuyable: true, isInStock: true, availableQuantity: 10 },
    price: { actual: money(10), list: money(10), discountAmount: { amount: 0 } },
    properties: [],
    breadcrumbs: [],
    ...overrides,
  };
}

function item(id: string, overrides: Partial<ComparePageItemType> = {}): ComparePageItemType {
  return { id, productId: `product-${id}`, configurationSections: [], product: product(`product-${id}`), ...overrides };
}

function category(id: string | undefined, name: string | undefined, items: ComparePageItemType[]): CompareCategoryType {
  return { id, category: name === undefined ? undefined : { id: id!, name }, items };
}

function setCategories(categories: CompareCategoryType[]) {
  page.result = ref({ compareList: { id: "list-1", categories } });
}

// useComparePage reads i18n and the route, so it runs inside a component, as on the page.
function runPage() {
  let state!: ReturnType<typeof useComparePage>;
  const Host = defineComponent({
    setup() {
      state = useComparePage();
      return () => null;
    },
  });

  createWrapperFactory(mount, Host)();
  return state;
}

beforeEach(() => {
  vi.clearAllMocks();
  page.query = { category: undefined };
  page.mutate.mockResolvedValue({ data: { createConfiguredLineItem: undefined } });
});

describe("category tabs", () => {
  // The server names each category (in the buyer's language), groups the uncategorized products under a null id,
  // and returns no category for one deleted from the catalog.
  it("label each group, count its items and fall back for the groups without a name", () => {
    setCategories([
      category("printers", "Laser Printers", [item("1"), item("2")]),
      category(undefined, undefined, [item("3")]),
      category("deleted", undefined, [item("4")]),
    ]);

    expect(runPage().categoryTabs.value).toEqual([
      { key: "printers", label: "Laser Printers", count: 2 },
      { key: "", label: "product_comparison.table.uncategorized", count: 1 },
      { key: "deleted", label: "product_comparison.table.unresolved_category", count: 1 },
    ]);
  });

  // The "added to compare" notification links to ?category=, so the page opens where the product went.
  it("open on the category the link names", () => {
    page.query = { category: "laptops" };
    setCategories([category("printers", "Printers", [item("1")]), category("laptops", "Laptops", [item("2")])]);

    const state = runPage();

    expect(state.selectedCategoryKey.value).toBe("laptops");
    expect(state.columns.value.map((column) => column.item.id)).toEqual(["2"]);
  });

  it("open on the first category when the link names none, or one that is gone", () => {
    page.query = { category: "cleared-meanwhile" };
    setCategories([category("printers", "Printers", [item("1")]), category("laptops", "Laptops", [item("2")])]);

    expect(runPage().selectedCategoryKey.value).toBe("printers");
  });

  // An uncategorized product's notification links with an empty category, which is that group's key.
  it("open on the uncategorized group for an empty category", () => {
    page.query = { category: "" };
    setCategories([category("printers", "Printers", [item("1")]), category(undefined, undefined, [item("2")])]);

    expect(runPage().selectedCategoryKey.value).toBe("");
  });
});

// Decision §11.5: an item whose product no longer resolves stays in the list as "no longer available".
describe("an item whose product is gone", () => {
  it("is a column without a product, its cells the placeholder", () => {
    setCategories([category("printers", "Printers", [item("1"), item("2", { product: undefined })])]);

    const state = runPage();
    const priceRow = state.tableRows.value.find((row) => row.key === "field:price")!;

    expect(state.columns.value.map((column) => column.product?.id)).toEqual(["product-1", undefined]);
    expect(priceRow.values).toEqual(["$10", "–"]);
  });

  // Otherwise every row would "differ" against the placeholder and the Differences filter would show them all. The
  // two products here differ only in their SKU.
  it("does not make the other products differ", () => {
    setCategories([category("printers", "Printers", [item("1"), item("2"), item("3", { product: undefined })])]);

    const state = runPage();

    expect(state.tableRows.value.filter((row) => row.differs).map((row) => row.key)).toEqual(["field:sku"]);
    expect(state.differRowsCount.value).toBe(1);
  });

  it("is left out of the viewed list analytics", () => {
    setCategories([category("printers", "Printers", [item("1"), item("2", { product: undefined })])]);

    runPage();

    expect(page.analytics).toHaveBeenCalledWith(
      "viewItemList",
      [expect.objectContaining({ id: "product-1" })],
      expect.objectContaining({ item_list_id: "compare_products" }),
    );
  });
});

describe("a configured item", () => {
  const sections = [
    {
      sectionId: "color",
      type: "Product",
      productId: "red",
      quantity: 1,
      configurationSection: { id: "color", name: "Color", options: [{ product: { id: "red", name: "Red" } }] },
    },
    {
      sectionId: "engraving",
      type: "Text",
      customText: "For Anna",
      configurationSection: { id: "engraving", name: "Engraving", options: [] },
    },
    // A section the catalog no longer has: its label is gone, so it has no row.
    { sectionId: "removed", type: "Text", customText: "x", configurationSection: undefined },
  ];

  // Decision D9: only the selection is stored; the labels come live from the catalog's current configuration.
  it("shows its options under the catalog's current section names", () => {
    setCategories([
      category("printers", "Printers", [
        item("1", { configurationSections: sections }),
        item("2", { configurationSections: [] }),
      ]),
    ]);

    const rows = runPage().tableRows.value.filter((row) => row.key.startsWith("config:"));

    expect(rows).toEqual([
      expect.objectContaining({ label: "Color", values: ["Red", "–"] }),
      expect.objectContaining({ label: "Engraving", values: ["For Anna", "–"] }),
    ]);
  });

  // An option can change the price: the configured price is asked of the cart, once per configured item.
  it("is priced as configured", async () => {
    page.mutate.mockResolvedValue({
      data: { createConfiguredLineItem: { salePrice: money(15), listPrice: money(20) } },
    });
    setCategories([category("printers", "Printers", [item("1", { configurationSections: sections }), item("2")])]);

    const state = runPage();
    await flushPromises();

    expect(page.mutate).toHaveBeenCalledOnce();
    expect(page.mutate.mock.calls[0][0].variables.command).toEqual(
      expect.objectContaining({
        configurableProductId: "product-1",
        configurationSections: expect.arrayContaining([
          expect.objectContaining({ sectionId: "color", option: { productId: "red", quantity: 1 } }),
        ]),
      }),
    );
    expect(state.columns.value[0].product?.price.actual.amount).toBe(15);
    expect(state.columns.value[0].product?.price.list.amount).toBe(20);
    expect(state.columns.value[1].product?.price.actual.amount).toBe(10);
  });
});

describe("property rows", () => {
  it("show a boolean property as a check per product, and nothing for a product without it", () => {
    const flag = (value: boolean) => ({
      name: "Wireless",
      label: "Wireless",
      value,
      hidden: false,
      propertyType: PropertyType.Product,
      propertyValueType: PropertyValueTypes.Boolean,
    });
    setCategories([
      category("printers", "Printers", [
        item("1", { product: product("product-1", { properties: [flag(true)] }) }),
        item("2", { product: product("product-2", { properties: [flag(false)] }) }),
        item("3"),
      ]),
    ]);

    const row = runPage().tableRows.value.find((x) => x.key === "property:wireless")!;

    expect(row.kind).toBe("boolean");
    expect(row.boolValues).toEqual([true, false, undefined]);
    expect(row.differs).toBe(true);
  });
});
