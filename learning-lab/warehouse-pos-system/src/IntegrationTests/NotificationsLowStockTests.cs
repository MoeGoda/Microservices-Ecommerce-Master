using System.Net.Http.Json;
using Xunit;

namespace IntegrationTests
{
    [Collection("Gateway")]
    public class NotificationsLowStockTests
    {
        // Household category, its own aisle/shelf (B1, LocationId 3) and a
        // freshly-created item per test run — deliberately not touching
        // seeded Cola, so this test's own stock adjustment can't race with
        // the other test classes' checkout/receive/return traffic against
        // the same item+location.
        private const int HouseholdCategoryId = 3;
        private const int PcsUnitOfMeasureId = 1;
        private const int ShelfB1LocationId = 3;

        private readonly GatewayFixture _fixture;

        public NotificationsLowStockTests(GatewayFixture fixture)
        {
            _fixture = fixture;
        }

        [Fact]
        public async Task StockAdjustment_CrossingBelowReorderThreshold_ProducesLowStockNotification()
        {
            using var client = _fixture.CreateAdminClient();

            var sku = $"INT-TEST-{Guid.NewGuid():N}"[..20];
            var createResponse = await client.PostAsJsonAsync("/Warehouse/Items", new
            {
                Sku = sku,
                Name = "Integration Test Household Item",
                UnitPrice = 5.00m,
                CategoryId = HouseholdCategoryId,
                BaseUnitOfMeasureId = PcsUnitOfMeasureId,
                Barcode = $"9{Guid.NewGuid():N}"[..13],
            });
            createResponse.EnsureSuccessStatusCode();
            var item = (await createResponse.Content.ReadFromJsonAsync<ItemDetailModel>())!;

            // ReceiveStock's own createIfMissing path defaults a brand-new
            // StockLevel's ReorderThreshold to 0 (see StockAdjustmentStager) —
            // receiving above zero first, then adjusting down to a negative
            // balance, is what actually crosses "at/below its reorder point"
            // for an item Notifications has never seen a StockLevelChanged
            // event for before.
            var receiveResponse = await client.PostAsJsonAsync("/Warehouse/Stock/receive", new
            {
                ItemId = item.Id,
                LocationId = ShelfB1LocationId,
                Quantity = 20,
                UnitOfMeasureId = PcsUnitOfMeasureId,
                Reference = "integration-test-seed",
            });
            receiveResponse.EnsureSuccessStatusCode();

            var adjustResponse = await client.PostAsJsonAsync("/Warehouse/Stock/adjust", new
            {
                ItemId = item.Id,
                LocationId = ShelfB1LocationId,
                QuantityChange = -20,
                Reference = "integration-test-cross-below-threshold",
            });
            adjustResponse.EnsureSuccessStatusCode();

            await Polling.Until(
                () => client.GetFromJsonAsync<List<NotificationModel>>("/Notifications?take=100"),
                notifications => notifications.Any(n => n.Type == "LowStock" && n.Message.Contains(sku)),
                $"no LowStock notification ever arrived for item '{sku}' crossing below its reorder threshold");
        }
    }
}
