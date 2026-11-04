<template>
  <VcProductActionsButton
    icon="compare"
    :active="isInList"
    :aria-label="tooltipText"
    :aria-pressed="isInList"
    :tooltip-text="tooltipText"
    @click="toggle"
  />
</template>

<script setup lang="ts">
import { computed, ref } from "vue";
import { useI18n } from "vue-i18n";
import { useCompareList } from "../composables/useCompareList";
import type { ProductActionPropsType } from "@vc-frontend/core";

interface IProps {
  product: ProductActionPropsType["product"];
  configurationSections?: ProductActionPropsType["configurationSections"];
}

const props = defineProps<IProps>();

const { t } = useI18n();
const { findItem, add, remove } = useCompareList();

const item = computed(() => findItem(props.product.id, props.configurationSections));
const isInList = computed(() => !!item.value);
const busy = ref(false);

const tooltipText = computed(() =>
  isInList.value
    ? t("product_comparison.add_to_compare.tooltips.remove")
    : t("product_comparison.add_to_compare.tooltips.add"),
);

async function toggle() {
  if (busy.value) {
    return;
  }

  busy.value = true;
  try {
    if (item.value) {
      await remove(item.value, props.product.name);
    } else {
      await add(props.product, props.configurationSections);
    }
  } finally {
    busy.value = false;
  }
}
</script>
