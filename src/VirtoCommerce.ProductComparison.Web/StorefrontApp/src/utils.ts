import { getProductRoute } from "@vc-frontend/core";
import { CATEGORY_BREADCRUMB_TYPE_NAME, COMPARE_CATEGORY_DEPTH, COMPARE_ITEM_ID_PARAM } from "./constants";
import type { CompareListItemType, ICompareColumn } from "./types";
import type { CompareProductFragment } from "./api/graphql/types";
import type { ConfigurationSectionInput } from "@vc-frontend/core";
import type { RouteLocationRaw } from "vue-router";

type BreadcrumbType = { itemId: string; typeName?: string };
type SectionKeyPartsType = {
  sectionId: string;
  type: string;
  productId?: string;
  quantity?: number;
  customText?: string;
};

export function truncate(value: string, length: number): string {
  return value.length > length ? `${value.slice(0, length)}...` : value;
}

/** The category a product is compared in: the deepest of its first two category breadcrumbs. */
export function getProductCategoryId(product: { breadcrumbs?: BreadcrumbType[] }): string | undefined {
  return (product.breadcrumbs ?? [])
    .filter((breadcrumb) => breadcrumb.typeName === CATEGORY_BREADCRUMB_TYPE_NAME)
    .slice(0, COMPARE_CATEGORY_DEPTH)
    .at(-1)?.itemId;
}

/** A product is one of its own variations, so the count is one more than the `variations` array. */
export function getVariationsCount(product: CompareProductFragment): number {
  return product.variations.length + 1;
}

export function getDisplayPrice(product: CompareProductFragment) {
  return product.hasVariations && product.minVariationPrice ? product.minVariationPrice : product.price;
}

// The server's configuration key, so a list item and the product page's selection match as the server matches
// them: sections by id, then id, type, product, quantity and text.
function getConfigurationKey(sections: SectionKeyPartsType[]): string {
  return [...sections]
    .sort((a, b) => (a.sectionId < b.sectionId ? -1 : a.sectionId > b.sectionId ? 1 : 0))
    .map((x) => [x.sectionId, x.type, x.productId ?? "", x.quantity ?? "", x.customText ?? ""].join("|"))
    .join("\n");
}

export function getItemConfigurationKey(item: CompareListItemType): string {
  return getConfigurationKey(item.configurationSections);
}

export function getSelectionConfigurationKey(sections: readonly ConfigurationSectionInput[] = []): string {
  return getConfigurationKey(
    sections.map((section) => ({
      sectionId: section.sectionId,
      type: section.type,
      productId: section.option?.productId,
      quantity: section.option?.quantity,
      customText: section.customText ?? undefined,
    })),
  );
}

export function toConfigurationSectionInputs(item: Pick<CompareListItemType, "configurationSections">) {
  return item.configurationSections.map<ConfigurationSectionInput>((section) => ({
    sectionId: section.sectionId,
    type: section.type,
    option: section.productId ? { productId: section.productId, quantity: section.quantity! } : undefined,
    customText: section.customText,
  }));
}

/** The product page; for a configured item, preselecting its configuration through the plugin's source. */
export function getConfigurationLink(item: ICompareColumn["item"], product: CompareProductFragment): RouteLocationRaw {
  const route = getProductRoute(product.id, product.slug);
  const routeAsObject = typeof route === "string" ? { path: route } : route;

  if (!item.configurationSections.length) {
    return routeAsObject;
  }

  return { ...routeAsObject, query: { ...routeAsObject.query, [COMPARE_ITEM_ID_PARAM]: item.id } };
}

/** A list's category groups by id; the uncategorized group's id is null, its key "". */
export function getCategoryKey(categoryId?: string | null): string {
  return categoryId ?? "";
}

export function getCategoryItems<T extends { categoryId?: string | null }>(
  items: T[],
  categoryId?: string | null,
): T[] {
  return items.filter((item) => getCategoryKey(item.categoryId) === getCategoryKey(categoryId));
}
