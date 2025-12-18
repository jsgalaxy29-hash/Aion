namespace Aion.DataEngine.Interfaces
{
    public interface IUserContext
    {
        int UserId { get; }
        int TenantId { get; }
        string? Username { get; }
    }
}
