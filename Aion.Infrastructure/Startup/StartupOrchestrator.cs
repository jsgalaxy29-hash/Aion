using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Aion.DataEngine.Interfaces;
using Aion.Security;
using Aion.Infrastructure.Seeders;

namespace Aion.Infrastructure.Startup
{
    /// <summary>
    /// Orchestrateur de démarrage Aion.
    /// Coordonne AionProvisioningService (structure SQL) et SecuritySeeder (données EF Core).
    /// </summary>
    public class StartupOrchestrator
    {
        private readonly ILogger<StartupOrchestrator> _logger;
        private readonly IAionProvisioningService _provisioning;
        private readonly SecurityDbContext _securityDb;

        public StartupOrchestrator(
            ILogger<StartupOrchestrator> logger,
            IAionProvisioningService provisioning,
            SecurityDbContext securityDb)
        {
            _logger = logger;
            _provisioning = provisioning;
            _securityDb = securityDb;
        }

        /// <summary>
        /// Initialise complètement la base de données Aion au premier démarrage.
        /// </summary>
        public async Task InitializeAsync()
        {
            try
            {
                _logger.LogInformation("🚀 Démarrage de l'initialisation Aion...");

                // ====== PHASE 1 : Structure SQL via AionProvisioningService ======
                _logger.LogInformation("📊 Phase 1 : Création de la structure SQL...");
                await _provisioning.EnsureDatabaseReadyAsync();
                _logger.LogInformation("✅ Structure SQL créée");

                // ====== PHASE 2 : Données de sécurité via EF Core ======
                _logger.LogInformation("🔐 Phase 2 : Seed des données de sécurité...");
                await SecuritySeeder.SeedAsync(_securityDb);
                _logger.LogInformation("✅ Données de sécurité créées");

                // ====== PHASE 3 : Droits par défaut sur les menus ======
                _logger.LogInformation("🔑 Phase 3 : Attribution des droits par défaut...");
                await GrantDefaultMenuRightsAsync();
                _logger.LogInformation("✅ Droits par défaut attribués");

                _logger.LogInformation("🎉 Initialisation Aion terminée avec succès !");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Erreur lors de l'initialisation Aion");
                throw;
            }
        }

        /// <summary>
        /// Accorde les droits Menu par défaut au groupe Administrateurs.
        /// Récupère les menus existants depuis AionProvisioningService.
        /// </summary>
        private async Task GrantDefaultMenuRightsAsync()
        {
            // Récupérer les IDs de menus depuis la table SMenu
            // (créée par AionProvisioningService)
            var menuIds = new[] { 1, 2, 3, 4 }; // IDs des menus du YAML

            // TODO: Récupérer dynamiquement depuis la base
            // var menuIds = await _securityDb.Database.SqlQuery<int>(
            //     "SELECT ID FROM dbo.SMenu").ToListAsync();

            await SecuritySeeder.GrantAdminMenuRightsAsync(_securityDb, menuIds);
        }

        /// <summary>
        /// Méthode statique pour appel depuis Program.cs.
        /// </summary>
        public static async Task InitializeDatabaseAsync(IServiceProvider services)
        {
            using var scope = services.CreateScope();
            var orchestrator = scope.ServiceProvider.GetRequiredService<StartupOrchestrator>();
            await orchestrator.InitializeAsync();
        }
    }
}