using Aion.DataEngine.Interfaces;
namespace Aion.Infrastructure
{
    public sealed class DefaultUserContext : IUserContext
    {
        public int UserId { get; }
        public int TenantId { get; }
        public string? Username { get; }
        public DefaultUserContext(int tenantId = 1, int userId = 1, string? username = "Admin")
        { TenantId = tenantId; UserId = userId; Username = username; }
    }
}
