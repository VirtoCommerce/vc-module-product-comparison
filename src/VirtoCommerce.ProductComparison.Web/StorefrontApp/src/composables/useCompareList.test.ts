import { beforeEach, describe, expect, it, vi } from "vitest";
import { computed } from "vue";
import {
  ClearCompareListCategoryDocument,
  ClearCompareListDocument,
  CompareListDocument,
  MergeCompareListDocument,
  RemoveCompareListItemsDocument,
  SaveProductsToCompareListDocument,
} from "../api/graphql/types";
import { GUEST_LIST_ID_STORAGE_KEY } from "../constants";
import type { CompareListFragment } from "../api/graphql/types";
import type { CompareListItemType } from "../types";

// Where the plugin keeps a guest's list id in the store the cases run in.
const GUEST_LIST_KEY = `${GUEST_LIST_ID_STORAGE_KEY}:B2B-store`;

// The store talks to the server through the host's Apollo client and reads the store settings, the user and the
// notifications from the facade; each case sets what it needs here.
const facade = vi.hoisted(() => ({
  query: vi.fn(),
  mutate: vi.fn(),
  isAuthenticated: false,
  settings: {} as Record<string, unknown>,
  notifications: { success: vi.fn(), warning: vi.fn(), error: vi.fn(), clear: vi.fn() },
}));

vi.mock("@vc-frontend/core", async (importOriginal) => ({
  ...(await importOriginal<Record<string, unknown>>()),
  apolloClient: { query: facade.query, mutate: facade.mutate },
  globals: {
    storeId: "B2B-store",
    userId: "user-1",
    // Keys and their values, so a case can assert on what a message was filled with.
    i18n: { global: { t: (key: string, values: object = {}) => `${key} ${JSON.stringify(values)}`.trim() } },
    router: { currentRoute: { value: { fullPath: "/catalog/printers" } } },
  },
  useUser: () => ({ isAuthenticated: computed(() => facade.isAuthenticated) }),
  useModuleSettings: () => ({
    getSettingValue: (name: string) => facade.settings[name],
    isEnabled: (name: string) => facade.settings[name] === true,
  }),
  useNotifications: () => facade.notifications,
}));

type StoreModuleType = typeof import("./useCompareList");

function item(overrides: Partial<CompareListItemType> = {}): CompareListItemType {
  return { id: "item-1", productId: "product-1", categoryId: "printers", configurationSections: [], ...overrides };
}

function list(items: CompareListItemType[], id = "list-1"): CompareListFragment {
  return { id, itemsCount: items.length, items };
}

function printer(id = "product-9") {
  return {
    id,
    name: "LaserJet Pro",
    breadcrumbs: [
      { itemId: "office", typeName: "Category" },
      { itemId: "printers", typeName: "Category" },
    ],
  };
}

// A refusal as the X-API sends it: an ExecutionError whose code reaches the client in extensions.code.
function refusal(code: string) {
  return Object.assign(new Error(code), { graphQLErrors: [{ message: code, extensions: { code } }] });
}

type DocumentType = { definitions: readonly { name?: { value: string } }[] };

// Each case loads a fresh store module, which gets fresh copies of the generated documents: they are matched by
// operation name, not by identity.
function operationName(document: unknown): string | undefined {
  return (document as DocumentType).definitions[0].name?.value;
}

// The mutation's result field is the operation name with a lowercase first letter.
function resultField(document: unknown): string {
  const name = operationName(document)!;
  return name[0].toLowerCase() + name.slice(1);
}

// Answers each mutation with the list the case gives for its document, as the server returns the whole list.
function answerMutations(answers: [unknown, CompareListFragment | Error][]) {
  const byName = new Map(answers.map(([document, answer]) => [operationName(document), answer]));

  facade.mutate.mockImplementation(({ mutation }: { mutation: unknown }) => {
    const answer = byName.get(operationName(mutation));
    if (answer instanceof Error) {
      return Promise.reject(answer);
    }

    return Promise.resolve({ data: { [resultField(mutation)]: answer } });
  });
}

function mutationsOf(document: unknown) {
  return facade.mutate.mock.calls
    .filter(([options]) => operationName(options.mutation) === operationName(document))
    .map(([options]) => options);
}

// The store keeps its list at module level, as the host keeps the cart: every case starts from a fresh module with
// the list the server returns for it.
async function loadStore(serverList?: CompareListFragment): Promise<StoreModuleType> {
  vi.resetModules();
  facade.query.mockResolvedValue({ data: { compareList: serverList } });

  // A guest with a list is one whose browser saved it, and so remembers its id.
  if (serverList && !facade.isAuthenticated) {
    localStorage.setItem(GUEST_LIST_KEY, serverList.id);
  }

  const store = await import("./useCompareList");
  await store.loadCompareList();

  return store;
}

beforeEach(() => {
  vi.clearAllMocks();
  localStorage.clear();
  facade.isAuthenticated = false;
  facade.settings = {
    "ProductComparison.MaxItems": 5,
    "ProductComparison.AnonymousListEnabled": true,
  };
});

describe("loading the list", () => {
  it("reads the viewer's list and counts it for the header", async () => {
    const store = await loadStore(list([item(), item({ id: "item-2", productId: "product-2" })]));

    expect(operationName(facade.query.mock.calls[0][0].query)).toBe(operationName(CompareListDocument));
    expect(facade.query.mock.calls[0][0].variables).toEqual({ storeId: "B2B-store", userId: "user-1" });
    expect(store.useCompareCount().count.value).toBe(2);
  });

  // Decision D10: the guest's list is merged into the account's after sign-in, so the plugin remembers its id.
  it("remembers a guest's list, once the first save creates it, for the merge after sign-in", async () => {
    const store = await loadStore();
    answerMutations([[SaveProductsToCompareListDocument, list([item({ productId: "product-9" })], "guest-list")]]);

    await store.useCompareList().add(printer());

    expect(localStorage.getItem(GUEST_LIST_KEY)).toBe("guest-list");
  });

  // Most guests never compare: a guest's list is reachable only from the browser that remembers its id, so without
  // one a page load asks the server nothing.
  it("asks nothing for a guest who never compared", async () => {
    const store = await loadStore();

    expect(facade.query).not.toHaveBeenCalled();
    expect(store.useCompareCount().count.value).toBe(0);
  });

  // The cleanup deletes a guest list unchanged for two days: the browser stops asking for it.
  it("forgets a guest list the server no longer has", async () => {
    localStorage.setItem(GUEST_LIST_KEY, "cleaned-up");

    await loadStore();

    expect(facade.query).toHaveBeenCalledOnce();
    expect(localStorage.getItem(GUEST_LIST_KEY)).toBeNull();
  });

  it("remembers nothing for a signed-in buyer", async () => {
    facade.isAuthenticated = true;

    await loadStore(list([item()], "account-list"));

    expect(localStorage.getItem(GUEST_LIST_KEY)).toBeNull();
  });

  // The host reloads after sign-in, so the first load as the buyer is where the guest's products move over.
  it("merges the remembered guest list after sign-in, then forgets it", async () => {
    facade.isAuthenticated = true;
    localStorage.setItem(GUEST_LIST_KEY, "guest-list");
    answerMutations([[MergeCompareListDocument, list([item(), item({ id: "item-2" })], "account-list")]]);

    const store = await loadStore();

    expect(mutationsOf(MergeCompareListDocument)).toEqual([
      expect.objectContaining({
        variables: { command: { storeId: "B2B-store", userId: "user-1", secondListId: "guest-list" } },
      }),
    ]);
    expect(facade.query).not.toHaveBeenCalled();
    expect(store.useCompareCount().count.value).toBe(2);
    expect(localStorage.getItem(GUEST_LIST_KEY)).toBeNull();
  });

  // A merge the server refuses (the guest list belongs to someone else, or the account list is full) must not repeat
  // on every page load: the id is forgotten, and the buyer still gets their own list.
  it("does not retry a refused merge and loads the account's list instead", async () => {
    facade.isAuthenticated = true;
    localStorage.setItem(GUEST_LIST_KEY, "guest-list");
    answerMutations([[MergeCompareListDocument, refusal("Forbidden")]]);

    const store = await loadStore(list([item()], "account-list"));

    expect(facade.query).toHaveBeenCalledOnce();
    expect(store.useCompareCount().count.value).toBe(1);
    expect(localStorage.getItem(GUEST_LIST_KEY)).toBeNull();
  });

  // A request that never got an answer (offline, a gateway error) is not a refusal: forgetting the id then would lose
  // the guest's products for good, so the next page load merges again.
  it("keeps the guest list for the next page load when the merge request fails", async () => {
    facade.isAuthenticated = true;
    localStorage.setItem(GUEST_LIST_KEY, "guest-list");
    answerMutations([[MergeCompareListDocument, new Error("Failed to fetch")]]);

    await loadStore(list([item()], "account-list"));

    expect(mutationsOf(MergeCompareListDocument)).toHaveLength(1);
    expect(localStorage.getItem(GUEST_LIST_KEY)).toBe("guest-list");
  });

  // Stores on one origin share localStorage: a guest of another store has a list of its own there, which this store's
  // answers (here, that its guest list is gone) must leave alone.
  it("keeps each store's guest list apart", async () => {
    localStorage.setItem(`${GUEST_LIST_ID_STORAGE_KEY}:other-store`, "other-store-list");
    localStorage.setItem(GUEST_LIST_KEY, "cleaned-up");

    await loadStore();

    expect(localStorage.getItem(GUEST_LIST_KEY)).toBeNull();
    expect(localStorage.getItem(`${GUEST_LIST_ID_STORAGE_KEY}:other-store`)).toBe("other-store-list");
  });
});

describe("adding a product", () => {
  it("saves it in its category and says how many more fit there", async () => {
    const store = await loadStore(list([item()]));
    answerMutations([
      [SaveProductsToCompareListDocument, list([item(), item({ id: "item-9", productId: "product-9" })])],
    ]);

    await store.useCompareList().add(printer());

    expect(mutationsOf(SaveProductsToCompareListDocument)).toEqual([
      expect.objectContaining({
        variables: {
          command: {
            storeId: "B2B-store",
            userId: "user-1",
            products: [{ productId: "product-9", categoryId: "printers", configurationSections: undefined }],
          },
        },
        // The store shows its own message for a refusal, so the host's generic error toast stays quiet.
        context: { suppressErrorNotifications: true },
      }),
    ]);
    expect(facade.notifications.success).toHaveBeenCalledWith(
      expect.objectContaining({
        html: expect.stringContaining('"itemsLeft":3'),
        button: expect.objectContaining({ to: { path: "/compare", query: { category: "printers" } } }),
      }),
    );
    expect(store.useCompareList().findItem("product-9")).toBeDefined();
  });

  it("saves the product page's configuration with it", async () => {
    const store = await loadStore();
    const configurationSections = [{ sectionId: "a", type: "Text", customText: "engraving" }];
    answerMutations([[SaveProductsToCompareListDocument, list([item({ productId: "product-9" })])]]);

    await store.useCompareList().add(printer(), configurationSections);

    expect(mutationsOf(SaveProductsToCompareListDocument)[0].variables.command.products).toEqual([
      { productId: "product-9", categoryId: "printers", configurationSections },
    ]);
  });

  // AC 3: with guest lists off, a guest's click asks them to sign in, and the sign-in brings them back.
  it("asks a guest to sign in when the store keeps no guest lists", async () => {
    facade.settings["ProductComparison.AnonymousListEnabled"] = false;
    const store = await loadStore();

    await store.useCompareList().add(printer());

    expect(facade.mutate).not.toHaveBeenCalled();
    expect(facade.notifications.warning).toHaveBeenCalledWith(
      expect.objectContaining({
        text: expect.stringContaining("product_comparison.notifications.sign_in_required"),
        button: expect.objectContaining({ to: { name: "SignIn", query: { returnUrl: "/catalog/printers" } } }),
      }),
    );
  });

  it("lets a signed-in buyer add with guest lists off", async () => {
    facade.isAuthenticated = true;
    facade.settings["ProductComparison.AnonymousListEnabled"] = false;
    const store = await loadStore();
    answerMutations([[SaveProductsToCompareListDocument, list([item({ productId: "product-9" })])]]);

    await store.useCompareList().add(printer());

    expect(mutationsOf(SaveProductsToCompareListDocument)).toHaveLength(1);
  });

  // The limit is per category (decision D3): a full category is refused before any request, another category is not.
  it("warns without a request when the product's category is full", async () => {
    facade.settings["ProductComparison.MaxItems"] = 2;
    const store = await loadStore(list([item(), item({ id: "item-2", productId: "product-2" })]));

    await store.useCompareList().add(printer());

    expect(facade.mutate).not.toHaveBeenCalled();
    expect(facade.notifications.warning).toHaveBeenCalledWith(
      expect.objectContaining({ text: expect.stringContaining("product_comparison.notifications.limit_reached") }),
    );
  });

  it.each([
    ["COMPARE_LIST_LIMIT_REACHED", "product_comparison.notifications.limit_reached"],
    ["COMPARE_LIST_FULL", "product_comparison.notifications.list_full"],
    ["ANONYMOUS_COMPARE_LIST_DISABLED", "product_comparison.notifications.sign_in_required"],
  ])("shows the message for the server's %s refusal", async (code, message) => {
    const store = await loadStore();
    answerMutations([[SaveProductsToCompareListDocument, refusal(code)]]);

    await store.useCompareList().add(printer());

    expect(facade.notifications.warning).toHaveBeenCalledWith(
      expect.objectContaining({ text: expect.stringContaining(message) }),
    );
    expect(facade.notifications.success).not.toHaveBeenCalled();
  });

  // With the host's toast suppressed, a failure the store has no message for must still say something.
  it("reports any other failure as a generic error", async () => {
    const store = await loadStore();
    answerMutations([[SaveProductsToCompareListDocument, new Error("network")]]);

    await store.useCompareList().add(printer());

    expect(facade.notifications.error).toHaveBeenCalledWith(
      expect.objectContaining({ text: expect.stringContaining("common.messages.something_went_wrong") }),
    );
  });
});

describe("finding a product's item", () => {
  const configured = item({
    id: "configured",
    configurationSections: [{ sectionId: "a", type: "Text", customText: "engraving" }],
  });

  // One product can be compared plain (from a card) and configured (from its page) at once: each is its own item.
  it("tells the plain item and a configured one apart", async () => {
    const store = await loadStore(list([item({ id: "plain" }), configured]));
    const { findItem } = store.useCompareList();

    expect(findItem("product-1")?.id).toBe("plain");
    expect(findItem("product-1", [{ sectionId: "a", type: "Text", customText: "engraving" }])?.id).toBe("configured");
    expect(findItem("product-1", [{ sectionId: "a", type: "Text", customText: "other" }])).toBeUndefined();
  });
});

describe("removing and clearing", () => {
  it("removes an item and names the product", async () => {
    const store = await loadStore(list([item()]));
    answerMutations([[RemoveCompareListItemsDocument, list([])]]);

    await store.useCompareList().remove(item(), "LaserJet Pro");

    expect(mutationsOf(RemoveCompareListItemsDocument)[0].variables.command.itemIds).toEqual(["item-1"]);
    expect(facade.notifications.warning).toHaveBeenCalledWith(
      expect.objectContaining({ html: expect.stringContaining("LaserJet Pro") }),
    );
    expect(store.useCompareCount().count.value).toBe(0);
  });

  // An item whose product is gone has no name to show; the column disappearing is the feedback.
  it("removes a no-longer-available item quietly", async () => {
    const store = await loadStore(list([item()]));
    answerMutations([[RemoveCompareListItemsDocument, list([])]]);

    await store.useCompareList().remove(item());

    expect(facade.notifications.warning).not.toHaveBeenCalled();
  });

  // "Restore products" on the empty page undoes the clear, configuration included, in the original order.
  it("restores what clear all removed", async () => {
    const configured = item({
      id: "item-2",
      productId: "product-2",
      categoryId: undefined,
      configurationSections: [{ sectionId: "a", type: "Product", productId: "option-1", quantity: 2 }],
    });
    const store = await loadStore(list([item(), configured]));
    answerMutations([
      [ClearCompareListDocument, list([])],
      [SaveProductsToCompareListDocument, list([item(), configured])],
    ]);
    const { clear, restore, canRestore } = store.useCompareList();

    await clear();
    expect(canRestore.value).toBe(true);

    await restore();

    expect(mutationsOf(SaveProductsToCompareListDocument)[0].variables.command.products).toEqual([
      { productId: "product-1", categoryId: "printers", configurationSections: [] },
      {
        productId: "product-2",
        categoryId: undefined,
        configurationSections: [
          { sectionId: "a", type: "Product", option: { productId: "option-1", quantity: 2 }, customText: undefined },
        ],
      },
    ]);
    expect(canRestore.value).toBe(false);
  });

  // The page offers "Restore" only on its empty state: clearing one category of several leaves nothing to restore.
  it("offers a restore after clearing a category only when that emptied the list", async () => {
    const store = await loadStore(
      list([item(), item({ id: "item-2", productId: "product-2", categoryId: "laptops" })]),
    );
    answerMutations([[ClearCompareListCategoryDocument, list([item({ id: "item-2", categoryId: "laptops" })])]]);
    const { clearCategory, canRestore } = store.useCompareList();

    await clearCategory("printers");

    expect(mutationsOf(ClearCompareListCategoryDocument)[0].variables.command.categoryId).toBe("printers");
    expect(canRestore.value).toBe(false);
  });

  // The uncategorized group comes back with a null id; the command leaves the category out for it.
  it("clears the uncategorized group by sending no category", async () => {
    const store = await loadStore(list([item({ categoryId: undefined })]));
    answerMutations([[ClearCompareListCategoryDocument, list([])]]);
    const { clearCategory, canRestore } = store.useCompareList();

    await clearCategory(null);

    expect(mutationsOf(ClearCompareListCategoryDocument)[0].variables.command.categoryId).toBeUndefined();
    expect(canRestore.value).toBe(true);
  });
});

// The server saves a list whole, so two requests at once could lose one's change: the store sends them in turn.
describe("one request at a time", () => {
  it("sends the second save only after the first has answered", async () => {
    const store = await loadStore();
    let answerFirst: (value: unknown) => void = () => {};
    facade.mutate
      .mockImplementationOnce(() => new Promise((resolve) => (answerFirst = resolve)))
      .mockResolvedValue({ data: { saveProductsToCompareList: list([item(), item({ id: "item-2" })]) } });
    const { add } = store.useCompareList();

    const first = add(printer("product-1"));
    const second = add(printer("product-2"));
    await Promise.resolve();

    expect(facade.mutate).toHaveBeenCalledOnce();

    answerFirst({ data: { saveProductsToCompareList: list([item()]) } });
    await Promise.all([first, second]);

    expect(facade.mutate).toHaveBeenCalledTimes(2);
  });
});

// The product page asks the plugin for the configuration a "Customize" link names (registerProductConfigurationSource).
describe("the configuration source", () => {
  const configured = item({
    id: "configured",
    configurationSections: [{ sectionId: "a", type: "Text", customText: "engraving" }],
  });

  it("answers the viewer's own configured item with its selection", async () => {
    const store = await loadStore(list([configured]));

    expect(await store.getItemConfiguration("configured", "product-1")).toEqual([
      { sectionId: "a", type: "Text", option: undefined, customText: "engraving" },
    ]);
  });

  // A shared link names an item of someone else's list, or of another product: the page opens with its defaults.
  it("answers nothing for an item that is not the viewer's, or not this product's", async () => {
    const store = await loadStore(list([configured]));

    expect(await store.getItemConfiguration("someone-elses-item", "product-1")).toBeUndefined();
    expect(await store.getItemConfiguration("configured", "product-2")).toBeUndefined();
  });

  it("answers nothing for a plain item", async () => {
    const store = await loadStore(list([item()]));

    expect(await store.getItemConfiguration("item-1", "product-1")).toBeUndefined();
  });

  // A link opened in a fresh tab asks before the list has loaded: the answer waits for it.
  it("waits for the list to load", async () => {
    vi.resetModules();
    let answer: (value: unknown) => void = () => {};
    facade.query.mockImplementation(() => new Promise((resolve) => (answer = resolve)));
    localStorage.setItem(GUEST_LIST_KEY, "list-1");
    const store = await import("./useCompareList");

    void store.loadCompareList();
    const configuration = store.getItemConfiguration("configured", "product-1");
    await vi.waitFor(() => expect(facade.query).toHaveBeenCalled());
    answer({ data: { compareList: list([configured]) } });

    expect(await configuration).toHaveLength(1);
  });
});
