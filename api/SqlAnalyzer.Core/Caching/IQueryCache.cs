using System;
using System.Threading.Tasks;
using SqlAnalyzer.Core.Connections;

namespace SqlAnalyzer.Core.Caching
{
    /// <summary>
    /// Interface for caching query results to improve performance
    /// </summary>
    public interface IQueryCache
    {
        /// <summary>
        /// Gets a cached query result if available
        /// </summary>
        Task<CacheResult<T>> GetAsync<T>(string key) where T : class;

        /// <summary>
        /// Sets a query result in the cache
        /// </summary>
        Task SetAsync<T>(string key, T value, CacheEntryOptions? options = null) where T : class;

        /// <summary>
        /// Removes a cached query result
        /// </summary>
        Task RemoveAsync(string key);

        /// <summary>
        /// Clears all cached entries
        /// </summary>
        Task ClearAsync();

        /// <summary>
        /// Gets cache statistics
        /// </summary>
        CacheStatistics GetStatistics();

        /// <summary>
        /// Checks if a key exists in the cache
        /// </summary>
        Task<bool> ExistsAsync(string key);

        /// <summary>
        /// Gets or adds a value to the cache
        /// </summary>
        Task<T> GetOrAddAsync<T>(string key, Func<Task<T>> factory, CacheEntryOptions? options = null) where T : class;
    }

    /// <summary>
    /// Result of a cache lookup
    /// </summary>
    public class CacheResult<T> where T : class
    {
        /// <summary>
        /// Whether the value was found in cache
        /// </summary>
        public bool IsHit { get; set; }

        /// <summary>
        /// The cached value (null if miss)
        /// </summary>
        public T? Value { get; set; }

        /// <summary>
        /// When the entry was cached
        /// </summary>
        public DateTime? CachedAt { get; set; }

        /// <summary>
        /// When the entry expires
        /// </summary>
        public DateTime? ExpiresAt { get; set; }

        /// <summary>
        /// Number of times this entry has been accessed
        /// </summary>
        public int HitCount { get; set; }

        /// <summary>
        /// Creates a cache hit result
        /// </summary>
        public static CacheResult<T> Hit(T value, DateTime cachedAt, DateTime? expiresAt = null, int hitCount = 1)
        {
            return new CacheResult<T>
            {
                IsHit = true,
                Value = value,
                CachedAt = cachedAt,
                ExpiresAt = expiresAt,
                HitCount = hitCount
            };
        }

        /// <summary>
        /// Creates a cache miss result
        /// </summary>
        public static CacheResult<T> Miss()
        {
            return new CacheResult<T>
            {
                IsHit = false
            };
        }
    }

    /// <summary>
    /// Options for cache entries
    /// </summary>
    public class CacheEntryOptions
    {
        /// <summary>
        /// Absolute expiration time
        /// </summary>
        public DateTime? AbsoluteExpiration { get; set; }

        /// <summary>
        /// Sliding expiration time
        /// </summary>
        public TimeSpan? SlidingExpiration { get; set; }

        /// <summary>
        /// Priority for cache eviction
        /// </summary>
        public CachePriority Priority { get; set; } = CachePriority.Normal;

        /// <summary>
        /// Size of the cache entry (for memory limits)
        /// </summary>
        public long? Size { get; set; }

        /// <summary>
        /// Tags associated with the cache entry
        /// </summary>
        public string[]? Tags { get; set; }

        /// <summary>
        /// Callback when entry is evicted
        /// </summary>
        public Action<CacheEvictionReason>? EvictionCallback { get; set; }
    }

    /// <summary>
    /// Cache entry priority
    /// </summary>
    public enum CachePriority
    {
        Low,
        Normal,
        High,
        NeverRemove
    }

    /// <summary>
    /// Reasons for cache eviction
    /// </summary>
    public enum CacheEvictionReason
    {
        None,
        Expired,
        MemoryPressure,
        Replaced,
        Removed,
        Capacity
    }

    /// <summary>
    /// Statistics about cache usage
    /// </summary>
    public class CacheStatistics
    {
        /// <summary>
        /// Total number of cache hits
        /// </summary>
        public long TotalHits { get; set; }

        /// <summary>
        /// Total number of cache misses
        /// </summary>
        public long TotalMisses { get; set; }

        /// <summary>
        /// Current number of entries in cache
        /// </summary>
        public long CurrentEntryCount { get; set; }

        /// <summary>
        /// Total memory used by cache (if available)
        /// </summary>
        public long? CurrentMemoryUsage { get; set; }

        /// <summary>
        /// Number of evictions
        /// </summary>
        public long TotalEvictions { get; set; }

        /// <summary>
        /// Cache hit ratio
        /// </summary>
        public double HitRatio => TotalHits + TotalMisses > 0 
            ? (double)TotalHits / (TotalHits + TotalMisses) * 100 
            : 0;

        /// <summary>
        /// Average entry size (if available)
        /// </summary>
        public long? AverageEntrySize => CurrentEntryCount > 0 && CurrentMemoryUsage.HasValue
            ? CurrentMemoryUsage.Value / CurrentEntryCount
            : null;

        /// <summary>
        /// Time cache was created
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Last time cache was cleared
        /// </summary>
        public DateTime? LastClearedAt { get; set; }
    }

    /// <summary>
    /// Interface for generating cache keys
    /// </summary>
    public interface ICacheKeyGenerator
    {
        /// <summary>
        /// Generates a cache key for a query
        /// </summary>
        string GenerateKey(string query, params object[] parameters);

        /// <summary>
        /// Generates a cache key for a database object
        /// </summary>
        string GenerateKey(DatabaseType databaseType, string databaseName, string objectType, string objectName);
    }
}