namespace Aion.DataEngine.Interfaces
{
    /// <summary>
    /// Restores a database from a previously created backup file.
    /// </summary>
    public interface IRestoreService
    {
        /// <summary>
        /// Restores the configured database from the provided backup file.
        /// </summary>
        /// <param name="backupPath">Path to the backup previously created by <see cref="IBackupService"/>.</param>
        /// <param name="ct">Cancellation token.</param>
        Task RestoreAsync(string backupPath, CancellationToken ct = default);
    }
}
