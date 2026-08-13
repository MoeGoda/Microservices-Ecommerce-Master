using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;

namespace Warehouse.Application.Features.MasterData
{
    // Extracted the moment a THIRD caller (GetCategories/GetLocations/
    // GetUnitsOfMeasure all needing the identical "check cache, miss,
    // query, populate" shape) needed it — same reasoning as
    // StockAdjustmentStager getting pulled out for a SECOND caller.
    // IDistributedCache is a framework ABSTRACTION (Microsoft.Extensions.Caching.Abstractions),
    // not a concrete Infrastructure detail — the same category as
    // ILogger<T>, which Application-layer handlers elsewhere in this
    // project (e.g. Notifications' IngestStockLevelChangedCommandHandler)
    // already inject directly. The concrete Redis implementation is
    // registered in Warehouse.Infrastructure; this class, like
    // StockAdjustmentStager, has no idea Redis exists.
    //
    // A TTL exists here purely as a safety net, not because this data is
    // expected to change: Category/Location/UnitOfMeasure are seeded
    // reference data with no create/update command anywhere in this
    // system (confirmed before adding this cache — grepping for a
    // mutating command against any of the three came back empty). If
    // that ever changes, whoever adds the first mutating command will
    // need to also add cache invalidation here; until then, a page that's
    // up to 10 minutes stale is indistinguishable from a fresh one,
    // because the underlying data never actually moves.
    public class MasterDataCache
    {
        private static readonly DistributedCacheEntryOptions CacheOptions = new()
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10),
        };

        private readonly IDistributedCache _cache;

        public MasterDataCache(IDistributedCache cache)
        {
            _cache = cache;
        }

        public async Task<IReadOnlyList<T>> GetOrSetAsync<T>(string key, Func<Task<IEnumerable<T>>> factory, CancellationToken cancellationToken)
        {
            var cached = await _cache.GetStringAsync(key, cancellationToken);
            if (cached is not null)
            {
                var deserialized = JsonSerializer.Deserialize<List<T>>(cached);
                if (deserialized is not null)
                {
                    return deserialized;
                }
            }

            var value = (await factory()).ToList();
            await _cache.SetStringAsync(key, JsonSerializer.Serialize(value), CacheOptions, cancellationToken);
            return value;
        }
    }
}
