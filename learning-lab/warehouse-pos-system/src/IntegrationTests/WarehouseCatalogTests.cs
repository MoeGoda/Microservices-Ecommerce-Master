using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace IntegrationTests
{
    [Collection("Gateway")]
    public class WarehouseCatalogTests
    {
        // Seeded by WarehouseContextSeed — Cola 330ml, primary barcode,
        // shelf A1 (LocationId 1), $1.80 base price.
        private const string ColaBarcode = "5901234123457";
        private const int ShelfA1LocationId = 1;
        private const int PcsUnitOfMeasureId = 1;

        private readonly GatewayFixture _fixture;

        public WarehouseCatalogTests(GatewayFixture fixture)
        {
            _fixture = fixture;
        }

        [Fact]
        public async Task ResolveBarcode_SeededColaBarcode_ReturnsItemWithExpectedPrice()
        {
            using var client = _fixture.CreateAdminClient();
            var item = await client.GetFromJsonAsync<ItemDetailModel>($"/Warehouse/Items/barcodes/{ColaBarcode}");

            Assert.NotNull(item);
            Assert.Equal("BEV-COLA-330", item!.Sku);
            Assert.Equal(1.80m, item.UnitPrice);
        }

        [Fact]
        public async Task ResolveBarcode_UnknownBarcode_ReturnsNotFound()
        {
            using var client = _fixture.CreateAdminClient();
            var response = await client.GetAsync("/Warehouse/Items/barcodes/0000000000000");

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task ReceiveStock_IncreasesQuantityOnHandByExactlyTheReceivedAmount()
        {
            using var client = _fixture.CreateAdminClient();
            var item = await client.GetFromJsonAsync<ItemDetailModel>($"/Warehouse/Items/barcodes/{ColaBarcode}");
            var before = await GetQuantityAtShelfA1(client, item!.Id);

            var response = await client.PostAsJsonAsync("/Warehouse/Stock/receive", new
            {
                ItemId = item.Id,
                LocationId = ShelfA1LocationId,
                Quantity = 7,
                UnitOfMeasureId = PcsUnitOfMeasureId,
                Reference = "integration-test-receive",
            });
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var after = await GetQuantityAtShelfA1(client, item.Id);
            Assert.Equal(before + 7, after);
        }

        private static async Task<int> GetQuantityAtShelfA1(HttpClient client, int itemId)
        {
            var levels = await client.GetFromJsonAsync<List<StockLevelModel>>($"/Warehouse/Stock/{itemId}");
            return levels!.Single(l => l.LocationId == ShelfA1LocationId).QuantityOnHand;
        }
    }
}
