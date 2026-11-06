import { beforeEach, describe, expect, it, vi } from "vitest";
import pluginConfig from "../plugin.config";

const host = vi.hoisted(() => ({
  addRoute: vi.fn(),
  register: vi.fn(),
  registerContribution: vi.fn(),
  registerProductConfigurationSource: vi.fn(),
  loadCompareList: vi.fn(),
}));

vi.mock("@vc-frontend/core", async (importOriginal) => ({
  ...(await importOriginal<Record<string, unknown>>()),
  globals: {
    storeId: "B2B-store",
    router: { addRoute: host.addRoute },
    i18n: { global: { locale: "en", getLocaleMessage: () => ({}), setLocaleMessage: vi.fn() } },
  },
  useExtensionRegistry: () => ({ register: host.register, registerContribution: host.registerContribution }),
  registerProductConfigurationSource: host.registerProductConfigurationSource,
}));

vi.mock("./composables/useCompareList", () => ({
  loadCompareList: host.loadCompareList,
  getItemConfiguration: vi.fn(),
  useCompareCount: vi.fn(),
}));

beforeEach(() => {
  vi.clearAllMocks();
  host.loadCompareList.mockResolvedValue(undefined);
});

// What the storefront shows comes from two places that must agree: the declarations in plugin.config.ts (known
// before any of this plugin's code loads) and what init() registers when it runs.
describe("plugin init", () => {
  it("adds the compare page on the route the declaration names", async () => {
    const { init } = await import("./index");

    init();

    const declared = pluginConfig.routes?.[0];
    expect(host.addRoute).toHaveBeenCalledWith(expect.objectContaining({ path: declared?.path, name: declared?.name }));
    expect(declared).toEqual(expect.objectContaining({ path: "/compare", name: "CompareProducts" }));
  });

  // The button fills the slot the declaration reserves on the cards and the product page.
  it("registers the button in the productActions slot it declares", async () => {
    const { init } = await import("./index");

    init();

    expect(host.register).toHaveBeenCalledWith(
      "productActions",
      "compare",
      expect.objectContaining({ component: expect.anything() }),
    );
    expect(pluginConfig.slots).toEqual([{ at: "productActions/compare", policy: "reserve" }]);
  });

  // The header and mobile menus draw the declared "Compare" link themselves; the plugin adds only the count.
  it("puts the count on the declared header link, desktop and mobile", async () => {
    const { init } = await import("./index");

    init();

    const headerLink = pluginConfig.menu?.find((entry) => "group" in entry && entry.group === "main");
    expect(host.registerContribution).toHaveBeenCalledWith("headerMenu", headerLink?.id, expect.anything());
    expect(host.registerContribution).toHaveBeenCalledWith("mobileMenu", headerLink?.id, expect.anything());
  });

  it("answers the product page for its compareItemId links and loads the list", async () => {
    const { init } = await import("./index");

    init();

    expect(host.registerProductConfigurationSource).toHaveBeenCalledWith("compareItemId", expect.any(Function));
    expect(host.loadCompareList).toHaveBeenCalledOnce();
  });
});

// AC 4 and AC 7: a store with compare off, or a storefront without the module, has no compare at all. The
// plugin-level condition keeps the host from loading any of it.
describe("plugin declarations", () => {
  it("load the plugin only where the store enables product comparison", () => {
    expect(pluginConfig.when).toEqual({
      module: "VirtoCommerce.ProductComparison",
      setting: "ProductComparison.Enabled",
    });
  });

  // Decision D6: "Compare" stays in the header and also appears under My Account > Purchasing.
  it("link the page from the header and from My Account > Purchasing", () => {
    expect(pluginConfig.menu).toEqual([
      expect.objectContaining({ surface: "header", group: "main", routeName: "CompareProducts" }),
      expect.objectContaining({ surface: "header", group: "purchasing", routeName: "CompareProducts" }),
    ]);
  });
});
