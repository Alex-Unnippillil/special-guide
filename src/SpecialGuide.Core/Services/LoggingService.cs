using Microsoft.Extensions.Logging;

namespace SpecialGuide.Core.Services;

public class LoggingService
{
    private readonly ILogger<LoggingService> _logger;

    public LoggingService(ILogger<LoggingService> logger)
    {
        _logger = logger;
    }

    public virtual void LogError(Exception ex, string message) => _logger.LogError(ex, message);
}
