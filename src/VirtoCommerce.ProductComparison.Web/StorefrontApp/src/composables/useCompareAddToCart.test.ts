import { beforeEach, describe, expect, it, vi } from "vitest";
import { computed, ref } from "vue";
import { useCompareAddToCart } from "./useCompareAddToCart";
import type { CompareProductFragment } from "../api/graphql/types";

const host = vi.hoisted(() => ({
  addToCart: vi.fn(),
  mutate: vi.fn(),
  analytics: vi.fn(),
  notifications: { success: vi.fn(), error: vi.fn() },
  isAuthenticated: true,
  recommendationsEnabled: true,
  quantityValid: true,
  translate: vi.fn(),
}));

vi.mock("vue-i18n", () => ({
  useI18n: () => ({ t: (key: string, values: object = {}) => `${key} ${JSON.stringify(values)}` }),
}));

vi.mock("vue-router", () => ({ useRoute: () => ({ name: "CompareProducts", path: "/compare" }) }));

vi.mock("@vc-frontend/core", async (importOriginal) => ({
  ...(await importOriginal<Record<string, unknown>>()),
  apolloClient: { mutate: host.mutate },
  globals: { storeId: "B2B-store" },
  useShortCart: () => ({ cart: ref({ id: "cart-1" }), addToCart: host.addToCart }),
  useUser: () => ({ isAuthenticated: computed(() => host.isAuthenticated) }),
  useAnalytics: () => ({ analytics: host.analytics }),
  useNotifications: () => host.notifications,
  useErrorsTranslator: () => ({ translate: host.translate }),
  useModuleSettings: () => ({ isEnabled: () => host.recommendationsEnabled }),
  // The host's quantity rules, reduced to their verdict: the product's min/max/pack size/stock are its business.
  useQuantityValidationSchema: () => ({
    quantitySchema: computed(() => ({ isValidSync: () => host.quantityValid })),
  }),
}));

function product(overrides: Partial<CompareProductFragment> = {}): CompareProductFragment {
  return {
    id: "product-1",
    name: "LaserJet Pro",
    code: "SKU-1",
    isConfigurable: false,
    hasVariations: false,
    packSize: 1,
    variations: [],
    availabilityData: { isActive: true, isAvailable: true, isBuyable: true, isInStock: true, availableQuantity: 10 },
    price: { actual: { amount: 10 }, list: { amount: 10 }, discountAmount: { amount: 0 } },
    properties: [],
    breadcrumbs: [],
    ...overrides,
  } as CompareProductFragment;
}

beforeEach(() => {
  vi.clearAllMocks();
  host.isAuthenticated = true;
  host.recommendationsEnabled = true;
  host.quantityValid = true;
  host.addToCart.mockResolvedValue({ validationErrors: [] });
  host.mutate.mockResolvedValue({ data: { pushHistoricalEvent: true } });
});

describe("compare add to cart", () => {
  // No stepper on the compare page: one click adds the smallest quantity the product allows.
  it("adds the minimum quantity, rounded up to the pack size", async () => {
    const { onAddToCart } = useCompareAddToCart();

    await onAddToCart("item-1", product({ minQuantity: 3, packSize: 2 }));

    expect(host.addToCart).toHaveBeenCalledWith("product-1", 4);
    expect(host.notifications.success).toHaveBeenCalledWith(
      expect.objectContaining({ html: expect.stringContaining("LaserJet Pro") }),
    );
  });

  it("reports the cart's validation error for this product", async () => {
    host.addToCart.mockResolvedValue({
      validationErrors: [
        { objectId: "product-1", objectType: "CatalogProduct", errorCode: "PRODUCT_QTY_INSUFFICIENT" },
      ],
    });
    host.translate.mockReturnValue("Not enough in stock");
    const { onAddToCart } = useCompareAddToCart();

    await onAddToCart("item-1", product());

    expect(host.notifications.error).toHaveBeenCalledWith(
      expect.objectContaining({ text: expect.stringContaining("Not enough in stock") }),
    );
    expect(host.notifications.success).not.toHaveBeenCalled();
    expect(host.analytics).not.toHaveBeenCalled();
  });

  // The host's own add-to-cart buttons report the add to analytics and, for a signed-in buyer, to recommendations
  // (with the cart as the session): the compare page does the same.
  it("reports the add to analytics and to recommendations", async () => {
    const { onAddToCart } = useCompareAddToCart();

    await onAddToCart("item-1", product());

    expect(host.analytics).toHaveBeenCalledWith("addItemToCart", expect.objectContaining({ id: "product-1" }), 1, {
      source_route: "CompareProducts",
      source_block: "compare",
    });
    expect(host.mutate).toHaveBeenCalledWith(
      expect.objectContaining({
        variables: {
          command: { eventType: "addToCart", productId: "product-1", sessionId: "cart-1", storeId: "B2B-store" },
        },
      }),
    );
  });

  it.each([
    ["a guest", { isAuthenticated: false, recommendationsEnabled: true }],
    ["recommendations off", { isAuthenticated: true, recommendationsEnabled: false }],
  ])("sends no recommendation event for %s", async (_case, state) => {
    Object.assign(host, state);
    const { onAddToCart } = useCompareAddToCart();

    await onAddToCart("item-1", product());

    expect(host.mutate).not.toHaveBeenCalled();
    expect(host.analytics).toHaveBeenCalledOnce();
  });

  it("is off for a product that can't be bought, and for a quantity the product's rules refuse", () => {
    const { isAddToCartDisabled } = useCompareAddToCart();
    const unbuyable = product({
      availabilityData: { isActive: true, isAvailable: true, isBuyable: false, isInStock: true, availableQuantity: 10 },
    });

    expect(isAddToCartDisabled(product())).toBe(false);
    expect(isAddToCartDisabled(unbuyable)).toBe(true);

    host.quantityValid = false;
    expect(isAddToCartDisabled(product())).toBe(true);
  });

  // Each column has its own busy state, and a second click on the same column waits for the first.
  it("adds a column's product once while its add is running", async () => {
    let finish: (value: unknown) => void = () => {};
    host.addToCart.mockReturnValue(new Promise((resolve) => (finish = resolve)));
    const { onAddToCart, isAddingToCart } = useCompareAddToCart();

    const first = onAddToCart("item-1", product());
    void onAddToCart("item-1", product());

    expect(isAddingToCart("item-1")).toBe(true);
    expect(isAddingToCart("item-2")).toBe(false);

    finish({ validationErrors: [] });
    await first;

    expect(host.addToCart).toHaveBeenCalledOnce();
    expect(isAddingToCart("item-1")).toBe(false);
  });
});
