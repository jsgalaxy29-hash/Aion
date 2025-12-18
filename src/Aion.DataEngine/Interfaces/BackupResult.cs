namespace Aion.DataEngine.Interfaces
{
    /// <summary>
    /// Describes the outcome of a backup operation.
    /// </summary>
    public sealed record BackupResult(string Provider, string DatabaseName, string BackupPath, DateTimeOffset CreatedAt);
}
