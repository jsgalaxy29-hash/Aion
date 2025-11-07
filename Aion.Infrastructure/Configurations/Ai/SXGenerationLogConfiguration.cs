using Aion.Domain.AI;
using Aion.Infrastructure.Configurations.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aion.Infrastructure.Configurations.Ai;

public sealed class SXGenerationLogConfiguration : BaseEntityConfiguration<SXGenerationLog>
{
    protected override void ConfigureEntity(EntityTypeBuilder<SXGenerationLog> builder)
    {
        builder.ToTable("SXGenerationLog");
        builder.Property(x => x.RequestText).IsRequired();
        builder.Property(x => x.Status).HasMaxLength(32).IsRequired();
        builder.Property(x => x.ModelVersion).HasMaxLength(64).IsRequired();
    }
}
