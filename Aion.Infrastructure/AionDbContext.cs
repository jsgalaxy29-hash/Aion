using Microsoft.EntityFrameworkCore;
using Aion.DataEngine.Interfaces;
using System;
using System.Security.Cryptography;
using System.Text;
using Aion.DataEngine.Entities;

namespace Aion.Infrastructure
{
    public class AionDbContext(DbContextOptions<AionDbContext> options) : DbContext(options)
    {

        private readonly IUserContext _userContext;

        // DbContextOptions<AionDbContext> options, IUserContext userContext)
        //_userContext = userContext; 

        // System DbSets (unifiés)
        public DbSet<SUser> Users => Set<SUser>();
        public DbSet<SMenu> SMenu => Set<SMenu>();
        public DbSet<SModule> SModule => Set<SModule>();
        public DbSet<SAction> SAction => Set<SAction>();
        public DbSet<SReport> SReport => Set<SReport>();
        public DbSet<SUser> SUser => Set<SUser>();
        public DbSet<SGroup> SGroup => Set<SGroup>();
        public DbSet<SUserGroup> SUserGroup => Set<SUserGroup>();
        public DbSet<SRightType> SRightType => Set<SRightType>();
        public DbSet<SRight> SRight => Set<SRight>();
        public DbSet<STable> STable => Set<STable>();
        public DbSet<SField> SField => Set<SField>();
        public DbSet<RRegex> RRegex => Set<RRegex>();
        public DbSet<FDocument> FDocument => Set<FDocument>();
        public DbSet<SHistoVersion> SHistoVersion => Set<SHistoVersion>();
        public DbSet<SHistoChange> SHistoChange => Set<SHistoChange>();
        public DbSet<STenant> STenant => Set<STenant>();
        public DbSet<SWidget> SWidget => Set<SWidget>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder); 
            
            modelBuilder.Entity<SUser>(e =>
            {
                e.ToTable("SUser");
                e.HasKey(x => x.Id);
                e.Property(x => x.UserName).HasMaxLength(128).IsRequired();
                e.HasIndex(x => x.UserName).IsUnique();
                e.Property(x => x.PasswordHash).HasMaxLength(128).IsRequired();
            });

            modelBuilder.Entity<SUser>().HasData(new SUser
            {
                UserName = "admin",
                PasswordHash = Sha256("admin"),
                IsActive = true,
            });

            
            // Filtres multi-tenant basés sur IUserContext
            modelBuilder.Entity<Aion.DataEngine.Entities.SMenu>().HasQueryFilter(e => e.TenantId == _userContext.TenantId);
            modelBuilder.Entity<Aion.DataEngine.Entities.SModule>().HasQueryFilter(e => e.TenantId == _userContext.TenantId);
            modelBuilder.Entity<Aion.DataEngine.Entities.SAction>().HasQueryFilter(e => e.TenantId == _userContext.TenantId);
            modelBuilder.Entity<Aion.DataEngine.Entities.SReport>().HasQueryFilter(e => e.TenantId == _userContext.TenantId);
            modelBuilder.Entity<Aion.DataEngine.Entities.SUser>().HasQueryFilter(e => e.TenantId == _userContext.TenantId);
            modelBuilder.Entity<Aion.DataEngine.Entities.SGroup>().HasQueryFilter(e => e.TenantId == _userContext.TenantId);
            modelBuilder.Entity<Aion.DataEngine.Entities.SUserGroup>().HasQueryFilter(e => e.TenantId == _userContext.TenantId);
            modelBuilder.Entity<Aion.DataEngine.Entities.SRightType>().HasQueryFilter(e => e.TenantId == _userContext.TenantId);
            modelBuilder.Entity<Aion.DataEngine.Entities.SRight>().HasQueryFilter(e => e.TenantId == _userContext.TenantId);
            modelBuilder.Entity<Aion.DataEngine.Entities.STable>().HasQueryFilter(e => e.TenantId == _userContext.TenantId);
            modelBuilder.Entity<Aion.DataEngine.Entities.SField>().HasQueryFilter(e => e.TenantId == _userContext.TenantId);
            modelBuilder.Entity<Aion.DataEngine.Entities.RRegex>().HasQueryFilter(e => e.TenantId == _userContext.TenantId);
            modelBuilder.Entity<Aion.DataEngine.Entities.FDocument>().HasQueryFilter(e => e.TenantId == _userContext.TenantId);
            modelBuilder.Entity<Aion.DataEngine.Entities.SHistoVersion>().HasQueryFilter(e => e.TenantId == _userContext.TenantId);
            modelBuilder.Entity<Aion.DataEngine.Entities.SHistoChange>().HasQueryFilter(e => e.TenantId == _userContext.TenantId);
            modelBuilder.Entity<Aion.DataEngine.Entities.STenant>().HasQueryFilter(e => e.TenantId == _userContext.TenantId);
        }


        // ===== Méthode de seed (à externaliser dans un service dédié) =====
        /*
        async Task SeedSecurityData(SecurityDbContext db)
        {
            if (await db.SUser.AnyAsync()) return; // Déjà seedé

            // Création tenant par défaut
            var tenant = new STenant { Id = 1, Name = "Default" };

            // Création groupe admin
            var adminGroup = new SGroup 
            { 
                Name = "Administrateurs", 
                Description = "Groupe administrateur système",
                IsSystem = true,
                TenantId = 1
            };
            db.SGroup.Add(adminGroup);
            await db.SaveChangesAsync();

            // Création utilisateur admin
            var admin = new SUser
            {
                UserName = "admin",
                NormalizedUserName = "ADMIN",
                Email = "admin@aion.local",
                NormalizedEmail = "ADMIN@AION.LOCAL",
                PasswordHash = "admin", // À CHANGER avec BCrypt !
                FullName = "Administrateur",
                IsActive = true,
                TenantId = 1
            };
            db.SUser.Add(admin);
            await db.SaveChangesAsync();

            // Association admin au groupe
            db.SUserGroup.Add(new SUserGroup
            {
                UserId = admin.Id,
                GroupId = adminGroup.Id,
                IsLinkActive = true,
                TenantId = 1
            });

            // Création types de droits
            var rightTypes = new[]
            {
                new SRightType { Code = "Menu", Name = "Droits sur menus", DataSource = "SMenu", Right1Name = "Voir", TenantId = 1 },
                new SRightType { Code = "Module", Name = "Droits sur modules", DataSource = "SModule", Right1Name = "Lire", Right2Name = "Écrire", Right3Name = "Supprimer", TenantId = 1 },
                new SRightType { Code = "Table", Name = "Droits sur tables", DataSource = "STable", Right1Name = "Lire", Right2Name = "Écrire", Right3Name = "Supprimer", TenantId = 1 },
                new SRightType { Code = "Action", Name = "Droits sur actions", DataSource = "SAction", Right1Name = "Exécuter", TenantId = 1 }
            };
            db.SRightType.AddRange(rightTypes);
            await db.SaveChangesAsync();
        }
        */

        public static string Sha256(string input)
        {
            using var sha = SHA256.Create();
            var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(input));
            var sb = new StringBuilder();
            foreach (var b in bytes) sb.Append(b.ToString("x2"));
            return sb.ToString();
        }
    }



}
