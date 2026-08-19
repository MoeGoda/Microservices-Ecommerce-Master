using System.Net.Http.Json;
using Xunit;

namespace IntegrationTests
{
    [Collection("Gateway")]
    public class PosCustomerLoyaltyTests
    {
        private const string ColaBarcode = "5901234123457";
        private const int ShelfA1LocationId = 1;

        private readonly GatewayFixture _fixture;

        public PosCustomerLoyaltyTests(GatewayFixture fixture)
        {
            _fixture = fixture;
        }

        [Fact]
        public async Task AttachCustomer_CheckoutOverTenDollars_EarnsOnePointPerTenDollarsFloored()
        {
            using var client = _fixture.CreateAdminClient();

            var customerResponse = await client.PostAsJsonAsync("/Pos/Customers", new { Name = $"Integration Customer {Guid.NewGuid():N}" });
            customerResponse.EnsureSuccessStatusCode();
            var customer = (await customerResponse.Content.ReadFromJsonAsync<CustomerModel>())!;
            Assert.Equal(0, customer.LoyaltyPoints);

            var startResponse = await client.PostAsJsonAsync("/Pos/Sales", new { LocationId = ShelfA1LocationId });
            startResponse.EnsureSuccessStatusCode();
            var sale = (await startResponse.Content.ReadFromJsonAsync<SaleModel>())!;

            var attachResponse = await client.PutAsJsonAsync($"/Pos/Sales/{sale.Id}/customer", new { CustomerId = customer.Id });
            attachResponse.EnsureSuccessStatusCode();
            var attached = (await attachResponse.Content.ReadFromJsonAsync<SaleModel>())!;
            Assert.Equal(customer.Id, attached.CustomerId);

            // 1.80 x 6 = 10.80 net; +8.5% tax = 11.72 total. floor(11.72 / 10) = 1 point.
            var lineResponse = await client.PostAsJsonAsync($"/Pos/Sales/{sale.Id}/lines", new { Barcode = ColaBarcode, Quantity = 6 });
            lineResponse.EnsureSuccessStatusCode();

            var checkoutResponse = await client.PostAsync($"/Pos/Sales/{sale.Id}/checkout", content: null);
            checkoutResponse.EnsureSuccessStatusCode();
            var completed = (await checkoutResponse.Content.ReadFromJsonAsync<SaleModel>())!;
            Assert.Equal(11.72m, completed.Total);

            var updatedCustomer = await client.GetFromJsonAsync<CustomerModel>($"/Pos/Customers/{customer.Id}");
            Assert.Equal(1, updatedCustomer!.LoyaltyPoints);

            // Loyalty points post synchronously in the same request, but
            // the Warehouse stock decrement doesn't — draining it here
            // (rather than leaving it Pending) keeps this sale's async
            // effect from landing in the middle of whichever stock-level
            // test runs next against the same Cola@A1 location.
            await Polling.Until(
                () => client.GetFromJsonAsync<SaleModel>($"/Pos/Sales/{sale.Id}"),
                s => s.StockSyncStatus is "Synced" or "Failed",
                $"sale #{sale.Id}'s StockSyncStatus never left Pending");
        }

        [Fact]
        public async Task AdjustBalance_PositiveThenNegativeDelta_NetsCorrectly()
        {
            using var client = _fixture.CreateAdminClient();
            var customerResponse = await client.PostAsJsonAsync("/Pos/Customers", new { Name = $"Balance Customer {Guid.NewGuid():N}" });
            customerResponse.EnsureSuccessStatusCode();
            var customer = (await customerResponse.Content.ReadFromJsonAsync<CustomerModel>())!;

            var creditResponse = await client.PostAsJsonAsync($"/Pos/Customers/{customer.Id}/balance-adjustments", new { Delta = 50m, Reason = "Integration test credit" });
            creditResponse.EnsureSuccessStatusCode();
            var credited = (await creditResponse.Content.ReadFromJsonAsync<CustomerModel>())!;
            Assert.Equal(50m, credited.Balance);

            var debitResponse = await client.PostAsJsonAsync($"/Pos/Customers/{customer.Id}/balance-adjustments", new { Delta = -15m, Reason = "Integration test debit" });
            debitResponse.EnsureSuccessStatusCode();
            var debited = (await debitResponse.Content.ReadFromJsonAsync<CustomerModel>())!;
            Assert.Equal(35m, debited.Balance);
        }
    }
}
