using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Aion.DataEngine.Dynamic
{
    /// <summary>
    /// A DbContext that uses a <see cref="Metamodel"/> to dynamically configure
    /// entities and fields.  When the <see cref="Metamodel.Version"/> changes,
    /// EF Core will rebuild the model (with a custom IModelCacheKeyFactory).
    /// </summary>
    public class DynamicDbContext : DbContext
    {
        /// <summary>
        /// Gets the metamodel containing dynamic entity definitions.
        /// </summary>
        public Metamodel Metamodel { get; }

        /// <summary>
        /// Initializes a new instance of <see cref="DynamicDbContext"/> with
        /// the specified options and metamodel.
        /// </summary>
        /// <param name="options">The DbContext options.</param>
        /// <param name="metamodel">The dynamic metamodel.</param>
        public DynamicDbContext(DbContextOptions options, Metamodel metamodel)
            : base(options)
        {
            Metamodel = metamodel;
        }

        /// <inheritdoc />
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            base.OnConfiguring(optionsBuilder);
            // Replace the default model cache key factory so that EF Core
            // rebuilds the model when the metamodel version changes.  This
            // configuration is necessary when using dynamic entities and
            // fields.  Without this call, EF Core would reuse a cached
            // model and ignore changes in the metamodel.
            optionsBuilder.ReplaceService<IModelCacheKeyFactory, DynamicModelCacheKeyFactory>();
        }
        /// <inheritdoc />
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            Metamodel.ApplyChanges(modelBuilder);
        }
    }
}