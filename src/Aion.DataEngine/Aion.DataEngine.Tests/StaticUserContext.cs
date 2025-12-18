using Aion.DataEngine.Interfaces;

namespace Aion.DataEngine.Tests
{
    internal sealed class StaticUserContext : IUserContext
    {
        public int CurrentUserId { get; set; } = 1;
        public int Tenant { get; set; } = 1;
        public string? Name { get; set; }

        public int UserId => CurrentUserId;
        public int TenantId => Tenant;
        public string? Username => Name;
    }
}
