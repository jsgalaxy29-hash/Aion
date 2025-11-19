using Aion.Domain.ModuleBuilder;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aion.Infrastructure.Configurations.ModuleBuilder;

public class AiTableBlueprintConfiguration : IEntityTypeConfiguration<AiTableBlueprint>
{
    public void Configure(EntityTypeBuilder<AiTableBlueprint> builder)
    {
        builder.ToTable("AiTableBlueprint");
        builder.Property(x => x.TechnicalName).HasMaxLength(256).IsRequired();
        builder.Property(x => x.DisplayName).HasMaxLength(256).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(1024);

        builder.HasMany(x => x.Fields)
            .WithOne(x => x.TableBlueprint)
            .HasForeignKey(x => x.TableBlueprintId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
