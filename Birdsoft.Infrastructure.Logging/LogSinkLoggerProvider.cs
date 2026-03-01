using System.Diagnostics;
using Birdsoft.Infrastructure.Logging.Abstractions;
using Microsoft.Extensions.Logging;

namespace Birdsoft.Infrastructure.Logging;

public sealed class LogSinkLoggerProvider : ILoggerProvider, ISupportExternalScope
{
    private readonly ILogSink _sink;
    private IExternalScopeProvider _scopeProvider = new LoggerExternalScopeProvider();
    private bool _disposed;

    public LogSinkLoggerProvider(ILogSink sink)
    {
        _sink = sink;
    }

    public ILogger CreateLogger(string categoryName)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return new LogSinkLogger(categoryName, _sink, () => _scopeProvider);
    }

    public void SetScopeProvider(IExternalScopeProvider scopeProvider)
    {
        _scopeProvider = scopeProvider;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        if (_sink is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }

    private sealed class LogSinkLogger : ILogger
    {
        private readonly string _category;
        private readonly ILogSink _sink;
        private readonly Func<IExternalScopeProvider> _scopeProviderAccessor;

        public LogSinkLogger(string category, ILogSink sink, Func<IExternalScopeProvider> scopeProviderAccessor)
        {
            _category = category;
            _sink = sink;
            _scopeProviderAccessor = scopeProviderAccessor;
        }

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull
        {
            return _scopeProviderAccessor().Push(state);
        }

        public bool IsEnabled(Microsoft.Extensions.Logging.LogLevel logLevel)
        {
            return logLevel != Microsoft.Extensions.Logging.LogLevel.None;
        }

        public void Log<TState>(
            Microsoft.Extensions.Logging.LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel))
            {
                return;
            }

            var appLevel = LevelMapper.ToAppLevel(logLevel);
            if (appLevel == Abstractions.LogLevel.None)
            {
                return;
            }

            var properties = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            var messageTemplate = "{Message}";

            if (state is IReadOnlyList<KeyValuePair<string, object?>> statePairs)
            {
                foreach (var pair in statePairs)
                {
                    if (pair.Key == "{OriginalFormat}" && pair.Value is string template)
                    {
                        messageTemplate = template;
                        continue;
                    }

                    properties[pair.Key] = pair.Value;
                }
            }
            else
            {
                properties["state"] = state;
            }

            _scopeProviderAccessor().ForEachScope((scope, dict) =>
            {
                switch (scope)
                {
                    case IEnumerable<KeyValuePair<string, object?>> scopePairs:
                        foreach (var item in scopePairs)
                        {
                            dict[$"scope.{item.Key}"] = item.Value;
                        }
                        break;
                    default:
                        dict[$"scope.{dict.Count}"] = scope?.ToString();
                        break;
                }
            }, properties);

            if (Activity.Current is { } activity)
            {
                properties["trace_id"] = activity.TraceId.ToString();
                properties["span_id"] = activity.SpanId.ToString();
            }

            var normalizedAndRedacted = LogEntryRedactor.RedactProperties(properties);
            var rendered = formatter(state, exception);

            var entry = new LogEntry
            {
                Timestamp = DateTimeOffset.UtcNow,
                Level = appLevel,
                Category = _category,
                MessageTemplate = messageTemplate,
                RenderedMessage = LogEntryRedactor.Redact(rendered) ?? string.Empty,
                ExceptionDetail = LogEntryRedactor.Redact(exception?.ToString()),
                Properties = normalizedAndRedacted
            };

            try
            {
                _sink.WriteAsync(entry).GetAwaiter().GetResult();
            }
            catch
            {
            }
        }
    }
}