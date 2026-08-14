namespace Common.Pagination
{
    // Shared across services the same way Common.Exceptions is — this is
    // NOT domain data (no service's own EntityBase/DbContext lives here,
    // and the "no shared domain assemblies across services" rule this
    // project otherwise follows everywhere else stays intact), it's a
    // generic wire-format envelope, the exact same category as
    // Common.ExceptionHandling's ProblemDetails shape. Every list endpoint
    // that adopts real pagination (as opposed to the existing flat
    // "top N" idiom GetTopSellingItemsQuery/GetRecentNotificationsQuery
    // already used) returns this same shape, so the Angular client only
    // ever needs to understand ONE paged-response contract, not one per
    // service.
    public class PagedResult<T>
    {
        public IReadOnlyList<T> Items { get; set; } = Array.Empty<T>();
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }

        public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);

        public static PagedResult<T> Create(IReadOnlyList<T> items, int page, int pageSize, int totalCount)
        {
            return new PagedResult<T> { Items = items, Page = page, PageSize = pageSize, TotalCount = totalCount };
        }
    }
}
