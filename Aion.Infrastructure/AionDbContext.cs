using Microsoft.EntityFrameworkCore;
using Aion.DataEngine.Interfaces;
using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Aion.DataEngine.Entities;
using Aion.Domain.AI;
using Aion.Domain.Agenda;

namespace Aion.Infrastructure
{
    // 1. Ajoutez IUserContext userContext aux paramètres du constructeur principal
    public class AionDbContext(DbContextOptions<AionDbContext> options, IUserContext? userContext = null) : DbContext(options)
    {
        // 2. Initialisez le champ _userContext avec le paramètre injecté
        private readonly IUserContext _userContext = userContext ?? new DefaultUserContext();

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
        public DbSet<SXGenerationLog> SXGenerationLogs => Set<SXGenerationLog>();
        public DbSet<SXAiConfig> SXAiConfigs => Set<SXAiConfig>();
        public DbSet<SXSynonym> SXSynonyms => Set<SXSynonym>();
        public DbSet<SXTemplate> SXTemplates => Set<SXTemplate>();
        public DbSet<SAgenda> SAgendas => Set<SAgenda>();
        public DbSet<SAgendaUser> SAgendaUsers => Set<SAgendaUser>();
        public DbSet<SAgendaEvent> SAgendaEvents => Set<SAgendaEvent>();
        public DbSet<SAgendaReminder> SAgendaReminders => Set<SAgendaReminder>();
        public DbSet<SPushSubscription> SPushSubscriptions => Set<SPushSubscription>();
        public DbSet<SNotification> SNotifications => Set<SNotification>();
        public DbSet<SScheduledAction> SScheduledActions => Set<SScheduledAction>();
        public DbSet<RReminderChannel> RReminderChannels => Set<RReminderChannel>();
        public DbSet<RAgendaEventStatus> RAgendaEventStatuses => Set<RAgendaEventStatus>();
        public DbSet<RScheduledActionStatus> RScheduledActionStatuses => Set<RScheduledActionStatus>();
        public DbSet<RNotificationType> RNotificationTypes => Set<RNotificationType>();

        public bool IsSqlServer()
        {
            return Database.ProviderName == "Microsoft.EntityFrameworkCore.SqlServer";
        }

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
            modelBuilder.Entity<SAgenda>().HasQueryFilter(e => e.TenantId == _userContext.TenantId);
            modelBuilder.Entity<SAgendaUser>().HasQueryFilter(e => e.TenantId == _userContext.TenantId);
            modelBuilder.Entity<SAgendaEvent>().HasQueryFilter(e => e.TenantId == _userContext.TenantId);
            modelBuilder.Entity<SAgendaReminder>().HasQueryFilter(e => e.TenantId == _userContext.TenantId);
            modelBuilder.Entity<SPushSubscription>().HasQueryFilter(e => e.TenantId == _userContext.TenantId);
            modelBuilder.Entity<SNotification>().HasQueryFilter(e => e.TenantId == _userContext.TenantId);
            modelBuilder.Entity<SScheduledAction>().HasQueryFilter(e => e.TenantId == _userContext.TenantId);
            modelBuilder.Entity<RReminderChannel>().HasQueryFilter(e => e.TenantId == _userContext.TenantId);
            modelBuilder.Entity<RAgendaEventStatus>().HasQueryFilter(e => e.TenantId == _userContext.TenantId);
            modelBuilder.Entity<RScheduledActionStatus>().HasQueryFilter(e => e.TenantId == _userContext.TenantId);
            modelBuilder.Entity<RNotificationType>().HasQueryFilter(e => e.TenantId == _userContext.TenantId);

            modelBuilder.ApplyConfigurationsFromAssembly(typeof(AionDbContext).Assembly);

            modelBuilder.Entity<SAgenda>(entity =>
            {
                entity.ToTable("SAgenda");
                entity.Property(e => e.Libelle).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Color).HasMaxLength(32);
                entity.Property(e => e.TimeZoneId).HasMaxLength(128);
                entity.HasMany(e => e.Events)
                    .WithOne(e => e.Agenda)
                    .HasForeignKey(e => e.AgendaId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<SAgendaUser>(entity =>
            {
                entity.ToTable("SAgendaUser");
                entity.HasIndex(e => new { e.AgendaId, e.UserId }).IsUnique();
                entity.HasOne(e => e.Agenda)
                    .WithMany(e => e.SharedUsers)
                    .HasForeignKey(e => e.AgendaId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(e => e.User)
                    .WithMany()
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<RAgendaEventStatus>(entity =>
            {
                entity.ToTable("RAgendaEventStatus");
                entity.Property(e => e.Code).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Libelle).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Id).HasColumnType("smallint");
            });

            modelBuilder.Entity<RReminderChannel>(entity =>
            {
                entity.ToTable("RReminderChannel");
                entity.Property(e => e.Code).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Libelle).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Id).HasColumnType("smallint");
            });

            modelBuilder.Entity<RScheduledActionStatus>(entity =>
            {
                entity.ToTable("RScheduledActionStatus");
                entity.Property(e => e.Code).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Libelle).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Id).HasColumnType("smallint");
            });

            modelBuilder.Entity<RNotificationType>(entity =>
            {
                entity.ToTable("RNotificationType");
                entity.Property(e => e.Code).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Libelle).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Id).HasColumnType("smallint");
            });

            modelBuilder.Entity<SAgendaEvent>(entity =>
            {
                entity.ToTable("SAgendaEvent");
                entity.Property(e => e.Libelle).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Description).HasMaxLength(2000);
                entity.Property(e => e.ContextEntityType).HasMaxLength(128);
                entity.HasOne(e => e.Status)
                    .WithMany(s => s.Events)
                    .HasForeignKey(e => e.StatusId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<SAgendaReminder>(entity =>
            {
                entity.ToTable("SAgendaReminder");
                entity.HasOne(e => e.AgendaEvent)
                    .WithMany(e => e.Reminders)
                    .HasForeignKey(e => e.AgendaEventId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(e => e.Channel)
                    .WithMany(c => c.Reminders)
                    .HasForeignKey(e => e.ChannelId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<SPushSubscription>(entity =>
            {
                entity.ToTable("SPushSubscription");
                entity.Property(e => e.Endpoint).IsRequired().HasMaxLength(1024);
                entity.Property(e => e.P256dh).IsRequired().HasMaxLength(512);
                entity.Property(e => e.Auth).IsRequired().HasMaxLength(256);
                entity.Property(e => e.DeviceInfo).HasMaxLength(512);
                entity.HasOne(e => e.User)
                    .WithMany()
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<SNotification>(entity =>
            {
                entity.ToTable("SNotification");
                entity.Property(e => e.Title).IsRequired().HasMaxLength(256);
                entity.Property(e => e.Message).IsRequired().HasMaxLength(2000);
                entity.Property(e => e.LinkUrl).HasMaxLength(1024);
                entity.HasOne(e => e.User)
                    .WithMany()
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(e => e.NotificationType)
                    .WithMany(t => t.Notifications)
                    .HasForeignKey(e => e.NotificationTypeId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            modelBuilder.Entity<SScheduledAction>(entity =>
            {
                entity.ToTable("SScheduledAction");
                entity.Property(e => e.Libelle).IsRequired().HasMaxLength(200);
                entity.Property(e => e.CronExpression).IsRequired().HasMaxLength(64);
                entity.Property(e => e.ParametersJson).HasMaxLength(4000);
                entity.Property(e => e.LastError).HasMaxLength(2000);
                entity.HasOne(e => e.Action)
                    .WithMany()
                    .HasForeignKey(e => e.ActionId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(e => e.Status)
                    .WithMany(s => s.ScheduledActions)
                    .HasForeignKey(e => e.StatusId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            foreach (var entityType in modelBuilder.Model.GetEntityTypes()
                         .Where(t => typeof(BaseEntity).IsAssignableFrom(t.ClrType)))
            {
                var method = typeof(AionDbContext).GetMethod(nameof(ApplySoftDeleteFilter), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
                method?.MakeGenericMethod(entityType.ClrType).Invoke(null, new object[] { modelBuilder });
            }
        }

        public static string Sha256(string input)
        {
            using var sha = SHA256.Create();
            var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(input));
            var sb = new StringBuilder();
            foreach (var b in bytes) sb.Append(b.ToString("x2"));
            return sb.ToString();
        }

        public override int SaveChanges()
        {
            UpdateAuditInformation();
            return base.SaveChanges();
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            UpdateAuditInformation();
            return base.SaveChangesAsync(cancellationToken);
        }

        private void UpdateAuditInformation()
        {
            var utcNow = DateTimeOffset.UtcNow;
            foreach (var entry in ChangeTracker.Entries<BaseEntity>())
            {
                if (entry.State == EntityState.Added)
                {
                    entry.Entity.DtCreation = DateTime.UtcNow;
                    entry.Entity.DtModification = DateTime.UtcNow;
                }
                else if (entry.State == EntityState.Modified)
                {
                    entry.Entity.DtModification = DateTime.UtcNow;
                }
            }
        }

        private static void ApplySoftDeleteFilter<TEntity>(ModelBuilder builder) where TEntity : BaseEntity
        {
            builder.Entity<TEntity>().HasQueryFilter(entity => !entity.Deleted);
        }
    }
}