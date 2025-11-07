using Aion.Domain.AI;
using Aion.Infrastructure.Configurations.Common;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aion.Infrastructure.Configurations.Ai;

public sealed class SXTemplateConfiguration : BaseEntityConfiguration<SXTemplate>
{
    protected override void ConfigureEntity(EntityTypeBuilder<SXTemplate> builder)
    {
        builder.ToTable("SXTemplate");
        builder.Property(x => x.TemplateKey).HasMaxLength(100).IsRequired();
        builder.Property(x => x.TemplateType).HasMaxLength(50).IsRequired();
        builder.Property(x => x.Content).IsRequired();
        builder.HasIndex(x => x.TemplateKey).IsUnique();
    }
}
