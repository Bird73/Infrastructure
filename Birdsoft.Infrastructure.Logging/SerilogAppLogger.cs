using Birdsoft.Infrastructure.Logging.Abstractions;
using Serilog;

namespace Birdsoft.Infrastructure.Logging;

public sealed class SerilogAppLogger<T> : IAppLogger<T>
{
    private readonly ILogger _logger;
    private readonly ILogSink? _logSink;

    public SerilogAppLogger(ILogger logger, ILogSink? logSink = null)
    {
        _logger = logger;
        _logSink = logSink;
    }

    public bool IsEnabled(LogLevel level)
        => _logger.IsEnabled(SerilogLevelMapper.ToSerilogLevel(level));

    public void Log(LogLevel level, Exception? exception, string messageTemplate, params object?[] args)
    {
        if (messageTemplate is null)
        {
            throw new ArgumentNullException(nameof(messageTemplate));
        }

        _logger.Write(SerilogLevelMapper.ToSerilogLevel(level), exception, messageTemplate, args);

        if (_logSink is null)
        {
            return;
        }

        var entry = new LogEntry
        {
            Timestamp = DateTimeOffset.Now,
            Level = level,
            Category = typeof(T).FullName ?? typeof(T).Name,
            Message = messageTemplate,
            Exception = exception,
            Properties = new Dictionary<string, object?>
            {
                ["Args"] = args,
            },
        };

        _ = SafeWriteAsync(entry);
        return;

        async Task SafeWriteAsync(LogEntry e)
        {
            try
            {
                await _logSink.WriteAsync(e).ConfigureAwait(false);
            }
            catch
            {
                // Intentionally ignore sink failures to avoid breaking caller flow.
            }
        }
    }
}
