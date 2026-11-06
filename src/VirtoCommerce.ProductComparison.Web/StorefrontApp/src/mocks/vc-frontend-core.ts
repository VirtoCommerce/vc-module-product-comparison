// Test-only resolution target for "@vc-frontend/core" (see the alias in vitest.config.ts).
// Mocking the facade is wholesale, so this carries working defaults and specs SPREAD it:
//
//   vi.mock("@vc-frontend/core", async (importOriginal) => ({
//     ...(await importOriginal<Record<string, unknown>>()),
//     globals: { storeId: "test-store" },
//   }));
//
// Each symbol is host-faithful where a spec relies on its behaviour: same names, same shapes.
import { computed, defineComponent, h, ref } from "vue";

export const SUPPRESS_ERROR_NOTIFICATIONS_CONTEXT = { suppressErrorNotifications: true };

export const Logger = { error: () => {}, warn: () => {}, info: () => {}, debug: () => {} };

export const ROUTES = { SIGN_IN: { NAME: "SignIn", PATH: "/sign-in" } };

export const BREAKPOINTS = { xs: 0, sm: 480, md: 768, lg: 1024, xl: 1280, "2xl": 1536 };

export const globals = {
  storeId: "test-store",
  userId: "test-user",
  cultureName: "en-US",
  currencyCode: "USD",
  i18n: undefined,
  router: undefined,
};

export const apolloClient = {
  query: () => Promise.resolve({ data: {} }),
  mutate: () => Promise.resolve({ data: {} }),
};

export const getProductRoute = (id: string, slug?: string) => (slug ? `/${slug}` : `/product/${id}`);

export const InStock = defineComponent({
  name: "InStock",
  props: { isInStock: Boolean, isAvailable: Boolean, isDigital: Boolean, quantity: Number },
  setup: (props) => () => h("span", { class: "in-stock" }, String(props.quantity)),
});

export const useUser = () => ({ isAuthenticated: computed(() => false) });
export const useModuleSettings = () => ({
  isEnabled: () => true,
  getSettingValue: () => undefined,
});
export const useModal = () => ({ openModal: () => () => {}, closeModal: () => {} });
export const useNotifications = () => ({
  success: () => {},
  error: () => {},
  warning: () => {},
  info: () => {},
  clear: () => {},
});
export const useExtensionRegistry = () => ({ register: () => {}, registerContribution: () => {} });
export const useThemeContext = () => ({ themeContext: ref({ settings: {} }) });
export const useAnalytics = () => ({ analytics: () => {} });
export const useAnalyticsUtils = () => ({ trackAddItemToCart: () => {} });
export const useHistoricalEvents = () => ({ pushHistoricalEvent: () => Promise.resolve() });

type PropertyType = { name: string; label?: string; value?: unknown; hidden?: boolean };

// The host's grouping, without its value formatting: hidden properties out, one entry per name, multivalues joined.
export function getPropertiesGroupedByName(properties: PropertyType[]): Record<string, PropertyType> {
  return properties.reduce<Record<string, PropertyType>>((grouped, property) => {
    if (property.hidden || property.value === undefined) {
      return grouped;
    }

    const existing = grouped[property.name];
    grouped[property.name] = existing
      ? { ...existing, value: `${String(existing.value)}, ${String(property.value)}` }
      : { ...property, label: property.label || property.name };
    return grouped;
  }, {});
}
export const useErrorsTranslator = () => ({ translate: () => undefined });
export const useQuantityValidationSchema = () => ({
  quantitySchema: computed(() => ({ isValidSync: () => true })),
});
export const useShortCart = () => ({ cart: ref(), addToCart: () => Promise.resolve(undefined) });
export const useBreadcrumbs = (items: unknown[]) => computed(() => items);
export const usePageHead = () => {};
export const registerLocaleLoader = () => {};
export const registerProductConfigurationSource = () => {};
