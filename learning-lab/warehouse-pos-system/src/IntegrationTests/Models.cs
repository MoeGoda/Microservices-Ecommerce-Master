namespace IntegrationTests
{
    // Deliberately hand-copied, minimal wire shapes rather than project
    // references into the 5 services' own DTOs — this project tests the
    // gateway's real HTTP contract as an outside caller sees it, the same
    // way the Angular client does, not the server-side types.
    public record PagedResultModel<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount);

    public record ItemDetailModel(int Id, string Sku, string Name, decimal UnitPrice, int CategoryId, int BaseUnitOfMeasureId);

    public record StockLevelModel(int ItemId, int LocationId, string LocationCode, int QuantityOnHand, int ReorderThreshold);

    public record SaleLineModel(int Id, int ItemId, string Sku, decimal UnitPrice, int Quantity, decimal LineTotal);

    public record SaleModel(
        int Id,
        string DocumentNumber,
        int LocationId,
        int? CustomerId,
        string Status,
        decimal NetTotal,
        decimal TaxAmount,
        decimal Total,
        DateTime? CompletedAt,
        DateTime? ReturnedAt,
        string StockSyncStatus,
        List<SaleLineModel> Lines);

    public record CustomerModel(int Id, string Name, string? Phone, string? Email, int LoyaltyPoints, decimal Balance);

    public record CashDrawerSessionModel(int Id, int LocationId, decimal OpeningFloat, DateTime OpenedAt);

    public record CashMovementModel(int Id, string Type, decimal Amount, string Reason);

    public record CashDrawerXReportModel(
        int SessionId,
        decimal OpeningFloat,
        decimal CashInTotal,
        decimal CashOutTotal,
        int CompletedSaleCount,
        decimal SalesTotal,
        decimal ExpectedCashInDrawer);

    public record SaleRecordModel(int SaleId, int LocationId, decimal Total, DateTime CompletedAtUtc, int LineCount);

    public record NotificationModel(int Id, string Type, string Message, bool IsRead, DateTime CreatedAt);
}
