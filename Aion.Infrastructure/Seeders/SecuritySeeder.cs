using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Aion.Security;
using Aion.DataEngine.Entities;

namespace Aion.Infrastructure.Seeders
{
    /// <summary>
    /// Seed les données de sécurité initiales (utilisateur admin, groupes, types de droits).
    /// </summary>
    public static class SecuritySeeder
    {
        public static async Task SeedAsync(SecurityDbContext db)
        {
            // Vérification si déjà seedé
            if (await db.SUser.AnyAsync())
                return;

            Console.WriteLine("🌱 Seed des données de sécurité...");

            // 1. Création groupe administrateurs
            var adminGroup = new SGroup
            {
                Id = 1,
                Name = "Administrateurs",
                Description = "Groupe administrateur système avec tous les droits",
                IsSystem = true,
                TenantId = 1,
                Actif = true,
                Doc = false,
                Deleted = false,
                DtCreation = DateTime.UtcNow
            };
            db.SGroup.Add(adminGroup);
            await db.SaveChangesAsync();

            // 2. Création utilisateur admin
            var admin = new SUser
            {
                Id = 1,
                UserName = "admin",
                NormalizedUserName = "ADMIN",
                Email = "admin@aion.local",
                NormalizedEmail = "ADMIN@AION.LOCAL",
                PasswordHash = "admin", // ⚠️ À REMPLACER par BCrypt en production !
                FullName = "Administrateur Système",
                IsActive = true,
                TenantId = 1,
                Actif = true,
                Doc = false,
                Deleted = false,
                DtCreation = DateTime.UtcNow
            };
            db.SUser.Add(admin);
            await db.SaveChangesAsync();

            // 3. Association admin au groupe
            var userGroup = new SUserGroup
            {
                UserId = admin.Id,
                GroupId = adminGroup.Id,
                IsLinkActive = true,
                TenantId = 1,
                Actif = true,
                Doc = false,
                Deleted = false,
                DtCreation = DateTime.UtcNow
            };
            db.SUserGroup.Add(userGroup);
            await db.SaveChangesAsync();

            // 4. Création types de droits
            var rightTypes = new[]
            {
                new SRightType
                {
                    Code = "Menu",
                    Name = "Droits sur menus",
                    DataSource = "SMenu",
                    Right1Name = "Voir",
                    Right2Name = "",
                    Right3Name = "",
                    Right4Name = "",
                    Right5Name = "",
                    Order = 1,
                    IsActive = true,
                    TenantId = 1,
                    Actif = true,
                    Doc = false,
                    Deleted = false,
                    DtCreation = DateTime.UtcNow
                },
                new SRightType
                {
                    Code = "Module",
                    Name = "Droits sur modules",
                    DataSource = "S_Module",
                    Right1Name = "Lire",
                    Right2Name = "Écrire",
                    Right3Name = "Supprimer",
                    Right4Name = "Exporter",
                    Right5Name = "Administrer",
                    Order = 2,
                    IsActive = true,
                    TenantId = 1,
                    Actif = true,
                    Doc = false,
                    Deleted = false,
                    DtCreation = DateTime.UtcNow
                },
                new SRightType
                {
                    Code = "Table",
                    Name = "Droits sur tables",
                    DataSource = "STable",
                    Right1Name = "Lire",
                    Right2Name = "Créer",
                    Right3Name = "Modifier",
                    Right4Name = "Supprimer",
                    Right5Name = "Exporter",
                    Order = 3,
                    IsActive = true,
                    TenantId = 1,
                    Actif = true,
                    Doc = false,
                    Deleted = false,
                    DtCreation = DateTime.UtcNow
                },
                new SRightType
                {
                    Code = "Action",
                    Name = "Droits sur actions",
                    DataSource = "S_Action",
                    Right1Name = "Exécuter",
                    Right2Name = "",
                    Right3Name = "",
                    Right4Name = "",
                    Right5Name = "",
                    Order = 4,
                    IsActive = true,
                    TenantId = 1,
                    Actif = true,
                    Doc = false,
                    Deleted = false,
                    DtCreation = DateTime.UtcNow
                },
                new SRightType
                {
                    Code = "Report",
                    Name = "Droits sur rapports",
                    DataSource = "S_Report",
                    Right1Name = "Voir",
                    Right2Name = "Générer",
                    Right3Name = "",
                    Right4Name = "",
                    Right5Name = "",
                    Order = 5,
                    IsActive = true,
                    TenantId = 1,
                    Actif = true,
                    Doc = false,
                    Deleted = false,
                    DtCreation = DateTime.UtcNow
                }
            };

            db.SRightType.AddRange(rightTypes);
            await db.SaveChangesAsync();

            Console.WriteLine("✅ Seed terminé :");
            Console.WriteLine($"   - Utilisateur : admin / admin (TenantId: 1)");
            Console.WriteLine($"   - Groupe : Administrateurs");
            Console.WriteLine($"   - {rightTypes.Length} types de droits créés");
            Console.WriteLine("⚠️  ATTENTION : Changez le mot de passe admin en production !");
        }

        /// <summary>
        /// Donne tous les droits au groupe administrateurs sur tous les menus existants.
        /// À appeler après création des menus.
        /// </summary>
        public static async Task GrantAdminRightsAsync(SecurityDbContext securityDb, DbContext aionDb, int[] menuIds)
        {
            var adminGroup = await securityDb.SGroup.FirstOrDefaultAsync(g => g.Name == "Administrateurs");
            if (adminGroup == null)
                return;

            foreach (var menuId in menuIds)
            {
                var exists = await securityDb.SRight.AnyAsync(r =>
                    r.GroupId == adminGroup.Id &&
                    r.Target == "Menu" &&
                    r.SubjectId == menuId);

                if (!exists)
                {
                    securityDb.SRight.Add(new SRight
                    {
                        GroupId = adminGroup.Id,
                        Target = "Menu",
                        SubjectId = menuId,
                        Right1 = true, // Voir
                        TenantId = 1,
                        Actif = true,
                        Doc = false,
                        Deleted = false,
                        DtCreation = DateTime.UtcNow
                    });
                }
            }

            await securityDb.SaveChangesAsync();
        }
    }
}