namespace Aion.DataEngine.Interfaces
{
    /// <summary>
    /// Provides database backup capabilities for the configured provider.
    /// </summary>
    public interface IBackupService
    {
        /// <summary>
        /// Creates a database backup and returns metadata about the operation.
        /// </summary>
        /// <param name="destinationFolder">Optional target directory. If null, a default "backups" folder is used.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>Metadata describing the backup file.</returns>
        Task<BackupResult> BackupAsync(string? destinationFolder = null, CancellationToken ct = default);
    }
}
