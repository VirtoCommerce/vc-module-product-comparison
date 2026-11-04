import { definePluginManifest, settingEnabled } from "@vc-frontend/core/manifest";
import {
  COMPARE_ACTION_NAME,
  COMPARE_ROUTE_NAME,
  COMPARE_ROUTE_PATH,
  ENABLED_KEY,
  MENU_LINK_ID,
  MODULE_ID,
  PURCHASING_MENU_LINK_ID,
} from "./src/constants";

// What the storefront knows before running this plugin. Reference:
// https://github.com/VirtoCommerce/vc-frontend/blob/dev/client-app/core/federation/declarations.md
//
// The links' title is the host's own "Compare", so it shows before this plugin's locales load.
export default definePluginManifest({
  when: settingEnabled(MODULE_ID, ENABLED_KEY),
  routes: [{ path: COMPARE_ROUTE_PATH, name: COMPARE_ROUTE_NAME }],
  menu: [
    {
      surface: "header",
      group: "main",
      id: MENU_LINK_ID,
      title: "common.links.compare",
      icon: "compare",
      priority: 10,
      routeName: COMPARE_ROUTE_NAME,
    },
    {
      surface: "header",
      group: "purchasing",
      id: PURCHASING_MENU_LINK_ID,
      title: "common.links.compare",
      icon: "compare",
      priority: 35,
      routeName: COMPARE_ROUTE_NAME,
    },
  ],
  slots: [{ at: `productActions/${COMPARE_ACTION_NAME}`, policy: "reserve" }],
});
