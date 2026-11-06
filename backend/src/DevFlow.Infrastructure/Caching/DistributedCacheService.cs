using System.Text.Json;
using DevFlow.Application.Common.Interfaces;
using Microsoft.Extensions.Caching.Distributed;

namespace DevFlow.Infrastructure.Caching;

public sealed class DistributedCacheService(IDistributedCache cache) : ICacheService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken)
    {
        var payload = await cache.GetStringAsync(key, cancellationToken);
        return payload is null ? default : JsonSerializer.Deserialize<T>(payload, JsonOptions);
    }

    public Task SetAsync<T>(string key, T value, TimeSpan lifetime, CancellationToken cancellationToken) =>
        cache.SetStringAsync(
            key,
            JsonSerializer.Serialize(value, JsonOptions),
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = lifetime },
            cancellationToken);

    public Task RemoveAsync(string key, CancellationToken cancellationToken) =>
        cache.RemoveAsync(key, cancellationToken);
}
