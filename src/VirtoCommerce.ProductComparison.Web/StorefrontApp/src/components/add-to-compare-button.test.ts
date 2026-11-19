import { mount } from "@vue/test-utils";
import { createWrapperFactory } from "@vc-frontend/core/testing";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { defineComponent, h } from "vue";
import AddToCompareButton from "./add-to-compare-button.vue";
import type { ProductActionPropsType } from "@vc-frontend/core";

const store = vi.hoisted(() => ({ findItem: vi.fn(), add: vi.fn(), remove: vi.fn() }));

vi.mock("../composables/useCompareList", () => ({ useCompareList: () => store }));

// The host's action button, reduced to what this one passes it: the pressed state, the tooltip and the click.
const VcProductActionsButton = defineComponent({
  name: "VcProductActionsButton",
  props: { active: Boolean, tooltipText: String, icon: String },
  emits: ["click"],
  setup:
    (props, { emit }) =>
    () =>
      h("button", { "aria-pressed": String(props.active), title: props.tooltipText, onClick: () => emit("click") }),
});

const product = {
  id: "product-1",
  name: "LaserJet Pro",
  breadcrumbs: [],
} as unknown as ProductActionPropsType["product"];
const configurationSections = [{ sectionId: "a", type: "Text", customText: "engraving" }];

const createWrapper = createWrapperFactory(mount, AddToCompareButton, {
  global: { stubs: { VcProductActionsButton } },
});

beforeEach(() => {
  vi.clearAllMocks();
  store.add.mockResolvedValue(undefined);
  store.remove.mockResolvedValue(undefined);
});

describe("add-to-compare button", () => {
  // On the product page the host passes the configurator's current selection: the button adds and finds the item
  // for exactly that configuration, as the in-browser compare did.
  it("adds the product with the selection it was given", async () => {
    store.findItem.mockReturnValue(undefined);
    const wrapper = createWrapper({ props: { product, configurationSections } });

    expect(store.findItem).toHaveBeenCalledWith("product-1", configurationSections);
    expect(wrapper.get("button").attributes("aria-pressed")).toBe("false");
    expect(wrapper.get("button").attributes("title")).toBe("product_comparison.add_to_compare.tooltips.add");

    await wrapper.get("button").trigger("click");

    expect(store.add).toHaveBeenCalledWith(product, configurationSections);
    expect(store.remove).not.toHaveBeenCalled();
  });

  it("removes the product's item, naming the product", async () => {
    const item = { id: "item-1", productId: "product-1", configurationSections: [] };
    store.findItem.mockReturnValue(item);
    const wrapper = createWrapper({ props: { product } });

    expect(wrapper.get("button").attributes("aria-pressed")).toBe("true");
    expect(wrapper.get("button").attributes("title")).toBe("product_comparison.add_to_compare.tooltips.remove");

    await wrapper.get("button").trigger("click");

    expect(store.remove).toHaveBeenCalledWith(item, "LaserJet Pro");
    expect(store.add).not.toHaveBeenCalled();
  });

  // A double click must not save the product twice: the second click lands while the first request is in flight,
  // before the list says the product is in it.
  it("ignores clicks while its request is running", async () => {
    store.findItem.mockReturnValue(undefined);
    let finish: () => void = () => {};
    store.add.mockReturnValue(new Promise<void>((resolve) => (finish = resolve)));
    const wrapper = createWrapper({ props: { product } });

    await wrapper.get("button").trigger("click");
    await wrapper.get("button").trigger("click");
    finish();

    expect(store.add).toHaveBeenCalledOnce();
  });
});
