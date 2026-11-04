<template>
  <VcContainer class="compare-products">
    <VcBreadcrumbs class="compare-products__breadcrumbs" :items="breadcrumbs" />

    <div class="compare-products__head">
      <div class="compare-products__title-block">
        <VcTypography tag="h1" class="compare-products__title">
          {{ $t("product_comparison.page.title") }}
        </VcTypography>

        <i18n-t
          :keypath="
            selectedCategoryLabel
              ? 'product_comparison.page.added_count_in_category'
              : 'product_comparison.page.added_count'
          "
          scope="global"
          tag="p"
          class="compare-products__added-count"
        >
          <template #count>
            <span class="compare-products__added-count-value">{{ selectedCategoryCount }}</span>
          </template>

          <template #total>{{ maxItems }}</template>

          <template #category>{{ selectedCategoryLabel }}</template>
        </i18n-t>
      </div>

      <div class="compare-products__actions">
        <VcButton
          class="compare-products__clear-all"
          variant="outline"
          size="sm"
          :disabled="!totalCount"
          prepend-icon="trash-2"
          @click="openClearAllModal"
        >
          {{ $t("product_comparison.page.actions.clear_all") }}
        </VcButton>

        <VcButton
          class="compare-products__clear-all-icon"
          variant="outline"
          size="sm"
          :disabled="!totalCount"
          icon="trash-2"
          :aria-label="$t('product_comparison.page.actions.clear_all')"
          @click="openClearAllModal"
        />
      </div>
    </div>

    <VcWidget v-if="isEmpty" class="compare-products__empty-state">
      <VcEmptyView>
        <template #icon>
          <div class="compare-products__empty-icon">
            <VcIcon name="compare" />
          </div>
        </template>

        <VcTypography
          ref="emptyStateHeadingRef"
          tag="h3"
          data-test-id="compare-empty-heading"
          class="compare-products__empty-heading"
          tabindex="-1"
        >
          {{ $t("product_comparison.page.empty.title") }}
        </VcTypography>

        <p class="compare-products__empty-description">
          {{ $t("product_comparison.page.empty.description", { limit: maxItems }) }}
        </p>

        <template #button>
          <div class="compare-products__empty-actions">
            <VcButton v-if="canRestore" size="sm" prepend-icon="refresh-cw" @click="restoreProducts">
              {{ $t("product_comparison.page.empty.restore_button") }}
            </VcButton>

            <VcButton
              variant="outline"
              size="sm"
              prepend-icon="plus"
              :external-link="continueShoppingLink"
              :to="continueShoppingLink ? undefined : '/'"
            >
              {{ $t("product_comparison.page.empty.add_button") }}
            </VcButton>
          </div>
        </template>
      </VcEmptyView>
    </VcWidget>

    <VcWidget
      v-else-if="failed && !columns.length"
      data-test-id="compare-error-state"
      class="compare-products__error-state"
    >
      <VcEmptyView variant="error" :text="$t('product_comparison.page.error')">
        <template #button>
          <VcButton size="sm" @click="refetch()">
            {{ $t("product_comparison.page.retry") }}
          </VcButton>
        </template>
      </VcEmptyView>
    </VcWidget>

    <div v-else-if="!columns.length" data-test-id="compare-loading-state" class="compare-products__loading-state">
      <VcLoaderOverlay visible no-bg />
    </div>

    <template v-else>
      <div class="compare-products__category-tabs" @wheel="onCategoryTabsWheel">
        <VcButton
          v-for="tab in categoryTabs"
          :key="tab.key"
          class="compare-products__category-tab"
          size="xs"
          color="secondary"
          :variant="tab.key === selectedCategoryKey ? 'solid' : 'outline'"
          :aria-pressed="tab.key === selectedCategoryKey"
          @click="selectCategory(tab.key)"
        >
          <span class="compare-products__category-tab-content">
            {{ tab.label }}

            <VcBadge
              size="sm"
              color="secondary"
              rounded
              :variant="tab.key === selectedCategoryKey ? 'surface' : 'outline'"
            >
              {{ tab.count }}
            </VcBadge>
          </span>
        </VcButton>
      </div>

      <div class="compare-products__table-wrap">
        <VcLoaderOverlay :visible="loading" />

        <CompareTable
          :columns="columns"
          :rows="tableRows"
          :differ-count="differRowsCount"
          :total-rows="tableRows.length"
          @clear-category="openClearCategoryModal"
          @remove-product="removeColumn"
          @select-item="selectItemEvent"
        />
      </div>
    </template>
  </VcContainer>
</template>

<script setup lang="ts">
import { useDocumentVisibility } from "@vueuse/core";
import { useBreadcrumbs, useModal, useModuleSettings, usePageHead } from "@vc-frontend/core";
import { nextTick, onUnmounted, useTemplateRef, watch } from "vue";
import { useI18n } from "vue-i18n";
import CompareTable from "../components/compare-table.vue";
import { useCompareList } from "../composables/useCompareList";
import { useComparePage } from "../composables/useComparePage";
import { CONTINUE_SHOPPING_LINK_KEY, XAPI_MODULE_ID } from "../constants";
import type { ICompareColumn } from "../types";

const { t } = useI18n();
const { openModal, closeModal } = useModal();

const breadcrumbs = useBreadcrumbs([{ title: t("product_comparison.page.title") }]);

const continueShoppingLinkSetting = useModuleSettings(XAPI_MODULE_ID).getSettingValue(CONTINUE_SHOPPING_LINK_KEY);
const continueShoppingLink = typeof continueShoppingLinkSetting === "string" ? continueShoppingLinkSetting : undefined;

const { maxItems, canRestore, remove, clear, clearCategory, restore, clearRestoreBuffer, reload } = useCompareList();

const {
  loading,
  failed,
  isEmpty,
  categoryTabs,
  selectedCategory,
  selectedCategoryKey,
  selectedCategoryLabel,
  selectedCategoryCount,
  columns,
  tableRows,
  differRowsCount,
  totalCount,
  refetch,
  selectCategory,
  selectItemEvent,
} = useComparePage();

// Back on the page's tab: another tab or device may have changed the list meanwhile.
watch(useDocumentVisibility(), (visibility) => {
  if (visibility === "visible") {
    void reload();
    void refetch();
  }
});

function onCategoryTabsWheel(event: WheelEvent) {
  if (Math.abs(event.deltaY) <= Math.abs(event.deltaX)) {
    return;
  }

  event.preventDefault();
  (event.currentTarget as HTMLElement).scrollLeft += event.deltaY;
}

// Focus lands on the <h3> itself, so a screen reader keeps "heading level 3".
const emptyStateHeadingRef = useTemplateRef<{ $el: HTMLElement }>("emptyStateHeadingRef");

watch(isEmpty, async (empty) => {
  if (!empty) {
    return;
  }

  await nextTick();
  emptyStateHeadingRef.value?.$el?.focus();
});

async function removeColumn({ item, product }: ICompareColumn) {
  await remove(item, product?.name);
  await refetch();
}

async function restoreProducts() {
  await restore();
  await refetch();
}

function openClearAllModal() {
  openModal({
    component: "VcConfirmationModal",
    props: {
      variant: "danger",
      title: t("product_comparison.page.clear_all_modal.title"),
      text: t("product_comparison.page.clear_all_modal.message", { n: totalCount.value }, totalCount.value),
      async onConfirm() {
        closeModal();
        await clear();
        await refetch();
      },
    },
  });
}

function openClearCategoryModal() {
  const categoryId = selectedCategory.value?.id;
  const count = selectedCategoryCount.value;

  openModal({
    component: "VcConfirmationModal",
    props: {
      variant: "danger",
      title: t("product_comparison.page.clear_category_modal.title"),
      text: t(
        "product_comparison.page.clear_category_modal.message",
        { n: count, category: selectedCategoryLabel.value },
        count,
      ),
      async onConfirm() {
        closeModal();
        await clearCategory(categoryId);
        await refetch();
      },
    },
  });
}

usePageHead({
  title: t("product_comparison.page.title"),
  meta: {
    keywords: t("product_comparison.page.meta.keywords"),
    description: t("product_comparison.page.meta.description"),
  },
});

// "Restore" undoes a clear only while the buyer stays on the page that showed them the empty list.
onUnmounted(() => {
  clearRestoreBuffer();
});
</script>

<style lang="scss">
.compare-products {
  overflow-anchor: none;

  &__breadcrumbs {
    @apply mb-3;
  }

  &__head {
    @apply flex items-start justify-between gap-4;
  }

  &__added-count {
    @apply mt-1.5 text-sm text-neutral-500;
  }

  &__added-count-value {
    @apply font-bold text-neutral-950;
  }

  &__actions {
    @apply flex gap-2;

    @media print {
      @apply hidden;
    }
  }

  &__clear-all {
    @media (width < theme("screens.md")) {
      @apply hidden;
    }
  }

  &__clear-all-icon {
    @media (min-width: theme("screens.md")) {
      @apply hidden;
    }
  }

  &__empty-state,
  &__error-state {
    @apply mt-6;
  }

  &__empty-icon {
    @apply flex size-14 items-center justify-center rounded-[--vc-radius] bg-neutral-100 text-neutral-400;
  }

  &__empty-description {
    @apply max-w-md text-sm text-neutral-500;
  }

  &__empty-actions {
    @apply flex flex-wrap items-center justify-center gap-3;
  }

  &__loading-state {
    @apply relative mt-6 min-h-[25rem];
  }

  &__category-tabs {
    @apply mt-5 flex flex-nowrap items-center gap-2.5 overflow-x-auto py-1;

    -ms-overflow-style: none; /* for Edge */
    scrollbar-width: none; /* for Firefox */

    &::-webkit-scrollbar {
      display: none; /* for Chrome, Safari, and Opera */
    }
  }

  &__category-tab {
    @apply shrink-0;
  }

  &__category-tab-content {
    @apply flex items-center gap-2;
  }

  &__table-wrap {
    @apply relative mt-4;
  }
}
</style>
