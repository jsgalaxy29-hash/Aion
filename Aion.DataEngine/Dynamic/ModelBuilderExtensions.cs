using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace Aion.DataEngine.Dynamic
{
    /// <summary>
    /// Provides extension methods for applying a dynamic metamodel to an EF
    /// <see cref="ModelBuilder"/>.  Entities and their fields defined in
    /// <see cref="Metamodel"/> are registered at runtime.
    /// </summary>
    public static class ModelBuilderExtensions
    {
        /// <summary>
        /// Applies the dynamic entities and fields from the metamodel to the
        /// <see cref="ModelBuilder"/>.  Each entity is mapped to the
        /// specified table and schema, and keys and properties are
        /// configured according to the metadata.
        /// </summary>
        /// <param name="metamodel">The dynamic metamodel.</param>
        /// <param name="modelBuilder">The EF model builder.</param>
        public static void ApplyChanges(this Metamodel metamodel, ModelBuilder modelBuilder)
        {
            foreach (var entity in metamodel.Entities)
            {
                var entityBuilder = modelBuilder.Entity(entity.EntityName);
                entityBuilder.ToTable(entity.TableName, entity.TableSchema);
                // Configure key
                if (entity.Key.Count > 0)
                {
                    // Define the primary key on the entity
                    entityBuilder.HasKey(entity.Key.Select(k => k.PropertyName).ToArray());
                    // Configure each key property individually
                    foreach (var keyField in entity.Key)
                    {
                        var propertyBuilder = entityBuilder.Property(keyField.PropertyName);
                        propertyBuilder.IsRequired(keyField.IsRequired);
                        if (keyField.MaxLength.HasValue)
                        {
                            propertyBuilder.HasMaxLength(keyField.MaxLength.Value);
                        }
                    }
                }
                // Configure additional fields
                foreach (var field in entity.Fields)
                {
                    // Skip key fields
                    if (entity.Key.Any(k => k.PropertyName == field.PropertyName)) continue;
                    var propertyBuilder = entityBuilder.Property(field.PropertyName);
                    propertyBuilder.IsRequired(field.IsRequired);
                    if (field.MaxLength.HasValue)
                    {
                        propertyBuilder.HasMaxLength(field.MaxLength.Value);
                    }
                }
            }
        }
    }
}