using Aion.Domain.AI;
using Aion.Infrastructure.Configurations.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aion.Infrastructure.Configurations.Ai;

public sealed class SAuditRecordConfiguration : BaseEntityConfiguration<SAuditRecord>
{
    protected override void ConfigureEntity(EntityTypeBuilder<SAuditRecord> builder)
    {
        builder.ToTable("SAuditRecord");
        builder.Property(x => x.RequestText).IsRequired();
        builder.Property(x => x.Status).HasMaxLength(32).IsRequired();
        builder.Property(x => x.ModelVersion).HasMaxLength(64).IsRequired();
    }
}
