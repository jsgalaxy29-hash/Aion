using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Aion.DataEngine.Dynamic
{
    /// <summary>
    /// Provides a custom cache key for EF Core models when using dynamic
    /// metamodels.  EF Core caches the model for a given context type; this
    /// implementation adds the metamodel version to the cache key so that
    /// whenever the version changes the model will be rebuilt.  Without
    /// this factory EF Core would reuse the cached model and ignore new
    /// dynamic entities or fields.  See https://www.thinktecture.com for
    /// details.
    /// </summary>
    public class DynamicModelCacheKeyFactory : IModelCacheKeyFactory
    {
        /// <inheritdoc />
        public object Create(DbContext context, bool designTime)
        {
            if (context is DynamicDbContext dynamicContext)
            {
                var version = dynamicContext.Metamodel?.Version ?? 0;
                // Include designTime in the key to differentiate design-time vs run-time models
                return (context.GetType(), version, designTime);
            }
            // Fall back to default key (context type + design time)
            return (context.GetType(), designTime);
        }
    }
}