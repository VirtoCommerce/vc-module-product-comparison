import "./styles.css";
import { globals, registerProductConfigurationSource, useExtensionRegistry } from "@vc-frontend/core";
import { defineAsyncComponent } from "vue";
import { getItemConfiguration, loadCompareList, useCompareCount } from "./composables/useCompareList";
import {
  COMPARE_ACTION_NAME,
  COMPARE_ITEM_ID_PARAM,
  COMPARE_ROUTE_NAME,
  COMPARE_ROUTE_PATH,
  MENU_LINK_ID,
} from "./constants";
import { loadLocales } from "./i18n";

const ComparePage = () => import("./pages/compare-products.vue");

export function init(): void {
  globals.router.addRoute({ path: COMPARE_ROUTE_PATH, name: COMPARE_ROUTE_NAME, component: ComparePage });

  const { register, registerContribution } = useExtensionRegistry();
  register("productActions", COMPARE_ACTION_NAME, {
    component: defineAsyncComponent(() => import("./components/add-to-compare-button.vue")),
  });
  registerContribution("headerMenu", MENU_LINK_ID, { use: useCompareCount });
  registerContribution("mobileMenu", MENU_LINK_ID, { use: useCompareCount });
  registerProductConfigurationSource(COMPARE_ITEM_ID_PARAM, getItemConfiguration);

  loadLocales();
  void loadCompareList();
}
