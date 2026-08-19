using System.Net.Http.Headers;
using System.Net.Http.Json;
using Xunit;

namespace IntegrationTests
{
    // Every test in this project is a black-box HTTP call through the real
    // Ocelot gateway into the real, already-running docker-compose stack —
    // there is no in-memory TestServer here, unlike the *.Application.Tests
    // projects. GATEWAY_BASE_URL lets CI point this at a different host;
    // the default matches every earlier phase's own documented local port.
    public class GatewayFixture : IAsyncLifetime
    {
        public static readonly string BaseUrl = Environment.GetEnvironmentVariable("GATEWAY_BASE_URL") ?? "http://localhost:5058";

        public string AdminToken { get; private set; } = null!;

        // Seeded Cola (WarehouseContextSeed): barcode below, 50 on hand at
        // shelf A1 (LocationId 1) the FIRST time this whole DB is seeded —
        // but every rerun of this suite against the same persistent stack
        // draws that pool down further via real checkouts. Topping it up
        // once per run, here, keeps every test's own "add N units" call
        // from 409-ing on insufficient stock regardless of how many prior
        // runs already ate into the original 50.
        private const string ColaBarcode = "5901234123457";
        private const int ShelfA1LocationId = 1;
        private const int PcsUnitOfMeasureId = 1;

        public async Task InitializeAsync()
        {
            using var client = new HttpClient { BaseAddress = new Uri(BaseUrl) };
            AdminToken = await Login(client, "admin", "Admin@12345");

            using var adminClient = CreateAdminClient();
            var item = await adminClient.GetFromJsonAsync<ItemDetailModel>($"/Warehouse/Items/barcodes/{ColaBarcode}");
            var topUp = await adminClient.PostAsJsonAsync("/Warehouse/Stock/receive", new
            {
                ItemId = item!.Id,
                LocationId = ShelfA1LocationId,
                Quantity = 500,
                UnitOfMeasureId = PcsUnitOfMeasureId,
                Reference = "integration-test-suite-topup",
            });
            topUp.EnsureSuccessStatusCode();
        }

        public Task DisposeAsync() => Task.CompletedTask;

        public HttpClient CreateAdminClient()
        {
            var client = new HttpClient { BaseAddress = new Uri(BaseUrl) };
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", AdminToken);
            return client;
        }

        public HttpClient CreateAnonymousClient() => new() { BaseAddress = new Uri(BaseUrl) };

        public static async Task<string> Login(HttpClient client, string userName, string password)
        {
            var response = await client.PostAsJsonAsync("/Identity/Auth/login", new { UserName = userName, Password = password });
            response.EnsureSuccessStatusCode();
            var auth = await response.Content.ReadFromJsonAsync<AuthResponseModel>();
            return auth!.Token;
        }
    }

    public record AuthResponseModel(string Token, DateTime ExpiresAtUtc, string UserName, string Role);

    [CollectionDefinition("Gateway")]
    public class GatewayCollection : ICollectionFixture<GatewayFixture>
    {
        // Every test class shares one collection so none of them run in
        // parallel against the one live stack/database this project talks
        // to — xUnit only parallelizes across collections, never within one.
    }
}
