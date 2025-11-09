using System.Threading.Tasks;

namespace Aion.DataEngine.Interfaces
{
    /// <summary>
    /// Ensures the database is ready for Aion (idempotent):
    /// - Creates required tables (security, metadata S_TABLE/S_CHAMP, FDocument)
    /// - Populates STABLE and SFIELD from the real DB schema
    /// - Seeds Admin group/user and right types
    /// - Grants default rights to Admin and members of Administrateur
    /// - Detects new F_* tables and grants Admin rights automatically
    /// </summary>
    public interface IAionProvisioningService
    {
        Task EnsureDatabaseReadyAsync();
        Task EnsureAgendaSchemaAsync();
        Task SeedAgendaReferentialsAsync();
        Task SeedAgendaSystemScheduledActionsAsync();
        Task EnsureAgendaModuleAsync();
        Task EnsureAdminDefaultAgendaAsync();
    }
}
