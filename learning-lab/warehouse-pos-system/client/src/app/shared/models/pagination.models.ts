// Mirrors Common.Pagination.PagedResult<T> (backend, F1) — the one
// paged-response shape every service that adopts real paging returns, so
// this is the only paging contract the Angular client ever needs to know,
// same reasoning as the backend's own shared BuildingBlocks project.
export interface PagedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}

// A typed empty page to initialize a signal with, before the first real
// response arrives — avoids every component re-declaring the same
// all-zeros literal.
export function emptyPage<T>(pageSize = 20): PagedResult<T> {
  return { items: [], page: 1, pageSize, totalCount: 0, totalPages: 0 };
}
