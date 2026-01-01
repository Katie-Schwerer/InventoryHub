using ClientApp.Services;

namespace ClientApp.Services;

/// <summary>
/// Generic caching service with expiration support
/// </summary>
/// <typeparam name="T">Type of data to cache</typeparam>
public class CacheService<T>
{
    private T? _cachedData;
    private DateTime? _cacheTimestamp;
    private readonly TimeSpan _cacheExpiration;

    public CacheService(TimeSpan cacheExpiration)
    {
        _cacheExpiration = cacheExpiration;
    }

    /// <summary>
    /// Gets cached data if available and valid
    /// </summary>
    public T? GetCachedData()
    {
        if (IsValid())
        {
            return _cachedData;
        }
        return default;
    }

    /// <summary>
    /// Updates the cache with new data
    /// </summary>
    public void SetCachedData(T data)
    {
        _cachedData = data;
        _cacheTimestamp = DateTime.UtcNow;
    }

    /// <summary>
    /// Clears the cache
    /// </summary>
    public void Clear()
    {
        _cachedData = default;
        _cacheTimestamp = null;
    }

    /// <summary>
    /// Checks if cached data is still valid
    /// </summary>
    public bool IsValid()
    {
        return _cachedData != null &&
               _cacheTimestamp.HasValue &&
               (DateTime.UtcNow - _cacheTimestamp.Value) < _cacheExpiration;
    }
}
