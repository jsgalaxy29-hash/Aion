using Aion.Domain.ModuleBuilder;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aion.Infrastructure.Configurations.ModuleBuilder;

public class AiFieldBlueprintConfiguration : IEntityTypeConfiguration<AiFieldBlueprint>
{
    public void Configure(EntityTypeBuilder<AiFieldBlueprint> builder)
    {
        builder.ToTable("AiFieldBlueprint");
        builder.Property(x => x.TechnicalName).HasMaxLength(256).IsRequired();
        builder.Property(x => x.DisplayName).HasMaxLength(256).IsRequired();
        builder.Property(x => x.DataType).HasMaxLength(128).IsRequired();
        builder.Property(x => x.ForeignKeyTargetTable).HasMaxLength(256);
        builder.Property(x => x.ForeignKeyTargetField).HasMaxLength(256);
    }
}
