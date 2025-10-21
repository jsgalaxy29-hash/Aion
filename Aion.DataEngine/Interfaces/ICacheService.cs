using System;
using System.Threading.Tasks;

namespace Aion.DataEngine.Interfaces
{
    /// <summary>
    /// Simple cache abstraction used by the data engine to reduce database
    /// round‑trips for frequently accessed data (e.g. metadata).  An
    /// implementation may be in‑memory or distributed (e.g. Redis).
    /// </summary>
    public interface ICacheService
    {
        /// <summary>
        /// Attempts to retrieve a value from cache.
        /// </summary>
        /// <typeparam name="T">Type of the cached value.</typeparam>
        /// <param name="key">Cache key.</param>
        /// <returns>The cached value if present; otherwise null.</returns>
        Task<T?> GetAsync<T>(string key);

        /// <summary>
        /// Stores a value in the cache for the given duration.
        /// </summary>
        /// <typeparam name="T">Type of the value.</typeparam>
        /// <param name="key">Cache key.</param>
        /// <param name="value">Value to store.</param>
        /// <param name="expiration">Absolute expiration relative to now.</param>
        Task SetAsync<T>(string key, T value, TimeSpan expiration);
    }
}