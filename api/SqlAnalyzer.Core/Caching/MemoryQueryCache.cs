using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Runtime.Caching;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SqlAnalyzer.Core.Connections;

namespace SqlAnalyzer.Core.Caching
{
    /// <summary>
    /// In-memory implementation of query cache
    /// </summary>
    public class MemoryQueryCache : IQueryCache, IDisposable
    {
        private readonly ILogger<MemoryQueryCache> _logger;
        private readonly MemoryCache _cache;
        private readonly ConcurrentDictionary<string, CacheEntryMetadata> _metadata;
        private readonly CacheStatistics _statistics;
        private readonly ReaderWriterLockSlim _lock;
        private readonly Timer _cleanupTimer;
        private bool _disposed;
        
        // Atomic counters for thread-safe operations
        private long _totalHits;
        private long _totalMisses;
        private long _totalEvictions;

        public MemoryQueryCache(ILogger<MemoryQueryCache> logger, string name = "SqlAnalyzerCache")
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _cache = new MemoryCache(name);
            _metadata = new ConcurrentDictionary<string, CacheEntryMetadata>();
            _statistics = new CacheStatistics { CreatedAt = DateTime.UtcNow };
            _lock = new ReaderWriterLockSlim();
            
            // Set up periodic cleanup
            _cleanupTimer = new Timer(CleanupExpiredEntries, null, 
                TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5));
        }

        public Task<CacheResult<T>> GetAsync<T>(string key) where T : class
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Cache key cannot be null or empty", nameof(key));

            _lock.EnterReadLock();
            try
            {
                var item = _cache.Get(key);
                if (item != null)
                {
                    Interlocked.Increment(ref _totalHits);
                    _statistics.TotalHits = _totalHits;
                    
                    if (_metadata.TryGetValue(key, out var metadata))
                    {
                        metadata.HitCount++;
                        metadata.LastAccessedAt = DateTime.UtcNow;
                        
                        // Handle sliding expiration
                        if (metadata.SlidingExpiration.HasValue)
                        {
                            _cache.Set(key, item, new CacheItemPolicy
                            {
                                SlidingExpiration = metadata.SlidingExpiration.Value,
                                RemovedCallback = OnCacheEntryRemoved
                            });
                        }
                    }

                    var value = DeserializeValue<T>(item);
                    return Task.FromResult(CacheResult<T>.Hit(
                        value, 
                        metadata?.CachedAt ?? DateTime.UtcNow,
                        metadata?.ExpiresAt,
                        metadata?.HitCount ?? 1));
                }

                Interlocked.Increment(ref _totalMisses);
                _statistics.TotalMisses = _totalMisses;
                return Task.FromResult(CacheResult<T>.Miss());
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }

        public Task SetAsync<T>(string key, T value, CacheEntryOptions? options = null) where T : class
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Cache key cannot be null or empty", nameof(key));
            
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            _lock.EnterWriteLock();
            try
            {
                var policy = new CacheItemPolicy();
                var metadata = new CacheEntryMetadata
                {
                    Key = key,
                    CachedAt = DateTime.UtcNow,
                    Priority = options?.Priority ?? CachePriority.Normal,
                    Tags = options?.Tags,
                    Size = options?.Size
                };

                // Set expiration
                if (options?.AbsoluteExpiration.HasValue == true)
                {
                    policy.AbsoluteExpiration = options.AbsoluteExpiration.Value;
                    metadata.ExpiresAt = options.AbsoluteExpiration.Value;
                }
                else if (options?.SlidingExpiration.HasValue == true)
                {
                    policy.SlidingExpiration = options.SlidingExpiration.Value;
                    metadata.SlidingExpiration = options.SlidingExpiration.Value;
                    metadata.ExpiresAt = DateTime.UtcNow.Add(options.SlidingExpiration.Value);
                }

                // Set priority
                policy.Priority = ConvertPriority(options?.Priority ?? CachePriority.Normal);
                
                // Set callback
                if (options?.EvictionCallback != null)
                {
                    metadata.EvictionCallback = options.EvictionCallback;
                }
                policy.RemovedCallback = OnCacheEntryRemoved;

                var serializedValue = SerializeValue(value);
                _cache.Set(key, serializedValue, policy);
                _metadata[key] = metadata;

                _logger.LogDebug("Cached entry with key: {Key}", key);
                return Task.CompletedTask;
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        public Task RemoveAsync(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                return Task.CompletedTask;

            _lock.EnterWriteLock();
            try
            {
                _cache.Remove(key);
                _metadata.TryRemove(key, out _);
                _logger.LogDebug("Removed cache entry with key: {Key}", key);
                return Task.CompletedTask;
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        public Task ClearAsync()
        {
            _lock.EnterWriteLock();
            try
            {
                // Get all keys before clearing
                var keys = _metadata.Keys.ToList();
                
                // Clear cache
                foreach (var key in keys)
                {
                    _cache.Remove(key);
                }
                
                _metadata.Clear();
                _statistics.LastClearedAt = DateTime.UtcNow;
                
                _logger.LogInformation("Cache cleared. Removed {Count} entries", keys.Count);
                return Task.CompletedTask;
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        public CacheStatistics GetStatistics()
        {
            _lock.EnterReadLock();
            try
            {
                _statistics.CurrentEntryCount = _metadata.Count;
                _statistics.CurrentMemoryUsage = _metadata.Values
                    .Where(m => m.Size.HasValue)
                    .Sum(m => m.Size!.Value);
                
                return new CacheStatistics
                {
                    TotalHits = _statistics.TotalHits,
                    TotalMisses = _statistics.TotalMisses,
                    CurrentEntryCount = _statistics.CurrentEntryCount,
                    CurrentMemoryUsage = _statistics.CurrentMemoryUsage,
                    TotalEvictions = _statistics.TotalEvictions,
                    CreatedAt = _statistics.CreatedAt,
                    LastClearedAt = _statistics.LastClearedAt
                };
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }

        public Task<bool> ExistsAsync(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                return Task.FromResult(false);

            _lock.EnterReadLock();
            try
            {
                return Task.FromResult(_cache.Contains(key));
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }

        public async Task<T> GetOrAddAsync<T>(string key, Func<Task<T>> factory, CacheEntryOptions? options = null) where T : class
        {
            var result = await GetAsync<T>(key);
            if (result.IsHit && result.Value != null)
            {
                return result.Value;
            }

            // Not in cache, execute factory
            var value = await factory();
            
            // Cache the result
            await SetAsync(key, value, options);
            
            return value;
        }

        private T DeserializeValue<T>(object cachedValue) where T : class
        {
            if (cachedValue is T typedValue)
                return typedValue;

            if (cachedValue is string json)
                return JsonSerializer.Deserialize<T>(json) ?? throw new InvalidOperationException("Failed to deserialize cached value");

            throw new InvalidCastException($"Cannot convert cached value to type {typeof(T).Name}");
        }

        private object SerializeValue<T>(T value) where T : class
        {
            // For simple types, store directly
            if (value is string || value.GetType().IsPrimitive)
                return value;

            // For complex types, serialize to JSON
            return JsonSerializer.Serialize(value);
        }

        private CacheItemPriority ConvertPriority(CachePriority priority)
        {
            return priority switch
            {
                CachePriority.Low => CacheItemPriority.Default,
                CachePriority.Normal => CacheItemPriority.Default,
                CachePriority.High => CacheItemPriority.NotRemovable,
                CachePriority.NeverRemove => CacheItemPriority.NotRemovable,
                _ => CacheItemPriority.Default
            };
        }

        private void OnCacheEntryRemoved(CacheEntryRemovedArguments args)
        {
            if (_metadata.TryRemove(args.CacheItem.Key, out var metadata))
            {
                Interlocked.Increment(ref _totalEvictions);
                _statistics.TotalEvictions = _totalEvictions;
                
                var reason = ConvertRemovedReason(args.RemovedReason);
                metadata.EvictionCallback?.Invoke(reason);
                
                _logger.LogDebug("Cache entry removed. Key: {Key}, Reason: {Reason}", 
                    args.CacheItem.Key, reason);
            }
        }

        private CacheEvictionReason ConvertRemovedReason(CacheEntryRemovedReason reason)
        {
            return reason switch
            {
                CacheEntryRemovedReason.Expired => CacheEvictionReason.Expired,
                CacheEntryRemovedReason.Evicted => CacheEvictionReason.MemoryPressure,
                CacheEntryRemovedReason.Removed => CacheEvictionReason.Removed,
                CacheEntryRemovedReason.ChangeMonitorChanged => CacheEvictionReason.Replaced,
                _ => CacheEvictionReason.None
            };
        }

        private void CleanupExpiredEntries(object? state)
        {
            if (_disposed) return;

            _lock.EnterWriteLock();
            try
            {
                var expiredKeys = _metadata
                    .Where(kvp => kvp.Value.ExpiresAt.HasValue && kvp.Value.ExpiresAt.Value < DateTime.UtcNow)
                    .Select(kvp => kvp.Key)
                    .ToList();

                foreach (var key in expiredKeys)
                {
                    _cache.Remove(key);
                    _metadata.TryRemove(key, out _);
                }

                if (expiredKeys.Count > 0)
                {
                    _logger.LogDebug("Cleaned up {Count} expired cache entries", expiredKeys.Count);
                }
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        public void Dispose()
        {
            if (_disposed) return;

            _disposed = true;
            _cleanupTimer?.Dispose();
            _cache?.Dispose();
            _lock?.Dispose();
        }

        private class CacheEntryMetadata
        {
            public string Key { get; set; } = string.Empty;
            public DateTime CachedAt { get; set; }
            public DateTime? ExpiresAt { get; set; }
            public TimeSpan? SlidingExpiration { get; set; }
            public DateTime LastAccessedAt { get; set; }
            public int HitCount { get; set; }
            public CachePriority Priority { get; set; }
            public string[]? Tags { get; set; }
            public long? Size { get; set; }
            public Action<CacheEvictionReason>? EvictionCallback { get; set; }
        }
    }

    /// <summary>
    /// Default implementation of cache key generator
    /// </summary>
    public class DefaultCacheKeyGenerator : ICacheKeyGenerator
    {
        public string GenerateKey(string query, params object[] parameters)
        {
            var baseKey = query.GetHashCode().ToString();
            
            if (parameters != null && parameters.Length > 0)
            {
                var paramHash = string.Join("_", parameters.Select(p => p?.GetHashCode() ?? 0));
                return $"{baseKey}_{paramHash}";
            }

            return baseKey;
        }

        public string GenerateKey(DatabaseType databaseType, string databaseName, string objectType, string objectName)
        {
            return $"{databaseType}_{databaseName}_{objectType}_{objectName}".ToLowerInvariant();
        }
    }
}