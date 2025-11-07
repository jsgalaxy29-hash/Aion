using Aion.Domain.AI;
using Aion.Infrastructure.Configurations.Common;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aion.Infrastructure.Configurations.Ai;

public sealed class SXSynonymConfiguration : BaseEntityConfiguration<SXSynonym>
{
    protected override void ConfigureEntity(EntityTypeBuilder<SXSynonym> builder)
    {
        builder.ToTable("SXSynonym");
        builder.Property(x => x.DomainTerm).HasMaxLength(100).IsRequired();
        builder.Property(x => x.SynonymsCsv).HasMaxLength(500).IsRequired();
        builder.Property(x => x.Category).HasMaxLength(50).IsRequired();

        builder.HasIndex(x => x.DomainTerm).IsUnique();
    }
}
