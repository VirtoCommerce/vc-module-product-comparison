import { describe, expect, it, vi } from "vitest";
import {
  getConfigurationLink,
  getItemConfigurationKey,
  getProductCategoryId,
  getSelectionConfigurationKey,
  toConfigurationSectionInputs,
} from "./utils";
import type { CompareProductFragment } from "./api/graphql/types";
import type { CompareListItemType, ComparePageItemType } from "./types";

vi.mock("@vc-frontend/core", async (importOriginal) => ({
  ...(await importOriginal<Record<string, unknown>>()),
  getProductRoute: (id: string, slug?: string) => (slug ? `/${slug}` : { name: "Product", params: { productId: id } }),
}));

function breadcrumb(itemId: string, typeName = "Category") {
  return { itemId, typeName };
}

function item(overrides: Partial<CompareListItemType> = {}): CompareListItemType {
  return { id: "item-1", productId: "product-1", categoryId: "category-1", configurationSections: [], ...overrides };
}

describe("getProductCategoryId", () => {
  // The client names the category (decision D2): the deepest of the first two category breadcrumbs, as the
  // in-browser compare grouped products before.
  it("is the second category from the root", () => {
    const product = { breadcrumbs: [breadcrumb("root"), breadcrumb("mid"), breadcrumb("leaf")] };

    expect(getProductCategoryId(product)).toBe("mid");
  });

  it("is the only category of a product directly under a root category", () => {
    expect(getProductCategoryId({ breadcrumbs: [breadcrumb("root")] })).toBe("root");
  });

  it("skips catalog and product breadcrumbs", () => {
    const product = {
      breadcrumbs: [breadcrumb("catalog", "Catalog"), breadcrumb("root"), breadcrumb("product-1", "CatalogProduct")],
    };

    expect(getProductCategoryId(product)).toBe("root");
  });

  // An uncategorized product goes to the list's uncategorized group, which the server keys by a null category.
  it("is undefined for a product without categories", () => {
    expect(getProductCategoryId({ breadcrumbs: [] })).toBeUndefined();
    expect(getProductCategoryId({})).toBeUndefined();
  });
});

describe("configuration keys", () => {
  // The product page's button finds "its" item by the configurator's current selection, the same way the server
  // decides two saves are the same item. The keys must match across the two shapes (the selection's option is
  // nested, the stored section's fields are flat) and ignore the order sections arrive in.
  it("match a stored configuration and the same selection, whatever the section order", () => {
    const stored = item({
      configurationSections: [
        { sectionId: "b", type: "Text", customText: "engraving" },
        { sectionId: "a", type: "Product", productId: "option-1", quantity: 2 },
      ],
    });

    const selection = [
      { sectionId: "a", type: "Product", option: { productId: "option-1", quantity: 2 } },
      { sectionId: "b", type: "Text", customText: "engraving" },
    ];

    expect(getSelectionConfigurationKey(selection)).toBe(getItemConfigurationKey(stored));
  });

  it("differ when an option's quantity differs", () => {
    const stored = item({
      configurationSections: [{ sectionId: "a", type: "Product", productId: "option-1", quantity: 2 }],
    });

    const selection = [{ sectionId: "a", type: "Product", option: { productId: "option-1", quantity: 3 } }];

    expect(getSelectionConfigurationKey(selection)).not.toBe(getItemConfigurationKey(stored));
  });

  // Fields joined with "|" and sections with "\n" read one text section that spells out a second section the same as
  // the two sections themselves: the product page showed a configuration as compared when only the other one was.
  it("tell a text that spells out another section from the two sections", () => {
    const oneSection = [{ sectionId: "s1", type: "Text", customText: "x\ns2|Text|||y" }];
    const twoSections = [
      { sectionId: "s1", type: "Text", customText: "x" },
      { sectionId: "s2", type: "Text", customText: "y" },
    ];

    expect(getSelectionConfigurationKey(oneSection)).not.toBe(getSelectionConfigurationKey(twoSections));
  });

  // A card passes no selection at all, and the product page passes an empty one for a plain product: both are the
  // plain item, so the button on either shows the same state.
  it("of a plain product equal no selection and an empty one", () => {
    expect(getItemConfigurationKey(item())).toBe(getSelectionConfigurationKey());
    expect(getItemConfigurationKey(item())).toBe(getSelectionConfigurationKey([]));
  });
});

describe("toConfigurationSectionInputs", () => {
  it("rebuilds the selection a configured item was saved with", () => {
    const stored = item({
      configurationSections: [
        { sectionId: "a", type: "Product", productId: "option-1", quantity: 2 },
        { sectionId: "b", type: "Text", customText: "engraving" },
      ],
    });

    expect(toConfigurationSectionInputs(stored)).toEqual([
      { sectionId: "a", type: "Product", option: { productId: "option-1", quantity: 2 }, customText: undefined },
      { sectionId: "b", type: "Text", option: undefined, customText: "engraving" },
    ]);
  });
});

describe("getConfigurationLink", () => {
  const product = { id: "product-1", slug: "printers/printer-1" } as CompareProductFragment;

  // "Customize" opens the product page with the item's configuration preselected. The link carries only the item's
  // id: the plugin's configuration source finds the item in the viewer's own list (decision §11.16, JSON in the URL
  // was dropped for its size).
  it("adds the item id for a configured item", () => {
    const configured = { ...item(), configurationSections: [{ sectionId: "a", type: "Text", customText: "x" }] };

    expect(getConfigurationLink(configured as ComparePageItemType, product)).toEqual({
      path: "/printers/printer-1",
      query: { compareItemId: "item-1" },
    });
  });

  // A configurable product compared from a card has no configuration of its own: the product page opens with its
  // defaults, so the link carries nothing.
  it("is the plain product route for an item without a configuration", () => {
    expect(getConfigurationLink(item() as ComparePageItemType, product)).toEqual({ path: "/printers/printer-1" });
  });
});
