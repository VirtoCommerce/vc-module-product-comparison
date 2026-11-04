import { computed, ref, toValue } from "vue";
import type { ICompareTableRow } from "../types";
import type { MaybeRefOrGetter } from "vue";

/**
 * "Pin to top" for the compare table: a pinned row stays at the top under either filter. Local UI state: it lasts
 * while the table is mounted, which spans category tab switches.
 */
export function useCompareTableRowPins(rows: MaybeRefOrGetter<ICompareTableRow[]>) {
  const pinnedRowKeys = ref<Set<string>>(new Set());

  function togglePin(key: string) {
    const next = new Set(pinnedRowKeys.value);

    if (next.has(key)) {
      next.delete(key);
    } else {
      next.add(key);
    }

    pinnedRowKeys.value = next;
  }

  function isRowPinned(key: string): boolean {
    return pinnedRowKeys.value.has(key);
  }

  // Pinned rows keep the order they were pinned in.
  const pinnedRows = computed(() => {
    const rowsByKey = new Map(toValue(rows).map((row) => [row.key, row]));
    return Array.from(pinnedRowKeys.value)
      .map((key) => rowsByKey.get(key))
      .filter((row): row is ICompareTableRow => row !== undefined);
  });

  const unpinnedRows = computed(() => toValue(rows).filter((row) => !pinnedRowKeys.value.has(row.key)));

  return {
    isRowPinned,
    togglePin,
    pinnedRows,
    unpinnedRows,
  };
}
