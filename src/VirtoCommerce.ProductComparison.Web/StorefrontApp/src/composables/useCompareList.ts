import { useLocalStorage } from "@vueuse/core";
import {
  apolloClient,
  globals,
  Logger,
  ROUTES,
  SUPPRESS_ERROR_NOTIFICATIONS_CONTEXT,
  useModuleSettings,
  useNotifications,
  useUser,
} from "@vc-frontend/core";
import { computed, shallowRef } from "vue";
import {
  ClearCompareListCategoryDocument,
  ClearCompareListDocument,
  CompareListDocument,
  MergeCompareListDocument,
  RemoveCompareListItemsDocument,
  SaveProductsToCompareListDocument,
} from "../api/graphql/types";
import {
  ANONYMOUS_LIST_ENABLED_KEY,
  COMPARE_ROUTE_PATH,
  CATEGORY_PARAM,
  CompareListErrorCode,
  GUEST_LIST_ID_STORAGE_KEY,
  MAX_ITEMS_KEY,
  MODULE_ID,
  NOTIFICATION_PRODUCT_NAME_MAX_LENGTH,
  NOTIFICATIONS_GROUP,
} from "../constants";
import {
  getCategoryItems,
  getItemConfigurationKey,
  getProductCategoryId,
  getSelectionConfigurationKey,
  toConfigurationSectionInputs,
  truncate,
} from "../utils";
import type { CompareListFragment, InputCompareListProductType } from "../api/graphql/types";
import type { CompareListItemType } from "../types";
import type { ApolloError } from "@apollo/client";
import type { ConfigurationSectionInput } from "@vc-frontend/core";

type ProductType = { id: string; name: string; breadcrumbs?: { itemId: string; typeName?: string }[] };

const NOTIFICATION_DURATION = 15000;

const list = shallowRef<CompareListFragment>();
const lastRemovedItems = shallowRef<CompareListItemType[]>([]);
const guestListId = useLocalStorage<string | null>(GUEST_LIST_ID_STORAGE_KEY, null);

let loaded: Promise<void> = Promise.resolve();
let queue: Promise<unknown> = Promise.resolve();

// One request at a time: the server saves a list whole, so two saves at once can lose one's change.
function enqueue<T>(task: () => Promise<T>): Promise<T> {
  const result = queue.then(task);
  queue = result.catch(() => undefined);
  return result;
}

function t(key: string, values: Record<string, unknown> = {}): string {
  return globals.i18n.global.t(key, values);
}

function getUserVariables() {
  return { storeId: globals.storeId, userId: globals.userId };
}

function countInCategory(categoryId?: string): number {
  return getCategoryItems(list.value?.items ?? [], categoryId).length;
}

function getMaxItems(): number {
  return Number(useModuleSettings(MODULE_ID).getSettingValue(MAX_ITEMS_KEY));
}

function setList(value: CompareListFragment | undefined): void {
  list.value = value;

  if (value && !useUser().isAuthenticated.value) {
    guestListId.value = value.id;
  }
}

function getErrorCode(error: unknown): string | undefined {
  const { graphQLErrors = [] } = error as Partial<ApolloError>;
  return graphQLErrors.find((graphQLError) => graphQLError.extensions?.code)?.extensions?.code as string | undefined;
}

type NotificationType = { text?: string; html?: string; button?: { text: string; to: string | object } };

// One toast at a time for compare; its button also closes it.
function notify(kind: "success" | "warning", { button, ...content }: NotificationType): void {
  const notifications = useNotifications();

  notifications[kind]({
    duration: NOTIFICATION_DURATION,
    group: NOTIFICATIONS_GROUP,
    singleInGroup: true,
    ...content,
    button: button && {
      ...button,
      clickHandler() {
        notifications.clear(NOTIFICATIONS_GROUP);
      },
    },
  });
}

function warn(text: string, button?: NotificationType["button"]): void {
  notify("warning", { text, button });
}

function warnSignInRequired(): void {
  warn(t("product_comparison.notifications.sign_in_required"), {
    text: t("product_comparison.notifications.sign_in_button"),
    to: { name: ROUTES.SIGN_IN.NAME, query: { returnUrl: globals.router.currentRoute.value.fullPath } },
  });
}

function warnLimitReached(): void {
  warn(t("product_comparison.notifications.limit_reached", { productsLimit: getMaxItems() }));
}

function handleSaveError(error: unknown): void {
  switch (getErrorCode(error)) {
    case CompareListErrorCode.LimitReached:
      warnLimitReached();
      break;
    case CompareListErrorCode.AnonymousListDisabled:
      warnSignInRequired();
      break;
    case CompareListErrorCode.Full:
      warn(t("product_comparison.notifications.list_full"));
      break;
    default:
      Logger.error("[product-comparison] save", error);
      useNotifications().error({ text: t("common.messages.something_went_wrong"), duration: NOTIFICATION_DURATION });
  }
}

async function saveProducts(products: InputCompareListProductType[]): Promise<boolean> {
  try {
    await enqueue(async () => {
      const { data } = await apolloClient.mutate({
        mutation: SaveProductsToCompareListDocument,
        variables: { command: { ...getUserVariables(), products } },
        fetchPolicy: "no-cache",
        context: SUPPRESS_ERROR_NOTIFICATIONS_CONTEXT,
      });
      setList(data?.saveProductsToCompareList);
    });
    return true;
  } catch (error) {
    handleSaveError(error);
    return false;
  }
}

// Refusals other than a save's come back from the host's own error toast; the list stays as it was.
async function change(request: () => Promise<CompareListFragment | undefined>): Promise<boolean> {
  try {
    setList(await enqueue(request));
    return true;
  } catch (error) {
    Logger.error("[product-comparison] change", error);
    return false;
  }
}

function findItem(productId: string, configurationSections?: readonly ConfigurationSectionInput[]) {
  const key = getSelectionConfigurationKey(configurationSections);
  return list.value?.items.find((item) => item.productId === productId && getItemConfigurationKey(item) === key);
}

async function add(product: ProductType, configurationSections?: ConfigurationSectionInput[]): Promise<void> {
  if (!useUser().isAuthenticated.value && !useModuleSettings(MODULE_ID).isEnabled(ANONYMOUS_LIST_ENABLED_KEY)) {
    warnSignInRequired();
    return;
  }

  const categoryId = getProductCategoryId(product);
  if (countInCategory(categoryId) >= getMaxItems()) {
    warnLimitReached();
    return;
  }

  if (!(await saveProducts([{ productId: product.id, categoryId, configurationSections }]))) {
    return;
  }

  notify("success", {
    html: t("product_comparison.notifications.added_html", {
      productName: truncate(product.name, NOTIFICATION_PRODUCT_NAME_MAX_LENGTH),
      itemsLeft: getMaxItems() - countInCategory(categoryId),
    }),
    button: {
      text: t("product_comparison.notifications.compare_button"),
      to: { path: COMPARE_ROUTE_PATH, query: categoryId ? { [CATEGORY_PARAM]: categoryId } : {} },
    },
  });
}

async function remove(item: CompareListItemType, productName?: string): Promise<void> {
  const removed = await change(async () => {
    const { data } = await apolloClient.mutate({
      mutation: RemoveCompareListItemsDocument,
      variables: { command: { ...getUserVariables(), itemIds: [item.id] } },
      fetchPolicy: "no-cache",
    });
    return data?.removeCompareListItems;
  });

  if (removed && productName) {
    notify("warning", {
      html: t("product_comparison.notifications.removed_html", {
        productName: truncate(productName, NOTIFICATION_PRODUCT_NAME_MAX_LENGTH),
      }),
    });
  }
}

async function clear(): Promise<void> {
  const items = list.value?.items ?? [];

  const cleared = await change(async () => {
    const { data } = await apolloClient.mutate({
      mutation: ClearCompareListDocument,
      variables: { command: getUserVariables() },
      fetchPolicy: "no-cache",
    });
    return data?.clearCompareList;
  });

  if (cleared) {
    lastRemovedItems.value = items;
  }
}

/** `categoryId` as the list returns it: none for the uncategorized group. */
async function clearCategory(categoryId?: string | null): Promise<void> {
  const items = list.value?.items ?? [];
  const categoryItems = getCategoryItems(items, categoryId);

  const cleared = await change(async () => {
    const { data } = await apolloClient.mutate({
      mutation: ClearCompareListCategoryDocument,
      variables: { command: { ...getUserVariables(), categoryId: categoryId ?? undefined } },
      fetchPolicy: "no-cache",
    });
    return data?.clearCompareListCategory;
  });

  // "Restore" brings back what emptied the list, as the page offers it only on the empty state.
  if (cleared && categoryItems.length === items.length) {
    lastRemovedItems.value = categoryItems;
  }
}

async function restore(): Promise<void> {
  const items = lastRemovedItems.value;
  lastRemovedItems.value = [];

  await saveProducts(
    items.map((item) => ({
      productId: item.productId,
      categoryId: item.categoryId,
      configurationSections: toConfigurationSectionInputs(item),
    })),
  );
}

function clearRestoreBuffer(): void {
  lastRemovedItems.value = [];
}

async function mergeGuestList(secondListId: string): Promise<boolean> {
  try {
    const { data } = await apolloClient.mutate({
      mutation: MergeCompareListDocument,
      variables: { command: { ...getUserVariables(), secondListId } },
      fetchPolicy: "no-cache",
      context: SUPPRESS_ERROR_NOTIFICATIONS_CONTEXT,
    });
    setList(data?.mergeCompareList);
    return true;
  } catch (error) {
    Logger.error("[product-comparison] merge", error);
    return false;
  }
}

/** Loads the list; after a sign-in, merges the guest's list into the account's first. */
export function loadCompareList(): Promise<void> {
  loaded = enqueue(async () => {
    const secondListId = guestListId.value;

    // Forgotten before the call, so a merge the server refuses is not retried on every page load.
    if (useUser().isAuthenticated.value && secondListId) {
      guestListId.value = null;

      if (await mergeGuestList(secondListId)) {
        return;
      }
    }

    const { data } = await apolloClient.query({
      query: CompareListDocument,
      variables: getUserVariables(),
      fetchPolicy: "no-cache",
      context: SUPPRESS_ERROR_NOTIFICATIONS_CONTEXT,
    });
    setList(data.compareList);
  }).catch((error) => Logger.error("[product-comparison] load", error));

  return loaded;
}

/** The configuration a "Customize" link opens the product page with: the item's, if it is the viewer's own. */
export async function getItemConfiguration(itemId: string, productId: string) {
  await loaded;

  const item = list.value?.items.find((x) => x.id === itemId && x.productId === productId);
  return item?.configurationSections.length ? toConfigurationSectionInputs(item) : undefined;
}

/** The count the header and mobile menu links show. */
export function useCompareCount() {
  return { count: computed(() => list.value?.itemsCount ?? 0) };
}

export function useCompareList() {
  return {
    maxItems: computed(getMaxItems),
    canRestore: computed(() => lastRemovedItems.value.length > 0),
    findItem,
    add,
    remove,
    clear,
    clearCategory,
    restore,
    clearRestoreBuffer,
    reload: loadCompareList,
  };
}
