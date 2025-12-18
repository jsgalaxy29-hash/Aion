using Aion.Domain.ModuleBuilder;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aion.Infrastructure.Configurations.ModuleBuilder;

public class AiModuleBlueprintConfiguration : IEntityTypeConfiguration<AiModuleBlueprint>
{
    public void Configure(EntityTypeBuilder<AiModuleBlueprint> builder)
    {
        builder.ToTable("AiModuleBlueprint");
        builder.Property(x => x.Name).HasMaxLength(256).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(1024);
        builder.Property(x => x.NaturalLanguagePrompt).IsRequired();
        builder.Property(x => x.ParsedSpecificationJson).IsRequired();
        builder.Property(x => x.Status).HasMaxLength(64).IsRequired();

        builder.HasMany(x => x.Tables)
            .WithOne(x => x.ModuleBlueprint)
            .HasForeignKey(x => x.ModuleBlueprintId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
