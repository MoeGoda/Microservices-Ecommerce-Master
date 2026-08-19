using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace IntegrationTests
{
    [Collection("Gateway")]
    public class AuthFlowTests
    {
        private readonly GatewayFixture _fixture;

        public AuthFlowTests(GatewayFixture fixture)
        {
            _fixture = fixture;
        }

        [Fact]
        public async Task Login_WithSeededAdminCredentials_ReturnsAdminRoleAndToken()
        {
            using var client = _fixture.CreateAnonymousClient();
            var response = await client.PostAsJsonAsync("/Identity/Auth/login", new { UserName = "admin", Password = "Admin@12345" });

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var auth = await response.Content.ReadFromJsonAsync<AuthResponseModel>();
            Assert.False(string.IsNullOrWhiteSpace(auth!.Token));
            Assert.Equal("Admin", auth.Role);
        }

        [Fact]
        public async Task Login_WithWrongPassword_ReturnsUnauthorized()
        {
            using var client = _fixture.CreateAnonymousClient();
            var response = await client.PostAsJsonAsync("/Identity/Auth/login", new { UserName = "admin", Password = "WrongPassword!" });

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task Register_NewUser_AlwaysGetsCashierRoleRegardlessOfRequestedRole()
        {
            using var client = _fixture.CreateAnonymousClient();
            var userName = $"integration-{Guid.NewGuid():N}";
            var response = await client.PostAsJsonAsync("/Identity/Auth/register", new
            {
                UserName = userName,
                Email = $"{userName}@example.com",
                Password = "Passw0rd!123",
                FirstName = "Integration",
                LastName = "Tester",
                Role = "Admin", // anonymous registration must not be able to self-elevate (F2a)
            });

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var auth = await response.Content.ReadFromJsonAsync<AuthResponseModel>();
            Assert.Equal("Cashier", auth!.Role);
        }

        [Fact]
        public async Task Me_WithValidAdminToken_ReturnsMatchingUserName()
        {
            using var client = _fixture.CreateAdminClient();
            var response = await client.GetAsync("/Identity/Auth/me");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await response.Content.ReadAsStringAsync();
            Assert.Contains("admin", body, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task ProtectedEndpoint_WithNoToken_ReturnsUnauthorized()
        {
            using var client = _fixture.CreateAnonymousClient();
            var response = await client.GetAsync("/Warehouse/MasterData/locations");

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }
    }
}
