import {
  apolloClient,
  globals,
  Logger,
  useAnalytics,
  useErrorsTranslator,
  useModuleSettings,
  useNotifications,
  useQuantityValidationSchema,
  useShortCart,
  useUser,
} from "@vc-frontend/core";
import { ref } from "vue";
import { useI18n } from "vue-i18n";
import { useRoute } from "vue-router";
import { PushHistoricalEventDocument } from "../api/graphql/storefront/types";
import {
  CATALOG_PRODUCT_ERROR_OBJECT_TYPE,
  NOTIFICATION_PRODUCT_NAME_MAX_LENGTH,
  XRECOMMEND_ENABLED_KEY,
  XRECOMMEND_MODULE_ID,
} from "../constants";
import { truncate } from "../utils";
import type { CompareProductFragment } from "../api/graphql/types";
import type { ProductActionPropsType } from "@vc-frontend/core";

type ValidationErrorType = { errorCode?: string; objectId?: string; objectType?: string };

const NOTIFICATION_DURATION = 4000;

function getQuantity(product: CompareProductFragment): number {
  const minQuantity = product.minQuantity || 1;
  return product.packSize ? Math.ceil(minQuantity / product.packSize) * product.packSize : minQuantity;
}

/**
 * "Add to cart" for the table's plain products: a button, no stepper. The quantity rules are the host's own, as
 * its quantity controls check them, and each column has its own busy state.
 */
export function useCompareAddToCart() {
  const { t } = useI18n();
  const route = useRoute();
  const { translate } = useErrorsTranslator<ValidationErrorType>("validation_error");
  const { cart, addToCart } = useShortCart();
  const { isAuthenticated } = useUser();
  const { analytics } = useAnalytics();
  const notifications = useNotifications();
  const recommendationsEnabled = useModuleSettings(XRECOMMEND_MODULE_ID).isEnabled(XRECOMMEND_ENABLED_KEY);

  const addingItemIds = ref(new Set<string>());

  // One schema, re-pointed at the product being checked: the host's schema takes refs, for a single product's form.
  const minQuantity = ref<number>();
  const maxQuantity = ref<number>();
  const availableQuantity = ref<number>();
  const packSize = ref<number>();
  const { quantitySchema } = useQuantityValidationSchema({ minQuantity, maxQuantity, availableQuantity, packSize });

  function isAddToCartDisabled(product: CompareProductFragment): boolean {
    const availability = product.availabilityData;

    if (!availability.isActive || !availability.isAvailable || !availability.isBuyable || !availability.isInStock) {
      return true;
    }

    minQuantity.value = product.minQuantity;
    maxQuantity.value = product.maxQuantity;
    availableQuantity.value = availability.availableQuantity;
    packSize.value = product.packSize;

    return !quantitySchema.value.isValidSync(getQuantity(product));
  }

  function isAddingToCart(itemId: string): boolean {
    return addingItemIds.value.has(itemId);
  }

  // The event recommendations learn from, as the host's own add-to-cart buttons send it.
  function pushAddToCartEvent(productId: string) {
    if (!recommendationsEnabled || !isAuthenticated.value) {
      return;
    }

    apolloClient
      .mutate({
        mutation: PushHistoricalEventDocument,
        variables: {
          command: { eventType: "addToCart", productId, sessionId: cart.value?.id, storeId: globals.storeId },
        },
      })
      .catch((error: unknown) => Logger.error("[product-comparison] historical event", error));
  }

  async function onAddToCart(itemId: string, product: CompareProductFragment) {
    if (addingItemIds.value.has(itemId)) {
      return;
    }

    addingItemIds.value.add(itemId);

    try {
      const quantity = getQuantity(product);
      const updatedCart = await addToCart(product.id, quantity);
      const validationMessages =
        updatedCart?.validationErrors
          ?.filter((error) => error.objectId === product.id && error.objectType === CATALOG_PRODUCT_ERROR_OBJECT_TYPE)
          .map(translate)
          .filter(Boolean) ?? [];

      if (!updatedCart || validationMessages.length) {
        notifications.error({
          text: t("common.messages.fail_add_product_to_cart", { reason: validationMessages.join(" ") }),
          duration: NOTIFICATION_DURATION,
        });
        return;
      }

      analytics("addItemToCart", product as unknown as ProductActionPropsType["product"], quantity, {
        source_route: typeof route.name === "string" ? route.name : route.path,
        source_block: "compare",
      });
      pushAddToCartEvent(product.id);

      notifications.success({
        html: t("product_comparison.notifications.added_to_cart_html", {
          productName: truncate(product.name, NOTIFICATION_PRODUCT_NAME_MAX_LENGTH),
        }),
        duration: NOTIFICATION_DURATION,
      });
    } finally {
      addingItemIds.value.delete(itemId);
    }
  }

  return {
    isAddingToCart,
    isAddToCartDisabled,
    onAddToCart,
  };
}
