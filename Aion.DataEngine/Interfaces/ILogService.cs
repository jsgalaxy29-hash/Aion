using Microsoft.Extensions.Logging;

namespace Aion.DataEngine.Interfaces
{
    /// <summary>
    /// Centralized structured logging utility.
    /// </summary>
    public interface ILogService
    {
        void Log(LogLevel level, string message, Exception? exception = null, IDictionary<string, object?>? properties = null);
        void LogInformation(string message, IDictionary<string, object?>? properties = null);
        void LogWarning(string message, IDictionary<string, object?>? properties = null);
        void LogError(string message, Exception? exception = null, IDictionary<string, object?>? properties = null);
    }
}
