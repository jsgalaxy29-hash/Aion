using Aion.Domain.Security;
using Aion.Domain.UI;
using Aion.Domain.UI.State;
using Aion.Domain.Widgets;
using Microsoft.EntityFrameworkCore;

namespace Aion.Infrastructure
{
    /// <summary>
    /// DbContext principal de l'application Aion. Mappe les entités vers les tables et configure les clés et index.
    /// Contient un seed minimal pour le type de droit "Menu".
    /// </summary>
    public sealed class AionDbContext : DbContext
    {
        public DbSet<ModuleEntity> S_Module => Set<ModuleEntity>();
        public DbSet<MenuEntity> S_Menu => Set<MenuEntity>();
        public DbSet<GroupeEntity> S_Groupe => Set<GroupeEntity>();
        public DbSet<GroupeUserEntity> S_Groupe_User => Set<GroupeUserEntity>();
        public DbSet<DroitTypeEntity> S_Droit_Type => Set<DroitTypeEntity>();
        public DbSet<DroitEntity> S_Droit => Set<DroitEntity>();
        public DbSet<UserTabEntity> U_UserTab => Set<UserTabEntity>();
        public DbSet<UserDashboardLayoutEntity> U_UserDashboardLayout => Set<UserDashboardLayoutEntity>();
        public DbSet<WidgetEntity> S_Widget => Set<WidgetEntity>();

        public AionDbContext(DbContextOptions<AionDbContext> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder b)
        {
            // Tables et clés
            b.Entity<ModuleEntity>().ToTable("S_Module").HasKey(x => x.Id);
            b.Entity<MenuEntity>().ToTable("S_Menu").HasKey(x => x.Id);
            b.Entity<GroupeEntity>().ToTable("S_Groupe").HasKey(x => x.Id);
            b.Entity<GroupeUserEntity>().ToTable("S_Groupe_User").HasKey(x => new { x.GroupeId, x.UserId, x.TenantId });
            b.Entity<DroitTypeEntity>().ToTable("S_Droit_Type").HasKey(x => x.Id);
            b.Entity<DroitEntity>().ToTable("S_Droit").HasKey(x => x.Id);
            b.Entity<UserTabEntity>().ToTable("U_UserTab").HasKey(x => x.Id);
            b.Entity<UserDashboardLayoutEntity>().ToTable("U_UserDashboardLayout").HasKey(x => x.Id);
            b.Entity<WidgetEntity>().ToTable("S_Widget").HasKey(x => x.Id);

            // Index
            b.Entity<MenuEntity>().HasIndex(x => new { x.TenantId, x.ModuleId, x.ParentId, x.Order });
            b.Entity<DroitEntity>().HasIndex(x => new { x.TenantId, x.GroupeId, x.DroitTypeId, x.TargetId });
            b.Entity<GroupeUserEntity>().HasIndex(x => new { x.TenantId, x.UserId });

            // Defaults
            b.Entity<ModuleEntity>().Property(x => x.IsEnabled).HasDefaultValue(true);
            b.Entity<MenuEntity>().Property(x => x.IsLeaf).HasDefaultValue(true);
            b.Entity<DroitTypeEntity>().Property(x => x.DroitCount).HasDefaultValue(5);

            // Seed initial: type de droit Menu avec Droit1 = Autorisé
            b.Entity<DroitTypeEntity>().HasData(new DroitTypeEntity
            {
                Id = 1,
                TenantId = Guid.Empty,
                Code = "Menu",
                Libelle = "Droits sur menus",
                SourceObject = "S_Menu",
                DroitCount = 1,
                Droit1Libelle = "Autorisé"
            });
        }
    }
}
