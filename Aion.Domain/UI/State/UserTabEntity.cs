using System;

namespace Aion.Domain.UI.State
{
    /// <summary>
    /// Persisted tab opened by a user. Stores minimal info to restore session.
    /// </summary>
    public sealed class UserTabEntity
    {
        public long Id { get; set; }
        public Guid TenantId { get; set; }
        public Guid UserId { get; set; }
        public string Title { get; set; } = default!;
        public string Route { get; set; } = default!;
        public string? ParametersJson { get; set; }
        public DateTimeOffset OpenedAt { get; set; }
        public string? LastStateJson { get; set; }
    }
}
