using Aion.DataEngine.Interfaces;
using Microsoft.Extensions.Logging;

namespace Aion.Infrastructure.Services;

public sealed class LogService : ILogService
{
    private readonly ILogger<LogService> _logger;
    private readonly IUserContext _userContext;

    public LogService(ILogger<LogService> logger, IUserContext userContext)
    {
        _logger = logger;
        _userContext = userContext;
    }

    public void Log(LogLevel level, string message, Exception? exception = null, IDictionary<string, object?>? properties = null)
    {
        var payload = BuildPayload(properties);
        if (exception is not null)
        {
            _logger.Log(level, exception, "{Message} | {@Payload}", message, payload);
        }
        else
        {
            _logger.Log(level, "{Message} | {@Payload}", message, payload);
        }
    }

    public void LogInformation(string message, IDictionary<string, object?>? properties = null)
        => Log(LogLevel.Information, message, null, properties);

    public void LogWarning(string message, IDictionary<string, object?>? properties = null)
        => Log(LogLevel.Warning, message, null, properties);

    public void LogError(string message, Exception? exception = null, IDictionary<string, object?>? properties = null)
        => Log(LogLevel.Error, message, exception, properties);

    private StructuredLogPayload BuildPayload(IDictionary<string, object?>? properties)
    {
        return new StructuredLogPayload
        {
            TenantId = _userContext.TenantId,
            UserId = _userContext.UserId,
            Username = _userContext.Username,
            Properties = properties ?? new Dictionary<string, object?>()
        };
    }

    private sealed record StructuredLogPayload
    {
        public int TenantId { get; init; }
        public int UserId { get; init; }
        public string? Username { get; init; }
        public IDictionary<string, object?> Properties { get; init; } = new Dictionary<string, object?>();
    }
}
