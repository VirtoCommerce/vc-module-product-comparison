import type { CompareListFragment, CompareProductFragment, CompareProductsQuery } from "./api/graphql/types";

export type CompareListItemType = CompareListFragment["items"][number];

export type CompareCategoryType = NonNullable<CompareProductsQuery["compareList"]>["categories"][number];

export type ComparePageItemType = CompareCategoryType["items"][number];

/** A column of the compare table: the item, and its product when the catalog still has it. */
export interface ICompareColumn {
  item: ComparePageItemType;
  product?: CompareProductFragment;
}

export interface ICompareCategoryTab {
  key: string;
  label: string;
  count: number;
}

export type CompareRowKindType = "text" | "price" | "rating" | "availability" | "boolean";

export interface ICompareTableRow {
  key: string;
  label: string;
  kind: CompareRowKindType;
  /** One per column; an unavailable product's is the placeholder. */
  values: string[];
  differs: boolean;
  description?: string;
  /** Only for "boolean" rows: one per column, undefined where the product lacks the property. */
  boolValues?: (boolean | undefined)[];
}
