using Microsoft.EntityFrameworkCore;
using Aion.DataEngine.Interfaces;
using System;
using System.Security.Cryptography;
using System.Text;
using Aion.DataEngine.Entities;

namespace Aion.Infrastructure
{
    // 1. Ajoutez IUserContext userContext aux paramètres du constructeur principal
    public class AionDbContext(DbContextOptions<AionDbContext> options, IUserContext userContext) : DbContext(options)
    {
        // 2. Initialisez le champ _userContext avec le paramètre injecté
        private readonly IUserContext _userContext = userContext;

        // System DbSets (unifiés)
        public DbSet<SUser> Users => Set<SUser>(); // Gardé 'Users'
        public DbSet<SMenu> SMenu => Set<SMenu>();
        public DbSet<SModule> SModule => Set<SModule>();
        public DbSet<SAction> SAction => Set<SAction>();
        public DbSet<SReport> SReport => Set<SReport>();
        // public DbSet<SUser> SUser => Set<SUser>(); // <-- 3. Supprimé car doublon de 'Users'
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

            modelBuilder.Entity<SUser>(entity =>
            {
                entity.ToTable("SUser");
                entity.HasIndex(x => x.NormalizedUserName).IsUnique();
                entity.HasIndex(x => x.NormalizedEmail);
                entity.HasIndex(x => new { x.TenantId, x.Deleted });
            });

            // Ces filtres fonctionneront maintenant car _userContext sera initialisé
            // (à condition que IUserContext soit enregistré comme Scoped, voir note ci-dessous)
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