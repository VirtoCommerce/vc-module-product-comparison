<template>
  <VcWidget class="compare-table" :shadow="false">
    <template #default-container>
      <!-- Below md the design puts this block above the header row: teleported, so there is one tabs/differ instance. -->
      <div ref="mobileTabsBarRef" class="compare-table__mobile-tabs-bar"></div>

      <!-- An overflow-x wrapper would trap thead's page sticky, so thead and tbody scroll separately (synced in script). -->
      <table class="compare-table__table" :aria-label="$t('product_comparison.page.title')">
        <thead
          ref="headerRowRef"
          class="compare-table__header-row"
          :class="{ 'compare-table__header-row--stuck': isCompact }"
          tabindex="-1"
        >
          <tr class="compare-table__header-inner">
            <td class="compare-table__controls" :class="{ 'compare-table__controls--stuck': isCompact }">
              <Teleport v-if="mobileTabsBarRef" :to="mobileTabsBarRef" :disabled="!isMobile">
                <div class="compare-table__controls-top">
                  <div class="compare-table__tabs">
                    <VcTabSwitch
                      :model-value="activeTab"
                      class="compare-table__tab"
                      size="sm"
                      value="all"
                      :label="$t('product_comparison.table.tabs.all')"
                      :disabled="isTabSwitchDisabled"
                      @change="activeTab = $event"
                    />

                    <VcTabSwitch
                      :model-value="activeTab"
                      class="compare-table__tab"
                      size="sm"
                      value="differences"
                      :label="$t('product_comparison.table.tabs.differences')"
                      :disabled="isTabSwitchDisabled"
                      @change="activeTab = $event"
                    />
                  </div>

                  <p v-if="!isCompact && differCount > 0" class="compare-table__differ">
                    {{ $t("product_comparison.table.differ_rows", { count: differCount, total: totalRows }) }}
                  </p>
                </div>

                <VcButton
                  v-if="!isCompact"
                  class="compare-table__clear-category"
                  variant="soft"
                  color="neutral"
                  size="xs"
                  prepend-icon="x"
                  @click="emit('clearCategory')"
                >
                  {{ $t("product_comparison.table.clear_category") }}
                </VcButton>
              </Teleport>
            </td>

            <th
              v-for="(column, index) in columns"
              :key="column.item.id"
              class="compare-table__product"
              :class="{ 'compare-table__product--compact': isCompact }"
              scope="col"
              :aria-label="column.product?.name ?? $t('common.messages.product_no_longer_available')"
            >
              <template v-if="!column.product">
                <div v-if="!isCompact" class="compare-table__product-image-wrap">
                  <VcImage class="compare-table__product-image" />

                  <VcProductActions class="compare-table__product-remove" with-background>
                    <VcProductActionsButton
                      icon="trash-2"
                      :tooltip-text="$t('product_comparison.table.remove_product')"
                      @click="onRemove(column)"
                    />
                  </VcProductActions>
                </div>

                <div class="compare-table__product-footer">
                  <p class="compare-table__unavailable">
                    {{ $t("common.messages.product_no_longer_available") }}
                  </p>

                  <VcButton
                    v-if="isCompact"
                    icon="trash-2"
                    size="sm"
                    variant="outline"
                    :aria-label="$t('product_comparison.table.remove_product')"
                    @click="onRemove(column)"
                  />
                </div>
              </template>

              <template v-else-if="isCompact">
                <div class="compare-table__product-summary">
                  <div class="compare-table__product-summary-image-wrap">
                    <VcImage
                      class="compare-table__product-summary-image"
                      :src="column.product.imgSrc"
                      :alt="column.product.name"
                    />
                  </div>

                  <VcProductTitle
                    class="compare-table__product-summary-title"
                    :to="getProductRoute(column.product.id, column.product.slug)"
                    :title="column.product.name"
                    :lines-number="1"
                    @click="emit('selectItem', column.product, index)"
                  >
                    {{ column.product.name }}
                  </VcProductTitle>
                </div>

                <VcButton
                  v-if="column.product.isConfigurable"
                  class="compare-table__product-cart-button"
                  prepend-icon="cube-transparent"
                  size="sm"
                  :to="getConfigurationLink(column.item, column.product)"
                  :target="browserTarget"
                  :aria-label="$t('pages.catalog.customize_button')"
                >
                  <span>{{ $t("pages.catalog.customize_button") }}</span>
                </VcButton>

                <VcButton
                  v-else-if="column.product.hasVariations"
                  class="compare-table__product-cart-button"
                  prepend-icon="layers"
                  size="sm"
                  :to="getProductRoute(column.product.id, column.product.slug)"
                  :target="browserTarget"
                  :aria-label="getVariationsLabel(column.product)"
                >
                  <span>{{ getVariationsLabel(column.product) }}</span>
                </VcButton>

                <VcButton
                  v-else
                  class="compare-table__product-cart-button"
                  prepend-icon="shopping-cart"
                  size="sm"
                  :loading="isAddingToCart(column.item.id)"
                  :disabled="isAddToCartDisabled(column.product) || isAddingToCart(column.item.id)"
                  :aria-label="$t('product_comparison.table.add_to_cart')"
                  @click="onAddToCart(column.item.id, column.product)"
                >
                  <span>{{ $t("product_comparison.table.add_to_cart") }}</span>
                </VcButton>
              </template>

              <template v-else>
                <div class="compare-table__product-image-wrap">
                  <VcImage
                    class="compare-table__product-image"
                    :src="column.product.imgSrc"
                    :alt="column.product.name"
                  />

                  <VcProductActions class="compare-table__product-remove" with-background>
                    <VcProductActionsButton
                      icon="trash-2"
                      :tooltip-text="$t('product_comparison.table.remove_product')"
                      @click="onRemove(column)"
                    />
                  </VcProductActions>
                </div>

                <div class="compare-table__product-footer">
                  <VcProductTitle
                    class="compare-table__product-title"
                    :to="getProductRoute(column.product.id, column.product.slug)"
                    :title="column.product.name"
                    :lines-number="2"
                    @click="emit('selectItem', column.product, index)"
                  >
                    {{ column.product.name }}
                  </VcProductTitle>

                  <VcButton
                    v-if="column.product.isConfigurable"
                    icon="cube-transparent"
                    size="sm"
                    :to="getConfigurationLink(column.item, column.product)"
                    :target="browserTarget"
                    :aria-label="$t('pages.catalog.customize_button')"
                  />

                  <VcButton
                    v-else-if="column.product.hasVariations"
                    icon="layers"
                    size="sm"
                    :to="getProductRoute(column.product.id, column.product.slug)"
                    :target="browserTarget"
                    :aria-label="getVariationsLabel(column.product)"
                  />

                  <VcButton
                    v-else
                    icon="shopping-cart"
                    size="sm"
                    :loading="isAddingToCart(column.item.id)"
                    :disabled="isAddToCartDisabled(column.product) || isAddingToCart(column.item.id)"
                    :aria-label="$t('product_comparison.table.add_to_cart')"
                    @click="onAddToCart(column.item.id, column.product)"
                  />
                </div>
              </template>
            </th>
          </tr>
        </thead>

        <tbody ref="bodyScrollRef" class="compare-table__scroll">
          <tr
            v-for="(row, rowIndex) in visibleRows"
            :key="row.key"
            class="compare-table__row"
            :class="{ 'compare-table__row--alt': rowIndex % 2 === 1 }"
          >
            <th
              class="compare-table__row-label"
              scope="row"
              :aria-label="row.label"
              :aria-describedby="row.description ? `${rowDescriptionIdPrefix}-${rowIndex}` : undefined"
            >
              <span class="compare-table__row-label-info-wrap">
                <span class="compare-table__row-label-text">{{ row.label }}</span>

                <VcTooltip
                  v-if="row.description"
                  class="compare-table__row-info"
                  placement="top"
                  strategy="fixed"
                  enable-teleport
                >
                  <template #trigger>
                    <VcIcon
                      :id="`${rowDescriptionIdPrefix}-${rowIndex}`"
                      name="information-circle"
                      size="xs"
                      :label="row.description"
                    />
                  </template>

                  <template #content>
                    {{ row.description }}
                  </template>
                </VcTooltip>
              </span>

              <VcButton
                class="compare-table__row-pin"
                :class="{ 'compare-table__row-pin--active': isRowPinned(row.key) }"
                size="xxs"
                variant="ghost"
                :color="isRowPinned(row.key) ? 'secondary' : 'neutral'"
                icon
                :aria-pressed="isRowPinned(row.key)"
                :aria-label="
                  $t(
                    isRowPinned(row.key)
                      ? 'product_comparison.table.pin.unpin_label'
                      : 'product_comparison.table.pin.pin_label',
                    { label: row.label },
                  )
                "
                :title="
                  $t(
                    isRowPinned(row.key)
                      ? 'product_comparison.table.pin.unpin_title'
                      : 'product_comparison.table.pin.pin_title',
                  )
                "
                @click="togglePin(row.key)"
              >
                <VcIcon name="pin" :variant="isRowPinned(row.key) ? 'solid' : 'outline'" />
              </VcButton>
            </th>

            <td v-for="(column, index) in columns" :key="column.item.id" class="compare-table__row-value">
              <template v-if="!column.product">{{ row.values[index] }}</template>

              <VcProductPrice
                v-else-if="row.kind === 'price'"
                class="compare-table__price"
                align="start"
                single-line
                :actual-price="getDisplayPrice(column.product).actual"
                :list-price="getDisplayPrice(column.product).list"
                :with-from-label="
                  column.product.hasVariations ||
                  (column.product.isConfigurable && !column.item.configurationSections.length)
                "
              />

              <span v-else-if="row.kind === 'rating' && column.product.rating" class="compare-table__rating">
                <VcRating mode="full" read-only :value="column.product.rating.value" size="xs" :with-text="false" />
                {{ row.values[index] }}
              </span>

              <InStock
                v-else-if="row.kind === 'availability'"
                :is-in-stock="column.product.availabilityData.isInStock"
                :is-available="column.product.availabilityData.isAvailable"
                :is-digital="column.product.productType === DIGITAL_PRODUCT_TYPE"
                :quantity="column.product.availabilityData.availableQuantity"
              />

              <span
                v-else-if="row.kind === 'boolean' && row.boolValues?.[index] !== undefined"
                class="compare-table__boolean"
              >
                <VcIcon
                  size="sm"
                  :name="row.boolValues[index] ? 'check' : 'x'"
                  :class="row.boolValues[index] ? 'text-success' : 'text-neutral-400'"
                />

                {{ row.values[index] }}
              </span>

              <template v-else>{{ row.values[index] }}</template>
            </td>
          </tr>
        </tbody>
      </table>
    </template>
  </VcWidget>
</template>

<script setup lang="ts">
import { useBreakpoints, useCssVar, useElementBounding } from "@vueuse/core";
import { BREAKPOINTS, getProductRoute, InStock, useThemeContext } from "@vc-frontend/core";
import { computed, nextTick, ref, useId, watch } from "vue";
import { useI18n } from "vue-i18n";
import { useCompareAddToCart } from "../composables/useCompareAddToCart";
import { useCompareTableRowPins } from "../composables/useCompareTableRowPins";
import { useHorizontalScrollSync } from "../composables/useHorizontalScrollSync";
import { AVAILABILITY_ROW_KEY, DIGITAL_PRODUCT_TYPE, PRICE_ROW_KEY } from "../constants";
import { getConfigurationLink, getDisplayPrice, getVariationsCount } from "../utils";
import type { CompareProductFragment } from "../api/graphql/types";
import type { ICompareColumn, ICompareTableRow } from "../types";

interface IEmits {
  (e: "clearCategory"): void;
  (e: "removeProduct", column: ICompareColumn): void;
  (e: "selectItem", product: CompareProductFragment, index: number): void;
}

interface IProps {
  columns: ICompareColumn[];
  rows?: ICompareTableRow[];
}

const emit = defineEmits<IEmits>();

const props = withDefaults(defineProps<IProps>(), {
  rows: () => [],
});

// Shown under the "Differences" filter whether they differ or not.
const PERMANENT_ROW_KEYS = [PRICE_ROW_KEY, AVAILABILITY_ROW_KEY];

const { t } = useI18n();
const { themeContext } = useThemeContext();
const { isAddingToCart, isAddToCartDisabled, onAddToCart } = useCompareAddToCart();

const browserTarget = computed(() => themeContext.value.settings.details_browser_target ?? "_blank");
const isMobile = useBreakpoints(BREAKPOINTS).smaller("md");

const { isRowPinned, togglePin, pinnedRows, unpinnedRows } = useCompareTableRowPins(() => props.rows);

const activeTab = ref("all");
const bodyScrollRef = ref<HTMLElement | null>(null);
const mobileTabsBarRef = ref<HTMLElement | null>(null);
const headerRowRef = ref<HTMLElement | null>(null);
const rowDescriptionIdPrefix = useId();

// The app header's current height, kept live by the host's header, so the table's header row sticks right below it.
const appHeaderHeightVar = useCssVar("--vc-app-header-height", undefined, { observe: true });
const appHeaderHeight = computed(() => Number.parseFloat(appHeaderHeightVar.value ?? "0") || 0);

// The row is stuck once its top meets the app header; then it compacts. Below md it sticks in its full layout.
const { top: headerRowTop, update: updateHeaderRowBounding } = useElementBounding(headerRowRef);
const isCompact = computed(() => !isMobile.value && headerRowTop.value <= appHeaderHeight.value + 1);

watch(appHeaderHeight, () => updateHeaderRowBounding());

// Compacting swaps whole subtrees, which destroys whatever held focus; keep keyboard users on the header row.
watch(isCompact, async () => {
  const previouslyFocused = document.activeElement;

  if (!headerRowRef.value?.contains(previouslyFocused)) {
    return;
  }

  await nextTick();

  if (previouslyFocused instanceof HTMLElement && previouslyFocused.isConnected) {
    return;
  }

  headerRowRef.value?.focus();
});

async function onRemove(column: ICompareColumn) {
  emit("removeProduct", column);

  await nextTick();
  headerRowRef.value?.focus();
}

function getVariationsLabel(product: CompareProductFragment): string {
  return t("pages.catalog.variations_button", getVariationsCount(product));
}

const differCount = computed(() => props.rows.filter((row) => row.differs).length);
const totalRows = computed(() => props.rows.length);

const isTabSwitchDisabled = computed(() => props.columns.filter((column) => column.product).length <= 1);

const visibleRows = computed(() => {
  const rest =
    activeTab.value === "differences"
      ? unpinnedRows.value.filter((row) => row.differs || PERMANENT_ROW_KEYS.includes(row.key))
      : unpinnedRows.value;

  return [...pinnedRows.value, ...rest];
});

useHorizontalScrollSync(bodyScrollRef, headerRowRef);

watch(
  isTabSwitchDisabled,
  (disabled) => {
    if (disabled) {
      activeTab.value = "all";
    }
  },
  { immediate: true },
);
</script>

<style lang="scss">
.compare-table {
  &__table {
    @apply block;
  }

  &__scroll {
    @apply block overflow-x-auto rounded-b-[--vc-radius];
  }

  &__header-row {
    // scroll-ps matches __controls, so focus scrolls a fully covered product control clear of the pinned corner cell.
    @apply sticky z-10 block scroll-ps-60 overflow-x-auto rounded-t-[--vc-radius] border-b border-neutral-200 bg-additional-50;

    -ms-overflow-style: none;
    scrollbar-width: none;
    top: var(--vc-app-header-height, 0px);

    @media (width < theme("screens.md")) {
      @apply scroll-ps-28 rounded-t-none;
    }

    &::-webkit-scrollbar {
      display: none;
    }

    &--stuck {
      @apply shadow-md;
    }
  }

  &__mobile-tabs-bar {
    @apply hidden;

    @media (width < theme("screens.md")) {
      @apply flex rounded-t-[--vc-radius] border-b border-neutral-200 bg-additional-50 px-3 py-2.5;
    }
  }

  &__header-inner {
    @apply flex w-fit min-w-full items-stretch;
  }

  &__controls {
    @apply sticky start-0 z-[1] flex w-60 shrink-0 flex-col justify-between gap-3 border-e border-neutral-200 bg-additional-50 py-3 pe-4 ps-3;

    @media (width < theme("screens.md")) {
      @apply w-28;
    }

    &--stuck {
      @apply justify-center;
    }
  }

  &__controls-top {
    @apply flex flex-col gap-3;

    @media (width < theme("screens.md")) {
      @apply w-full flex-row items-center justify-between gap-3;
    }
  }

  &__clear-category {
    @apply self-start whitespace-nowrap;

    @media (width < theme("screens.md")) {
      @apply self-center;
    }
  }

  &__tabs {
    @apply grid grid-cols-2 gap-0.5 rounded-[--vc-radius] bg-neutral-100 p-1.5;
  }

  &__tab {
    @apply w-full;

    --vc-tab-switch-border-color: transparent;
  }

  &__differ {
    @apply text-xs text-neutral-500;

    @media (width < theme("screens.md")) {
      @apply hidden;
    }
  }

  &__product {
    @apply flex min-w-48 max-w-60 flex-1 flex-col gap-3 p-3 text-start font-normal;

    // Keeps content out of the w-fit row's width, so header columns always match the body's.
    contain: inline-size;

    @media (width < theme("screens.md")) {
      @apply w-28 min-w-0 max-w-none flex-none;
    }

    &:not(:last-child) {
      @apply border-e border-neutral-200;
    }

    &--compact {
      @apply justify-between gap-2 py-2.5;
    }
  }

  &__product-image-wrap {
    @apply relative h-44 overflow-hidden rounded-[--vc-radius] border border-neutral-300;

    @media (width < theme("screens.md")) {
      @apply h-24;
    }
  }

  &__product-image {
    --vc-image-object-fit: cover;
    --vc-image-fallback-object-fit: scale-down;

    @apply size-full;
  }

  &__product-remove {
    @apply absolute end-1 top-1;
  }

  &__product-footer {
    @apply flex items-end gap-2;

    @media (width < theme("screens.md")) {
      @apply flex-col items-start;
    }
  }

  &__product-title {
    --vc-product-title-font-size: theme("fontSize.sm");

    @apply min-w-0 flex-1;
  }

  &__unavailable {
    @apply min-w-0 flex-1 text-sm font-bold text-neutral-500;
  }

  &__product-summary {
    @apply flex items-center gap-2.5;
  }

  &__product-summary-image-wrap {
    @apply size-10 shrink-0 overflow-hidden rounded-[--vc-radius] border border-neutral-300;
  }

  &__product-summary-image {
    --vc-image-object-fit: cover;
    --vc-image-fallback-object-fit: scale-down;

    @apply size-full;
  }

  &__product-summary-title {
    --vc-product-title-font-size: theme("fontSize.sm");

    @apply min-w-0 flex-1;
  }

  &__product-cart-button {
    @apply w-full;
  }

  &__row {
    @apply flex min-h-[50px] w-fit min-w-full items-stretch;

    &:not(:last-child) {
      @apply border-b border-neutral-200;
    }

    &:hover {
      @apply bg-secondary-100;

      .compare-table__row-pin {
        @apply opacity-100;
      }

      .compare-table__row-label {
        @apply bg-secondary-100;
      }
    }
  }

  &__row--alt {
    @apply bg-neutral-50;

    .compare-table__row-label {
      @apply bg-neutral-50;
    }
  }

  &__row-label {
    @apply sticky start-0 z-[1] flex w-60 shrink-0 items-center gap-1 border-e border-neutral-200 bg-additional-50 px-3 py-2.5 text-start text-xs font-normal text-neutral-600;

    @media (width < theme("screens.md")) {
      @apply w-28;
    }
  }

  &__row-label-info-wrap {
    @apply flex min-w-0 flex-1 items-center gap-1.5;
  }

  &__row-label-text {
    @apply min-w-0 truncate;
  }

  &__row-info {
    @apply shrink-0 text-neutral-500;

    @media (width < theme("screens.md")) {
      @apply hidden;
    }
  }

  &__row-pin {
    @apply shrink-0 opacity-0 transition-opacity;

    @media (width < theme("screens.md")) {
      @apply hidden;
    }

    @media (hover: none) {
      @apply opacity-100;
    }

    &:focus-visible {
      @apply opacity-100;
    }

    &--active {
      @apply opacity-100;
    }
  }

  &__row-value {
    @apply flex min-w-48 max-w-60 flex-1 items-center px-3 py-2.5 text-sm text-neutral-900;

    contain: inline-size;
    overflow-wrap: anywhere;

    @media (width < theme("screens.md")) {
      @apply w-28 min-w-0 max-w-none flex-none;
    }

    &:not(:last-child) {
      @apply border-e border-neutral-200;
    }
  }

  &__price {
    --vc-product-price-font-size: theme("fontSize.sm");
  }

  &__rating {
    @apply flex items-center gap-2 text-sm font-bold;
  }

  &__boolean {
    @apply flex items-center gap-2;
  }
}
</style>
