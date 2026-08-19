using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace IntegrationTests
{
    [Collection("Gateway")]
    public class CashDrawerTests
    {
        // Aisle B, Shelf 1 — a location none of the other test classes in
        // this project ever open a drawer against, so a stray "already has
        // an open session" 409 from a previous run's leftover session
        // can't leak into these tests.
        private const int ShelfB1LocationId = 3;

        private readonly GatewayFixture _fixture;

        public CashDrawerTests(GatewayFixture fixture)
        {
            _fixture = fixture;
        }

        [Fact]
        public async Task OpenDrawer_RecordCashMovements_XReportExpectedCashExcludesSalesTotal()
        {
            using var client = _fixture.CreateAdminClient();

            var session = await OpenFreshSession(client, ShelfB1LocationId, openingFloat: 100m);
            try
            {
                var cashIn = await client.PostAsJsonAsync("/Pos/CashDrawer/movements", new
                {
                    LocationId = ShelfB1LocationId,
                    Type = "CashIn",
                    Amount = 40m,
                    Reason = "Change fund top-up",
                });
                cashIn.EnsureSuccessStatusCode();

                var cashOut = await client.PostAsJsonAsync("/Pos/CashDrawer/movements", new
                {
                    LocationId = ShelfB1LocationId,
                    Type = "CashOut",
                    Amount = 25m,
                    Reason = "Petty cash",
                });
                cashOut.EnsureSuccessStatusCode();

                var report = await client.GetFromJsonAsync<CashDrawerXReportModel>($"/Pos/CashDrawer/{session.Id}/x-report");

                Assert.Equal(100m, report!.OpeningFloat);
                Assert.Equal(40m, report.CashInTotal);
                Assert.Equal(25m, report.CashOutTotal);
                // 100 + 40 - 25 = 115, deliberately excluding SalesTotal — see
                // CashDrawerXReportDto's own reasoning (no split-tender field).
                Assert.Equal(115m, report.ExpectedCashInDrawer);
            }
            finally
            {
                // Closes the session this test itself opened so a rerun
                // never collides with "location already has an open
                // session" — this is the one piece of state this black-box
                // suite can't just recreate fresh per run (unlike Sales/
                // Customers/Items, there's no way to list-then-reuse an
                // existing open session through the API alone).
                await client.PostAsJsonAsync($"/Pos/CashDrawer/{session.Id}/close", new { ClosingCount = 115m });
            }
        }

        [Fact]
        public async Task RecordCashMovement_WithNoOpenSession_ReturnsConflict()
        {
            using var client = _fixture.CreateAdminClient();
            // Aisle A, Shelf 2 — seeded but never used as a drawer location
            // by any test, so it's guaranteed to have no open session.
            const int neverOpenedLocationId = 2;

            var response = await client.PostAsJsonAsync("/Pos/CashDrawer/movements", new
            {
                LocationId = neverOpenedLocationId,
                Type = "CashIn",
                Amount = 10m,
                Reason = "Should fail",
            });

            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        }

        private static async Task<CashDrawerSessionModel> OpenFreshSession(HttpClient client, int locationId, decimal openingFloat)
        {
            var openResponse = await client.PostAsJsonAsync("/Pos/CashDrawer/open", new { LocationId = locationId, OpeningFloat = openingFloat });
            if (openResponse.StatusCode == HttpStatusCode.Conflict)
            {
                // A previous run left a session open at this location —
                // close it via the existing X-report's SessionId isn't
                // exposed by the conflict body, so the simplest robust
                // recovery is to read the current report through whatever
                // session GetOpenSession already knows about is not
                // reachable from here; fail loudly instead of guessing.
                throw new InvalidOperationException(
                    $"Location {locationId} already has an open cash drawer session from a previous run — close it before re-running this test.");
            }

            openResponse.EnsureSuccessStatusCode();
            return (await openResponse.Content.ReadFromJsonAsync<CashDrawerSessionModel>())!;
        }
    }
}
