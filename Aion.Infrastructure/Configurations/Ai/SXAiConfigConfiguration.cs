using Aion.Domain.AI;
using Aion.Infrastructure.Configurations.Common;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aion.Infrastructure.Configurations.Ai;

public sealed class SXAiConfigConfiguration : BaseEntityConfiguration<SXAiConfig>
{
    protected override void ConfigureEntity(EntityTypeBuilder<SXAiConfig> builder)
    {
        builder.ToTable("SXAiConfig");
        builder.Property(x => x.Provider).HasMaxLength(50).IsRequired();
        builder.Property(x => x.ApiKey).HasMaxLength(200);
        builder.Property(x => x.BaseUrl).HasMaxLength(200);
        builder.Property(x => x.ModelName).HasMaxLength(100).IsRequired();
    }
}
