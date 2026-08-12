using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using POS.Application.Contracts.Infrastructure;
using POS.Application.Exceptions;

namespace POS.Infrastructure.Http
{
    public class WarehouseCatalogClient : IWarehouseCatalogClient
    {
        // System.Text.Json is case-sensitive by default; ASP.NET Core
        // serializes camelCase (Warehouse.Application's DTOs, B2). Explicit
        // rather than relying on GetFromJsonAsync's own default, so this
        // doesn't quietly depend on a convenience method's undocumented behavior.
        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

        private readonly HttpClient _httpClient;

        public WarehouseCatalogClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<WarehouseItemLookup?> ResolveBarcodeAsync(string barcode, CancellationToken cancellationToken)
        {
            using var response = await SendAsync(HttpMethod.Get, $"api/v1/Items/barcodes/{Uri.EscapeDataString(barcode)}", cancellationToken);

            // Mirrors exactly what Warehouse.API's own ItemsController does
            // with an unresolved barcode (B3): 404 means "not in the
            // catalog," an ordinary outcome this client hands back as
            // null rather than an exception.
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }

            EnsureSuccess(response, "resolving a barcode");

            var item = await response.Content.ReadFromJsonAsync<ItemLookupResponse>(JsonOptions, cancellationToken)
                ?? throw new WarehouseUnavailableException("received an empty response resolving a barcode.");

            return new WarehouseItemLookup
            {
                ItemId = item.Id,
                Sku = item.Sku,
                ItemName = item.Name,
                UnitPrice = item.UnitPrice,
            };
        }

        public async Task<int> GetAvailableQuantityAsync(int itemId, int locationId, CancellationToken cancellationToken)
        {
            using var response = await SendAsync(HttpMethod.Get, $"api/v1/Stock/{itemId}", cancellationToken);
            EnsureSuccess(response, "checking stock");

            var levels = await response.Content.ReadFromJsonAsync<List<StockLevelResponse>>(JsonOptions, cancellationToken)
                ?? new List<StockLevelResponse>();

            // No stock record for this item at this location is 0
            // available — the same as if the count had genuinely reached
            // zero, not an error condition.
            return levels.FirstOrDefault(l => l.LocationId == locationId)?.QuantityOnHand ?? 0;
        }

        private async Task<HttpResponseMessage> SendAsync(HttpMethod method, string requestUri, CancellationToken cancellationToken)
        {
            try
            {
                return await _httpClient.SendAsync(new HttpRequestMessage(method, requestUri), cancellationToken);
            }
            catch (HttpRequestException ex)
            {
                throw new WarehouseUnavailableException("could not reach Warehouse.API.", ex);
            }
            catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
            {
                throw new WarehouseUnavailableException("the request to Warehouse.API timed out.", ex);
            }
        }

        private static void EnsureSuccess(HttpResponseMessage response, string action)
        {
            if (!response.IsSuccessStatusCode)
            {
                throw new WarehouseUnavailableException($"received {(int)response.StatusCode} {response.StatusCode} while {action}.");
            }
        }

        // Just the fields this client actually reads from
        // Warehouse.Application.Models.ItemDetailDto (B2) — no reason to
        // mirror its Barcodes/Units/Variants collections here too.
        private class ItemLookupResponse
        {
            public int Id { get; set; }
            public string Sku { get; set; } = null!;
            public string Name { get; set; } = null!;
            public decimal UnitPrice { get; set; }
        }

        // Just the fields this client actually reads from
        // Warehouse.Application.Models.StockLevelDto (B2).
        private class StockLevelResponse
        {
            public int LocationId { get; set; }
            public int QuantityOnHand { get; set; }
        }
    }
}
