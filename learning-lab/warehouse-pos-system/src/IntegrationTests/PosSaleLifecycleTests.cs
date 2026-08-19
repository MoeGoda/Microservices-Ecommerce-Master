using System.Net.Http.Json;
using Xunit;

namespace IntegrationTests
{
    [Collection("Gateway")]
    public class PosSaleLifecycleTests
    {
        // Seeded Cola: $1.80/unit, barcode below, 50 on hand at shelf A1
        // (LocationId 1) — see WarehouseContextSeed. Tax rate is POS.API's
        // own appsettings "Tax:RatePercent" = 8.5.
        private const string ColaBarcode = "5901234123457";
        private const int ShelfA1LocationId = 1;

        private readonly GatewayFixture _fixture;

        public PosSaleLifecycleTests(GatewayFixture fixture)
        {
            _fixture = fixture;
        }

        [Fact]
        public async Task FullHappyPath_StartSaleAddLineCheckout_ComputesCorrectTotals()
        {
            using var client = _fixture.CreateAdminClient();

            var sale = await StartSale(client);
            sale = await AddLine(client, sale.Id, ColaBarcode, quantity: 2);

            // 1.80 x 2 = 3.60 net; 3.60 x 8.5% = 0.306 -> rounds to 0.31; total 3.91.
            Assert.Equal(3.60m, sale.NetTotal);
            Assert.Equal(0.31m, sale.TaxAmount);
            Assert.Equal(3.91m, sale.Total);
            Assert.Equal("InProgress", sale.Status);
            Assert.Single(sale.Lines);
            Assert.Equal(2, sale.Lines[0].Quantity);

            var completed = await CheckoutAndWaitForStockSync(client, sale.Id);
            Assert.Equal("Completed", completed.Status);
            Assert.NotNull(completed.CompletedAt);
            Assert.Equal(3.91m, completed.Total);
        }

        [Fact]
        public async Task Checkout_DecrementsWarehouseStockByExactlyTheLineQuantity()
        {
            using var client = _fixture.CreateAdminClient();
            var item = await client.GetFromJsonAsync<ItemDetailModel>($"/Warehouse/Items/barcodes/{ColaBarcode}");
            var quantityBefore = await GetQuantityAtShelfA1(client, item!.Id);

            var sale = await StartSale(client);
            await AddLine(client, sale.Id, ColaBarcode, quantity: 3);
            // CheckoutAndWaitForStockSync blocks until POS.Application's own
            // OutboxDispatcher marks THIS sale's StockSyncStatus as Synced —
            // i.e. until Warehouse has specifically acknowledged decrementing
            // for this sale, not just "some pending delivery got flushed."
            // Polling the shared QuantityOnHand instead would race with
            // whichever other test's checkout lands in the same outbox
            // dispatch batch (both are async against the same location).
            await CheckoutAndWaitForStockSync(client, sale.Id);

            var quantityAfter = await GetQuantityAtShelfA1(client, item.Id);
            Assert.Equal(quantityBefore - 3, quantityAfter);
        }

        [Fact]
        public async Task Checkout_ThenSaleAppearsInReportingSalesLedger()
        {
            using var client = _fixture.CreateAdminClient();
            var sale = await StartSale(client);
            await AddLine(client, sale.Id, ColaBarcode, quantity: 1);
            var completed = await CheckoutAndWaitForStockSync(client, sale.Id);

            await Polling.Until(
                () => client.GetFromJsonAsync<PagedResultModel<SaleRecordModel>>("/Reporting/sales?page=1&pageSize=50"),
                page => page.Items.Any(s => s.SaleId == completed.Id),
                $"Reporting never ingested completed sale #{completed.Id} into the sales ledger");
        }

        [Fact]
        public async Task Return_AfterCheckout_RestoresWarehouseStockToItsOriginalLevel()
        {
            using var client = _fixture.CreateAdminClient();
            var item = await client.GetFromJsonAsync<ItemDetailModel>($"/Warehouse/Items/barcodes/{ColaBarcode}");
            var quantityBefore = await GetQuantityAtShelfA1(client, item!.Id);

            var sale = await StartSale(client);
            await AddLine(client, sale.Id, ColaBarcode, quantity: 4);
            var completed = await CheckoutAndWaitForStockSync(client, sale.Id);

            var quantityAfterCheckout = await GetQuantityAtShelfA1(client, item.Id);
            Assert.Equal(quantityBefore - 4, quantityAfterCheckout);

            var returnResponse = await client.PostAsync($"/Pos/Sales/{completed.Id}/return", content: null);
            returnResponse.EnsureSuccessStatusCode();
            var returned = await returnResponse.Content.ReadFromJsonAsync<SaleModel>();
            Assert.Equal("Returned", returned!.Status);
            Assert.NotNull(returned.ReturnedAt);

            // The return's own restock is dispatched through the exact same
            // per-sale outbox/StockSyncStatus mechanism as checkout — see
            // ReturnSaleCommandHandler's own SaleReturned event.
            await Polling.Until(
                () => client.GetFromJsonAsync<List<StockLevelModel>>($"/Warehouse/Stock/{item.Id}"),
                levels => levels.Single(l => l.LocationId == ShelfA1LocationId).QuantityOnHand == quantityBefore,
                "stock was never restored to its pre-sale level after the return");
        }

        private static async Task<SaleModel> StartSale(HttpClient client)
        {
            var response = await client.PostAsJsonAsync("/Pos/Sales", new { LocationId = ShelfA1LocationId });
            response.EnsureSuccessStatusCode();
            return (await response.Content.ReadFromJsonAsync<SaleModel>())!;
        }

        private static async Task<SaleModel> AddLine(HttpClient client, int saleId, string barcode, int quantity)
        {
            var response = await client.PostAsJsonAsync($"/Pos/Sales/{saleId}/lines", new { Barcode = barcode, Quantity = quantity });
            response.EnsureSuccessStatusCode();
            return (await response.Content.ReadFromJsonAsync<SaleModel>())!;
        }

        private static async Task<SaleModel> Checkout(HttpClient client, int saleId)
        {
            var response = await client.PostAsync($"/Pos/Sales/{saleId}/checkout", content: null);
            response.EnsureSuccessStatusCode();
            return (await response.Content.ReadFromJsonAsync<SaleModel>())!;
        }

        // Checkout itself returns synchronously with StockSyncStatus still
        // "Pending" — the actual Warehouse decrement happens later, off
        // POS.Application's own outbox dispatch cycle. Waiting here for it
        // to reach a terminal state before the test (and the next test)
        // moves on is what keeps every other assertion in this class
        // honest about "before"/"after" stock snapshots.
        private static async Task<SaleModel> CheckoutAndWaitForStockSync(HttpClient client, int saleId)
        {
            await Checkout(client, saleId);
            return await Polling.Until(
                () => client.GetFromJsonAsync<SaleModel>($"/Pos/Sales/{saleId}"),
                sale => sale.StockSyncStatus is "Synced" or "Failed",
                $"sale #{saleId}'s StockSyncStatus never left Pending");
        }

        private static async Task<int> GetQuantityAtShelfA1(HttpClient client, int itemId)
        {
            var levels = await client.GetFromJsonAsync<List<StockLevelModel>>($"/Warehouse/Stock/{itemId}");
            return levels!.Single(l => l.LocationId == ShelfA1LocationId).QuantityOnHand;
        }
    }
}
