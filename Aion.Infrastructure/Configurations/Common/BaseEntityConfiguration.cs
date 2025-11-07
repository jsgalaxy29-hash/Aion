using Aion.DataEngine.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aion.Infrastructure.Configurations.Common;

public abstract class BaseEntityConfiguration<TEntity> : IEntityTypeConfiguration<TEntity> where TEntity : BaseEntity
{
    public void Configure(EntityTypeBuilder<TEntity> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.DtCreation).HasDefaultValueSql("SYSUTCDATETIME()");
        builder.Property(x => x.DtModification).HasDefaultValueSql("SYSUTCDATETIME()");
        builder.Property(x => x.UsrCreationId).HasMaxLength(200);
        builder.Property(x => x.UsrModificationId).HasMaxLength(200);
        builder.Property(x => x.Deleted).HasDefaultValue(false);

        ConfigureEntity(builder);
    }

    protected abstract void ConfigureEntity(EntityTypeBuilder<TEntity> builder);
}
