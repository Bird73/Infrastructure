using System.Diagnostics;
using Birdsoft.Infrastructure.Logging.Abstractions;
using Serilog;

namespace Birdsoft.Infrastructure.Logging;

public sealed class SerilogAppLogger<T> : IAppLogger<T>
{
    private readonly ILogger _logger;
    private readonly ILogSink _sink;

    public SerilogAppLogger(ILogger logger, ILogSink sink)
    {
        _logger = logger;
        _sink = sink;
    }

    public bool IsEnabled(LogLevel level)
    {
        if (level == LogLevel.None)
        {
            return false;
        }

        return _logger.IsEnabled(LevelMapper.ToSerilogLevel(level));
    }

    public void Log(LogLevel level, Exception? exception, string messageTemplate, params object?[] args)
    {
        if (level == LogLevel.None || !IsEnabled(level))
        {
            return;
        }

        var parsed = MessageTemplateParser.Parse(messageTemplate, args);
        var properties = new Dictionary<string, object?>(parsed.Properties, StringComparer.OrdinalIgnoreCase);

        if (Activity.Current is { } activity)
        {
            properties["trace_id"] = activity.TraceId.ToString();
            properties["span_id"] = activity.SpanId.ToString();
        }

        var normalizedAndRedacted = LogEntryRedactor.RedactProperties(properties);
        var entry = new LogEntry
        {
            Timestamp = DateTimeOffset.UtcNow,
            Level = level,
            Category = typeof(T).FullName ?? typeof(T).Name,
            MessageTemplate = messageTemplate,
            RenderedMessage = LogEntryRedactor.Redact(parsed.RenderedMessage) ?? string.Empty,
            ExceptionDetail = LogEntryRedactor.Redact(exception?.ToString()),
            Properties = normalizedAndRedacted
        };

        _logger.Write(LevelMapper.ToSerilogLevel(level), exception, messageTemplate, args);

        try
        {
            _sink.WriteAsync(entry).GetAwaiter().GetResult();
        }
        catch
        {
        }
    }
}