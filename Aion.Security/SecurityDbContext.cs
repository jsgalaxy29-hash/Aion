using Microsoft.EntityFrameworkCore;
using Aion.DataEngine.Entities;

namespace Aion.Security
{
    /// <summary>
    /// Contexte de base de données pour le module de sécurité Aion.
    /// Gère les utilisateurs, groupes et droits (RBAC).
    /// </summary>
    public class SecurityDbContext : DbContext
    {
        public SecurityDbContext(DbContextOptions<SecurityDbContext> options) : base(options) { }

        public DbSet<SUser> SUser => Set<SUser>();
        public DbSet<SGroup> SGroup => Set<SGroup>();
        public DbSet<SUserGroup> SUserGroup => Set<SUserGroup>();
        public DbSet<SRightType> SRightType => Set<SRightType>();
        public DbSet<SRight> SRight => Set<SRight>();

        protected override void OnModelCreating(ModelBuilder b)
        {
            base.OnModelCreating(b);

            // Configuration SUser
            b.Entity<SUser>(e =>
            {
                e.ToTable("SUser");
                e.HasIndex(x => x.NormalizedUserName).IsUnique();
                e.HasIndex(x => x.NormalizedEmail);
                e.HasIndex(x => new { x.TenantId, x.Deleted });
            });

            // Configuration SGroup
            b.Entity<SGroup>(e =>
            {
                e.ToTable("SGroup");
                e.HasIndex(x => new { x.Name, x.TenantId }).IsUnique();
            });

            // Configuration SUserGroup
            b.Entity<SUserGroup>(e =>
            {
                e.ToTable("SUserGroup");
                e.HasIndex(x => new { x.UserId, x.GroupId, x.TenantId }).IsUnique();

                e.HasOne(ug => ug.User)
                    .WithMany(u => u.UserGroups)
                    .HasForeignKey(ug => ug.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                e.HasOne(ug => ug.Group)
                    .WithMany(g => g.UserGroups)
                    .HasForeignKey(ug => ug.GroupId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Configuration SRightType
            b.Entity<SRightType>(e =>
            {
                e.ToTable("SRightType");
                e.HasIndex(x => new { x.Code, x.TenantId }).IsUnique();
            });

            // Configuration SRight
            b.Entity<SRight>(e =>
            {
                e.ToTable("SRight");
                e.HasIndex(x => new { x.GroupId, x.Target, x.SubjectId, x.TenantId }).IsUnique();

                e.HasOne(r => r.Group)
                    .WithMany(g => g.Rights)
                    .HasForeignKey(r => r.GroupId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}