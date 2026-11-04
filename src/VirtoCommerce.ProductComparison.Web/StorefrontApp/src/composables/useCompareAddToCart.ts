import {
  useAnalyticsUtils,
  useErrorsTranslator,
  useHistoricalEvents,
  useNotifications,
  useQuantityValidationSchema,
  useShortCart,
} from "@vc-frontend/core";
import { ref } from "vue";
import { useI18n } from "vue-i18n";
import { CATALOG_PRODUCT_ERROR_OBJECT_TYPE, NOTIFICATION_PRODUCT_NAME_MAX_LENGTH } from "../constants";
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
  const { translate } = useErrorsTranslator<ValidationErrorType>("validation_error");
  const { addToCart } = useShortCart();
  const { trackAddItemToCart } = useAnalyticsUtils();
  const { pushHistoricalEvent } = useHistoricalEvents();
  const notifications = useNotifications();

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

      // As the host's own add-to-cart buttons report it. The analytics mapper reads only what the compare query
      // selects: code, name, vendor, price, properties, breadcrumbs.
      trackAddItemToCart(product as unknown as ProductActionPropsType["product"], quantity, {
        source_block: "compare",
      });
      void pushHistoricalEvent({ eventType: "addToCart", productId: product.id });

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
