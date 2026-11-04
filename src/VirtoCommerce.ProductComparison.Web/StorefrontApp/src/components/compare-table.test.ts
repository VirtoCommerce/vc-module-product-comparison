import { mount } from "@vue/test-utils";
import { afterEach, describe, expect, it, vi } from "vitest";
import { defineComponent, h, nextTick } from "vue";
import CompareTable from "./compare-table.vue";
import type { CompareProductFragment } from "../api/graphql/types";
import type { ICompareColumn, ICompareTableRow } from "../types";
import type { DOMWrapper } from "@vue/test-utils";

vi.mock("vue-i18n", () => ({
  useI18n: () => ({ t: (key: string) => key }),
}));

const tableRows: { value: ICompareTableRow[] } = { value: [] };

vi.mock("../composables/useCompareAddToCart", () => ({
  useCompareAddToCart: () => ({
    isAddingToCart: () => false,
    isAddToCartDisabled: () => false,
    onAddToCart: vi.fn(),
  }),
}));

vi.mock("../composables/useCompareTableRowPins", () => ({
  useCompareTableRowPins: () => ({
    isRowPinned: () => false,
    togglePin: vi.fn(),
    pinnedRows: { value: [] },
    unpinnedRows: tableRows,
  }),
}));

// isCompact derives from useElementBounding's headerRowTop against useCssVar's app header height, both real layout
// reads jsdom can't produce. One shared, real ref lets a case flip isCompact deterministically (and reactively: a live
// watch on it is what's under test) by moving headerRowTop below or above the header height, fixed at 0.
vi.mock("@vueuse/core", async (importOriginal) => {
  const actual = await importOriginal<typeof import("@vueuse/core")>();
  const { ref: vueRef } = await import("vue");
  const headerRowTop = vueRef(100); // > appHeaderHeight (0) + 1, i.e. not compact by default
  const isMobile = vueRef(false);

  return {
    ...actual,
    useBreakpoints: () => ({ smaller: () => isMobile }),
    useElementBounding: () => ({ top: headerRowTop, update: vi.fn() }),
    useCssVar: () => vueRef("0"),
    __headerRowTop: headerRowTop,
    __isMobile: isMobile,
  };
});

const VcWidgetStub = defineComponent({
  name: "VcWidget",

  setup(_, { slots }) {
    return () => h("div", slots["default-container"]?.());
  },
});

const VcButtonStub = defineComponent({
  name: "VcButton",
  props: { to: { type: null, default: undefined } },
  emits: { click: () => true },

  setup(props, { emit, slots }) {
    return () =>
      h(
        "button",
        { type: "button", "data-to": JSON.stringify(props.to), onClick: () => emit("click") },
        slots.default?.(),
      );
  },
});

const VcTabSwitchStub = defineComponent({
  name: "VcTabSwitch",
  props: { value: { type: null, default: undefined }, label: { type: String, default: "" } },
  emits: { change: (value: unknown) => value !== undefined },

  setup(props, { emit }) {
    return () => h("button", { type: "button", onClick: () => emit("change", props.value) }, props.label);
  },
});

const VcProductActionsButtonStub = defineComponent({
  name: "VcProductActionsButton",
  emits: { click: () => true },

  setup(_, { emit }) {
    return () => h("button", { type: "button", onClick: () => emit("click") }, "remove");
  },
});

const PassthroughStub = (name: string, tag = "div") =>
  defineComponent({
    name,
    setup:
      (_, { slots }) =>
      () =>
        h(tag, slots.default?.()),
  });

function column(id: string, overrides: Partial<CompareProductFragment> = {}): ICompareColumn {
  return {
    item: { id: `item-${id}`, productId: id, configurationSections: [] },
    product: {
      id,
      slug: id,
      name: `Product ${id}`,
      isConfigurable: false,
      hasVariations: false,
      ...overrides,
    } as CompareProductFragment,
  };
}

function unavailable(id: string): ICompareColumn {
  return { item: { id: `item-${id}`, productId: id, configurationSections: [] } };
}

async function setCompact(compact: boolean) {
  const vueuseCore = (await import("@vueuse/core")) as unknown as { __headerRowTop: { value: number } };
  vueuseCore.__headerRowTop.value = compact ? 0 : 100;
}

async function setMobile(mobile: boolean) {
  const vueuseCore = (await import("@vueuse/core")) as unknown as { __isMobile: { value: boolean } };
  vueuseCore.__isMobile.value = mobile;
}

function row(key: string, values: string[]): ICompareTableRow {
  return { key, label: `Label ${key}`, kind: "text", values, differs: false };
}

function mountTable(props: { columns?: ICompareColumn[] } = {}) {
  // attachTo: a real DOM connection is what document.activeElement tracks; a detached mount (the default) makes
  // every .focus() in this file a silent no-op.
  return mount(CompareTable, {
    attachTo: document.body,
    props: { columns: props.columns ?? [], rows: [] },
    global: {
      mocks: { $t: (key: string) => key },
      stubs: {
        VcWidget: VcWidgetStub,
        VcButton: VcButtonStub,
        VcTabSwitch: VcTabSwitchStub,
        VcProductActionsButton: VcProductActionsButtonStub,
        VcProductActions: PassthroughStub("VcProductActions"),
        VcTooltip: defineComponent({
          name: "VcTooltip",
          setup:
            (_, { slots }) =>
            () =>
              h("span", slots.trigger?.()),
        }),
        VcImage: true,
        VcProductTitle: true,
        VcProductPrice: true,
        VcRating: true,
        VcIcon: true,
        InStock: true,
      },
    },
  });
}

afterEach(async () => {
  await setCompact(false);
  await setMobile(false);
  tableRows.value = [];
});

describe("CompareTable — focus management", () => {
  it("does not steal focus on an isCompact flip when the previously focused control survives it (e.g. an All/Differences tab)", async () => {
    await setCompact(false);
    // 2 products, not 0: the tabs are disabled (and so unfocusable) at 1 or fewer.
    const wrapper = mountTable({ columns: [column("p1"), column("p2")] });

    // The tabs are teleported, gated by v-if="mobileTabsBarRef": null on the first render, so they only appear once
    // that ref populates and triggers a second render pass.
    await nextTick();

    const tabButton = wrapper.get(".compare-table__tab").element as HTMLElement;
    tabButton.focus();
    expect(document.activeElement).toBe(tabButton);

    await setCompact(true);
    await nextTick();
    await nextTick();

    expect(document.activeElement).toBe(tabButton);
    wrapper.unmount();
  });

  it("moves focus to the header row when the focused control is destroyed by an isCompact flip (e.g. Clear category disappearing)", async () => {
    await setCompact(false);
    const wrapper = mountTable();

    await nextTick();

    const clearCategoryButton = wrapper.get(".compare-table__clear-category").element as HTMLElement;
    clearCategoryButton.focus();
    expect(document.activeElement).toBe(clearCategoryButton);

    await setCompact(true);
    await nextTick();
    await nextTick();

    expect(clearCategoryButton.isConnected).toBe(false);
    expect(document.activeElement).toBe(wrapper.get(".compare-table__header-row").element);
    wrapper.unmount();
  });

  it("moves focus to the header row once a product is removed, since removing it can destroy whatever held focus", async () => {
    await setCompact(false);
    const columns = [column("p1")];
    const wrapper = mountTable({ columns });

    await wrapper.get(".compare-table__product-remove button").trigger("click");
    expect(wrapper.emitted("removeProduct")).toEqual([[columns[0]]]);

    await nextTick();
    await nextTick();

    expect(document.activeElement).toBe(wrapper.get(".compare-table__header-row").element);
    wrapper.unmount();
  });
});

describe("CompareTable — table semantics", () => {
  function cellRole(cell: DOMWrapper<Element>) {
    if (cell.element.tagName === "TD") {
      return "cell";
    }

    const scope = cell.attributes("scope");

    if (scope === "col") {
      return "columnheader";
    }

    return scope === "row" ? "rowheader" : `th[scope=${scope}]`;
  }

  function rowsOf(wrapper: ReturnType<typeof mountTable>) {
    const tables = wrapper.findAll("table");
    expect(tables).toHaveLength(1);

    return tables[0]
      .findAll("tr")
      .map((rowWrapper) =>
        rowWrapper
          .findAll("td, th")
          .map((cell) => [cellRole(cell), cell.element.tagName === "TH" ? cell.attributes("aria-label") : cell.text()]),
      );
  }

  it("puts the product header row and every attribute row in one table, column for column", async () => {
    tableRows.value = [row("sku", ["SKU-1", "SKU-2"]), row("color", ["Red", "Blue"])];
    const wrapper = mountTable({ columns: [column("p1"), column("p2")] });
    await nextTick();

    expect(wrapper.get("table").attributes("aria-label")).toBe("product_comparison.page.title");
    expect(rowsOf(wrapper)).toEqual([
      [
        ["cell", expect.stringContaining("product_comparison.table.tabs.all")],
        ["columnheader", "Product p1"],
        ["columnheader", "Product p2"],
      ],
      [
        ["rowheader", "Label sku"],
        ["cell", "SKU-1"],
        ["cell", "SKU-2"],
      ],
      [
        ["rowheader", "Label color"],
        ["cell", "Red"],
        ["cell", "Blue"],
      ],
    ]);

    wrapper.unmount();
  });

  it("keeps the product columns once the header row turns compact", async () => {
    tableRows.value = [row("sku", ["SKU-1", "SKU-2"])];
    const wrapper = mountTable({ columns: [column("p1"), column("p2")] });
    await nextTick();

    await setCompact(true);
    await nextTick();

    expect(wrapper.findAll(".compare-table__product--compact")).toHaveLength(2);
    expect(rowsOf(wrapper)[0]).toEqual([
      ["cell", expect.any(String)],
      ["columnheader", "Product p1"],
      ["columnheader", "Product p2"],
    ]);

    wrapper.unmount();
  });

  it("keeps the corner cell, and the tabs out of the table, on mobile", async () => {
    await setMobile(true);
    tableRows.value = [row("sku", ["SKU-1", "SKU-2"])];
    const wrapper = mountTable({ columns: [column("p1"), column("p2")] });
    await nextTick();

    const table = wrapper.get("table");
    expect(table.find(".compare-table__tabs").exists()).toBe(false);
    expect(wrapper.find(".compare-table__mobile-tabs-bar .compare-table__tabs").exists()).toBe(true);
    expect(rowsOf(wrapper)).toEqual([
      [
        ["cell", ""],
        ["columnheader", "Product p1"],
        ["columnheader", "Product p2"],
      ],
      [
        ["rowheader", "Label sku"],
        ["cell", "SKU-1"],
        ["cell", "SKU-2"],
      ],
    ]);

    wrapper.unmount();
  });

  it("scrolls the header row along with the body", async () => {
    tableRows.value = [row("sku", ["SKU-1", "SKU-2"])];
    const wrapper = mountTable({ columns: [column("p1"), column("p2")] });
    await nextTick();

    const body = wrapper.get("tbody").element;
    body.scrollLeft = 120;
    body.dispatchEvent(new Event("scroll"));

    expect(wrapper.get("thead").element.scrollLeft).toBe(120);

    wrapper.unmount();
  });

  it("describes a row header by its description, and only when it has one", async () => {
    tableRows.value = [{ ...row("price", ["$1", "$2"]), description: "Price, excl. VAT" }, row("sku", ["A", "B"])];
    const wrapper = mountTable({ columns: [column("p1"), column("p2")] });
    await nextTick();

    const [priceHeader, skuHeader] = wrapper.findAll("th[scope=row]");
    const describedBy = priceHeader.attributes("aria-describedby");

    expect(priceHeader.attributes("aria-label")).toBe("Label price");
    expect(describedBy).toBeTruthy();
    expect(wrapper.get(`[id="${describedBy}"]`).attributes("label")).toBe("Price, excl. VAT");
    expect(skuHeader.attributes("aria-describedby")).toBeUndefined();

    wrapper.unmount();
  });
});

describe("CompareTable — selectItem", () => {
  it.each([false, true])("emits the clicked column's position, compact: %s", async (compact) => {
    await setCompact(compact);
    // The same product twice, in two configurations: the event names the clicked column, not the first match.
    const columns = [column("p1"), { ...column("p1"), item: { ...column("p1").item, id: "item-p1-b" } }];
    const wrapper = mountTable({ columns });
    await nextTick();

    wrapper.findAllComponents({ name: "VcProductTitle" })[1].vm.$emit("click");

    expect(wrapper.emitted("selectItem")).toEqual([[columns[1].product, 1]]);

    wrapper.unmount();
  });
});

// Decision §11.5: an item whose product no longer resolves stays a column, says so, and can be removed.
describe("CompareTable — a product that is no longer available", () => {
  it("is a named column with placeholder cells", async () => {
    tableRows.value = [row("sku", ["SKU-1", "–"])];
    const wrapper = mountTable({ columns: [column("p1"), unavailable("gone")] });
    await nextTick();

    const headers = wrapper.findAll("th[scope=col]");
    expect(headers[1].attributes("aria-label")).toBe("common.messages.product_no_longer_available");
    expect(headers[1].text()).toContain("common.messages.product_no_longer_available");
    expect(wrapper.findAll("tbody td").map((cell) => cell.text())).toEqual(["SKU-1", "–"]);

    wrapper.unmount();
  });

  it.each([false, true])("can be removed, compact: %s", async (compact) => {
    await setCompact(compact);
    const gone = unavailable("gone");
    const wrapper = mountTable({ columns: [column("p1"), gone] });
    await nextTick();

    const header = wrapper.findAll("th[scope=col]")[1];
    await header.get("button").trigger("click");

    expect(wrapper.emitted("removeProduct")).toEqual([[gone]]);

    wrapper.unmount();
  });

  // With one product left to compare there is nothing to filter: the All/Differences switch stays off. Two products
  // beside the unavailable one turn it on.
  it.each([
    [[column("p1"), unavailable("gone")], true],
    [[column("p1"), column("p2"), unavailable("gone")], false],
  ])("does not count as a product for the Differences filter (%#)", async (columns, disabled) => {
    const wrapper = mountTable({ columns });
    await nextTick();

    const tabs = wrapper.findAllComponents({ name: "VcTabSwitch" });
    expect(tabs).toHaveLength(2);
    expect(tabs.map((tab) => tab.attributes("disabled") !== undefined)).toEqual([disabled, disabled]);

    wrapper.unmount();
  });
});

// Decision §11.16: "Customize" reopens the product page with the compared configuration, by the item's id.
describe("CompareTable — Customize", () => {
  it("links a configured item to its product page by the item's id", async () => {
    const configured: ICompareColumn = {
      ...column("p1", { isConfigurable: true }),
      item: {
        id: "item-configured",
        productId: "p1",
        configurationSections: [{ sectionId: "a", type: "Text", customText: "engraving" }],
      },
    };
    const wrapper = mountTable({ columns: [configured] });
    await nextTick();

    const links = wrapper.findAll("button[data-to]").map((button) => button.attributes("data-to"));
    expect(links).toContain(JSON.stringify({ path: "/p1", query: { compareItemId: "item-configured" } }));

    wrapper.unmount();
  });
});
