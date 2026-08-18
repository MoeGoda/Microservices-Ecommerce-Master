import { PagedResult } from '../models/pagination.models';

// M — Receipts/Transfers/Adjustments/Issues all read from the same
// server-paged stock-movements ledger (max pageSize 100, no server-side
// reason filter) and then need to filter that page down by reason/sign
// for their own screen before showing it in the same PagedResult-shaped
// grid every other screen already uses. This is the one bit of math
// (slice + totalCount + totalPages) shared by all four, kept as a plain
// function rather than a component since there's no UI in it.
export function paginateClientSide<T>(items: readonly T[], page: number, pageSize: number): PagedResult<T> {
  const start = (page - 1) * pageSize;
  return {
    items: items.slice(start, start + pageSize),
    page,
    pageSize,
    totalCount: items.length,
    totalPages: Math.max(1, Math.ceil(items.length / pageSize)),
  };
}
