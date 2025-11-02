using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Aion.Security;
using Aion.DataEngine.Entities;

namespace Aion.Infrastructure.Seeders
{
    /// <summary>
    /// Seed les données de sécurité initiales via EF Core.
    /// Complémentaire à AionProvisioningService qui gère la structure SQL.
    /// </summary>
    public static class SecuritySeeder
    {
        /// <summary>
        /// Seed utilisateur admin, groupe et types de droits.
        /// Version EF Core compatible avec AionProvisioningService.
        /// </summary>
        public static async Task SeedAsync(SecurityDbContext db)
        {
            Console.WriteLine("🔍 Vérification des données de sécurité...");

            // Vérifier si admin existe déjà (créé par AionProvisioningService)
            var adminExists = await db.SUser.AnyAsync(u => u.UserName == "admin" || u.UserName == "Admin");
            var groupExists = await db.SGroup.AnyAsync(g => g.Name == "Administrateurs" || g.Name == "Administrateur");

            if (adminExists && groupExists)
            {
                Console.WriteLine("✅ Données de sécurité déjà présentes (via AionProvisioningService)");
                return;
            }

            Console.WriteLine("🌱 Création des données de sécurité via EF Core...");

            // 1. Groupe Administrateurs (si n'existe pas)
            var adminGroup = await db.SGroup.FirstOrDefaultAsync(g => g.Name == "Administrateurs" || g.Name == "Administrateur");
            if (adminGroup == null)
            {
                adminGroup = new SGroup
                {
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
                Console.WriteLine($"   ✅ Groupe créé: {adminGroup.Name} (ID: {adminGroup.Id})");
            }

            // 2. Utilisateur admin (si n'existe pas)
            var admin = await db.SUser.FirstOrDefaultAsync(u => u.UserName == "admin" || u.UserName == "Admin");
            if (admin == null)
            {
                var hashedPassword = BCrypt.Net.BCrypt.HashPassword("admin");

                admin = new SUser
                {
                    UserName = "admin",
                    NormalizedUserName = "ADMIN",
                    Email = "admin@aion.local",
                    NormalizedEmail = "ADMIN@AION.LOCAL",
                    PasswordHash = hashedPassword,
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
                Console.WriteLine($"   ✅ Utilisateur créé: {admin.UserName} (ID: {admin.Id})");
            }

            // 3. Association user-groupe (si n'existe pas)
            var linkExists = await db.SUserGroup.AnyAsync(ug => ug.UserId == admin.Id && ug.GroupId == adminGroup.Id);
            if (!linkExists)
            {
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
                Console.WriteLine($"   ✅ Association créée: User {admin.Id} → Groupe {adminGroup.Id}");
            }

            // 4. Types de droits (complète si manquant)
            await EnsureRightTypesAsync(db);

            Console.WriteLine("✅ Seed de sécurité terminé");
            Console.WriteLine($"   🔑 Connexion : admin / admin (TenantId: 1)");
            Console.WriteLine("   ⚠️  IMPORTANT : Changez le mot de passe en production !");
        }

        /// <summary>
        /// S'assure que tous les types de droits existent.
        /// </summary>
        private static async Task EnsureRightTypesAsync(SecurityDbContext db)
        {
            var rightTypes = new[]
            {
                new { Code = "Menu", Name = "Droits sur menus", DataSource = "SMenu", R1 = "Voir", R2 = "", R3 = "", R4 = "", R5 = "" },
                new { Code = "Module", Name = "Droits sur modules", DataSource = "SModule", R1 = "Lire", R2 = "Écrire", R3 = "Supprimer", R4 = "Exporter", R5 = "Administrer" },
                new { Code = "Table", Name = "Droits sur tables", DataSource = "STable", R1 = "Lire", R2 = "Créer", R3 = "Modifier", R4 = "Supprimer", R5 = "Exporter" },
                new { Code = "Action", Name = "Droits sur actions", DataSource = "SAction", R1 = "Exécuter", R2 = "", R3 = "", R4 = "", R5 = "" },
                new { Code = "Report", Name = "Droits sur rapports", DataSource = "SReport", R1 = "Voir", R2 = "Générer", R3 = "", R4 = "", R5 = "" }
            };

            foreach (var rt in rightTypes)
            {
                var exists = await db.SRightType.AnyAsync(r => r.Code == rt.Code);
                if (!exists)
                {
                    db.SRightType.Add(new SRightType
                    {
                        Code = rt.Code,
                        Name = rt.Name,
                        DataSource = rt.DataSource,
                        Right1Name = rt.R1,
                        Right2Name = rt.R2,
                        Right3Name = rt.R3,
                        Right4Name = rt.R4,
                        Right5Name = rt.R5,
                        Order = rightTypes.ToList().IndexOf(rt) + 1,
                        IsActive = true,
                        TenantId = 1,
                        Actif = true,
                        Doc = false,
                        Deleted = false,
                        DtCreation = DateTime.UtcNow
                    });
                }
            }

            await db.SaveChangesAsync();
        }

        public static async Task EnsureSystemMenusAsync(AionDbContext appDb)
        {
            var utcNow = DateTime.UtcNow;

            var adminModule = await appDb.SModule.FirstOrDefaultAsync(m => m.Name == "Gestion des tables");
            if (adminModule == null)
            {
                adminModule = new SModule
                {
                    Name = "Gestion des tables",
                    Order = 900,
                    Route = "/admin/catalog",
                    TenantId = 1,
                    Actif = true,
                    DtCreation = utcNow,
                    UsrCreationId = 1
                };
                appDb.SModule.Add(adminModule);
                await appDb.SaveChangesAsync();
            }
            else if (string.IsNullOrWhiteSpace(adminModule.Route))
            {
                adminModule.Route = "/admin/catalog";
                appDb.SModule.Update(adminModule);
                await appDb.SaveChangesAsync();
            }

            var rightsModule = await appDb.SModule.FirstOrDefaultAsync(m => m.Name == "Gestion Droit");
            if (rightsModule == null)
            {
                rightsModule = new SModule
                {
                    Name = "Gestion Droit",
                    Order = adminModule?.Order + 1 ?? 901,
                    Route = "/admin/rights",
                    TenantId = 1,
                    Actif = true,
                    DtCreation = utcNow,
                    UsrCreationId = 1
                };
                appDb.SModule.Add(rightsModule);
                await appDb.SaveChangesAsync();
            }
            else if (string.IsNullOrWhiteSpace(rightsModule.Route))
            {
                rightsModule.Route = "/admin/rights";
                appDb.SModule.Update(rightsModule);
                await appDb.SaveChangesAsync();
            }

            var adminRootMenu = await appDb.SMenu.FirstOrDefaultAsync(m => m.Libelle == "Administration");
            if (adminRootMenu == null)
            {
                adminRootMenu = new SMenu
                {
                    Libelle = "Administration",
                    ParentId = null,
                    Icon = "Settings20Regular",
                    IsLeaf = false,
                    Order = 900,
                    TenantId = 1,
                    Actif = true,
                    DtCreation = utcNow,
                    UsrCreationId = 1
                };
                appDb.SMenu.Add(adminRootMenu);
                await appDb.SaveChangesAsync();
            }

            var designerMenu = await appDb.SMenu.FirstOrDefaultAsync(m => m.Libelle == "Liste des tables");
            if (designerMenu == null)
            {
                designerMenu = new SMenu
                {
                    ModuleId = adminModule.Id,
                    Libelle = "Liste des tables",
                    ParentId = adminRootMenu.Id,
                    Icon = "DatabaseLink20Regular",
                    IsLeaf = true,
                    Order = adminRootMenu.Order + 1,
                    TenantId = 1,
                    Actif = true,
                    DtCreation = utcNow,
                    UsrCreationId = 1
                };
                appDb.SMenu.Add(designerMenu);
                await appDb.SaveChangesAsync();
            }

            var rightsMenu = await appDb.SMenu.FirstOrDefaultAsync(m => m.Libelle == "Gestion des droits");
            if (rightsMenu == null)
            {
                rightsMenu = new SMenu
                {
                    ModuleId = rightsModule.Id,
                    Libelle = "Gestion des droits",
                    ParentId = adminRootMenu.Id,
                    Icon = "ShieldCheckmark20Regular",
                    IsLeaf = true,
                    Order = designerMenu.Order + 1,
                    TenantId = 1,
                    Actif = true,
                    DtCreation = utcNow,
                    UsrCreationId = 1
                };
                appDb.SMenu.Add(rightsMenu);
                await appDb.SaveChangesAsync();
            }
        }

        /// <summary>
        /// Donne tous les droits Menu au groupe Administrateurs.
        /// À appeler après création des menus.
        /// </summary>
        public static async Task GrantAdminMenuRightsAsync(SecurityDbContext securityDb, int[] menuIds)
        {
            var adminGroup = await securityDb.SGroup
                .FirstOrDefaultAsync(g => g.Name == "Administrateurs" || g.Name == "Administrateur");

            if (adminGroup == null)
            {
                Console.WriteLine("⚠️  Groupe Administrateurs introuvable, impossible de donner les droits menus");
                return;
            }

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
            Console.WriteLine($"✅ Droits Menu accordés au groupe Administrateurs ({menuIds.Length} menus)");
        }
    }
}