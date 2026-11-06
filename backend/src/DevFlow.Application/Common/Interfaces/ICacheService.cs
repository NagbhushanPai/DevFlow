namespace DevFlow.Application.Common.Interfaces;

public interface ICacheService
{
    Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken);
    Task SetAsync<T>(string key, T value, TimeSpan lifetime, CancellationToken cancellationToken);
    Task RemoveAsync(string key, CancellationToken cancellationToken);
}
