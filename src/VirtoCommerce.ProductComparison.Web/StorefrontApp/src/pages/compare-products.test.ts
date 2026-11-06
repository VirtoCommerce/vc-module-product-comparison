import { flushPromises, mount } from "@vue/test-utils";
import { createWrapperFactory } from "@vc-frontend/core/testing";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { defineComponent, h } from "vue";
import ComparePage from "./compare-products.vue";
import type { ICompareColumn } from "../types";

const store = vi.hoisted(() => ({
  remove: vi.fn(),
  clear: vi.fn(),
  clearCategory: vi.fn(),
  restore: vi.fn(),
  clearRestoreBuffer: vi.fn(),
  reload: vi.fn(),
}));

const page = vi.hoisted(() => ({
  refetch: vi.fn(),
  isEmpty: false,
  totalCount: 2,
  failed: false,
  columns: [{ item: { id: "item-1" } }] as { item: { id: string } }[],
}));
const modal = vi.hoisted(() => ({ openModal: vi.fn(), closeModal: vi.fn() }));

vi.mock("../composables/useCompareList", async () => {
  const { computed } = await import("vue");
  return {
    useCompareList: () => ({ ...store, maxItems: computed(() => 5), canRestore: computed(() => true) }),
  };
});

vi.mock("../composables/useComparePage", async () => {
  const { computed, ref } = await import("vue");
  return {
    useComparePage: () => ({
      loading: ref(false),
      failed: computed(() => page.failed),
      isEmpty: computed(() => page.isEmpty),
      totalCount: computed(() => page.totalCount),
      categoryTabs: ref([{ key: "printers", label: "Printers", count: 2 }]),
      selectedCategory: ref({ id: "printers" }),
      selectedCategoryKey: ref("printers"),
      selectedCategoryLabel: ref("Printers"),
      selectedCategoryCount: ref(2),
      columns: computed(() => page.columns),
      tableRows: ref([]),
      refetch: page.refetch,
      selectCategory: vi.fn(),
      selectItemEvent: vi.fn(),
    }),
  };
});

vi.mock("@vc-frontend/core", async (importOriginal) => ({
  ...(await importOriginal<Record<string, unknown>>()),
  useModal: () => modal,
}));

let visibility: DocumentVisibilityState = "visible";

function setVisibility(value: DocumentVisibilityState) {
  visibility = value;
  document.dispatchEvent(new Event("visibilitychange"));
}

// The host registers its ui-kit globally; here each renders as a plain element with all of its slots, so the empty
// state's #button slot (Restore) is reachable, and VcButton is a real button.
function passthrough(tag: string) {
  return defineComponent({
    inheritAttrs: false,
    setup:
      (_, { slots, attrs }) =>
      () =>
        h(
          tag,
          attrs,
          Object.values(slots).map((slot) => slot?.()),
        ),
  });
}

const createWrapper = createWrapperFactory(mount, ComparePage, {
  global: {
    stubs: {
      CompareTable: true,
      VcBreadcrumbs: true,
      VcIcon: true,
      VcLoaderOverlay: true,
      VcButton: passthrough("button"),
      VcContainer: passthrough("div"),
      VcWidget: passthrough("div"),
      VcEmptyView: passthrough("div"),
      VcTypography: passthrough("div"),
      VcBadge: passthrough("span"),
    },
  },
});

beforeEach(() => {
  vi.clearAllMocks();
  page.isEmpty = false;
  page.totalCount = 2;
  page.failed = false;
  page.columns = [{ item: { id: "item-1" } }];
  Object.defineProperty(document, "visibilityState", { configurable: true, get: () => visibility });
  setVisibility("visible");
});

afterEach(() => {
  // The page's listeners live on `document`: unmount between cases so one case's page does not answer the next.
  document.body.innerHTML = "";
});

// Decision §11.14: the list may change in another tab or on another device, so the page reloads when the buyer
// comes back to it. Only the compare page does this; the rest of the storefront keeps the list it loaded.
describe("coming back to the page's tab", () => {
  it("reloads the list and the page", async () => {
    const wrapper = createWrapper();

    setVisibility("hidden");
    await flushPromises();
    expect(store.reload).not.toHaveBeenCalled();
    expect(page.refetch).not.toHaveBeenCalled();

    setVisibility("visible");
    await flushPromises();

    expect(store.reload).toHaveBeenCalledOnce();
    expect(page.refetch).toHaveBeenCalledOnce();

    wrapper.unmount();
  });
});

// Deep test C5.9, which the dev server can't reach: a list that failed to load and left nothing on screen shows the
// error state, whose "Try again" asks again. A refetch that fails once the columns are there keeps them on screen:
// the buyer goes on comparing what they see.
describe("a failed load", () => {
  it("shows the error state, whose Try again asks again, when nothing has loaded", async () => {
    page.failed = true;
    page.columns = [];
    const wrapper = createWrapper();

    const errorState = wrapper.get('[data-test-id="compare-error-state"]');
    expect(errorState.get("[text]").attributes("text")).toContain("product_comparison.page.error");

    const retryButton = errorState
      .findAll("button")
      .find((button) => button.text().includes("product_comparison.page.retry"));
    await retryButton!.trigger("click");

    expect(page.refetch).toHaveBeenCalledOnce();

    wrapper.unmount();
  });

  it("keeps the table when a refetch fails after the columns have loaded", () => {
    page.failed = true;
    const wrapper = createWrapper();

    expect(wrapper.find('[data-test-id="compare-error-state"]').exists()).toBe(false);
    expect(wrapper.findComponent({ name: "CompareTable" }).exists()).toBe(true);

    wrapper.unmount();
  });
});

describe("page actions", () => {
  it("clears the whole list after the confirmation, then reloads the page", async () => {
    const wrapper = createWrapper();

    await wrapper.get(".compare-products__clear-all").trigger("click");

    expect(modal.openModal).toHaveBeenCalledWith(
      expect.objectContaining({
        component: "VcConfirmationModal",
        props: expect.objectContaining({ title: "product_comparison.page.clear_all_modal.title" }),
      }),
    );
    expect(store.clear).not.toHaveBeenCalled();

    await modal.openModal.mock.calls[0][0].props.onConfirm();

    expect(store.clear).toHaveBeenCalledOnce();
    expect(page.refetch).toHaveBeenCalledOnce();

    wrapper.unmount();
  });

  it("clears the selected category after the confirmation", async () => {
    const wrapper = createWrapper();

    wrapper.findComponent({ name: "CompareTable" }).vm.$emit("clearCategory");
    await modal.openModal.mock.calls[0][0].props.onConfirm();

    expect(store.clearCategory).toHaveBeenCalledWith("printers");
    expect(page.refetch).toHaveBeenCalledOnce();

    wrapper.unmount();
  });

  it("removes a column's item, naming its product, then reloads the page", async () => {
    const wrapper = createWrapper();
    const column = { item: { id: "item-1" }, product: { name: "LaserJet Pro" } } as ICompareColumn;

    wrapper.findComponent({ name: "CompareTable" }).vm.$emit("removeProduct", column);
    await flushPromises();

    expect(store.remove).toHaveBeenCalledWith(column.item, "LaserJet Pro");
    expect(page.refetch).toHaveBeenCalledOnce();

    wrapper.unmount();
  });

  it("restores what was just cleared from the empty page", async () => {
    page.isEmpty = true;
    page.totalCount = 0;
    const wrapper = createWrapper();

    const restoreButton = wrapper.findAll("button").find((button) => button.text().includes("restore_button"));
    await restoreButton!.trigger("click");
    await flushPromises();

    expect(store.restore).toHaveBeenCalledOnce();
    expect(page.refetch).toHaveBeenCalledOnce();

    wrapper.unmount();
  });

  // "Restore" undoes a clear only on the page that showed the empty list, not after the buyer browsed away.
  it("forgets what it could restore when the buyer leaves", () => {
    const wrapper = createWrapper();

    wrapper.unmount();

    expect(store.clearRestoreBuffer).toHaveBeenCalledOnce();
  });
});
