using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Aion.DataEngine.Interfaces;

namespace Aion.DataEngine.Services
{
    /// <summary>
    /// In‑memory implementation of <see cref="ICacheService"/>.
    /// Uses <see cref="MemoryCache"/> to store values.  This implementation
    /// is appropriate for single‑instance applications or as a local cache in
    /// distributed environments.  For multi‑server deployments, a distributed
    /// cache implementation should be provided instead.
    /// </summary>
    public class MemoryCacheService : ICacheService
    {
        private readonly MemoryCache _cache = new(new MemoryCacheOptions());

        /// <inheritdoc />
        public Task<T?> GetAsync<T>(string key)
        {
            return Task.FromResult(_cache.TryGetValue(key, out var value) ? (T?)value : default);
        }

        /// <inheritdoc />
        public Task SetAsync<T>(string key, T value, TimeSpan expiration)
        {
            _cache.Set(key, value, expiration);
            return Task.CompletedTask;
        }
    }
}