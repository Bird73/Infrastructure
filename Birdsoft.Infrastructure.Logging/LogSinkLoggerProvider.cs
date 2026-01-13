namespace Birdsoft.Infrastructure.Logging;

using Birdsoft.Infrastructure.Logging.Abstractions;
using Microsoft.Extensions.Logging;

internal sealed class LogSinkLoggerProvider : ILoggerProvider, ISupportExternalScope
{
    private readonly ILogSink _sink;
    private IExternalScopeProvider? _scopeProvider;

    public LogSinkLoggerProvider(ILogSink sink)
    {
        _sink = sink ?? throw new ArgumentNullException(nameof(sink));
    }

    public ILogger CreateLogger(string categoryName)
        => new LogSinkLogger(categoryName ?? string.Empty, _sink, () => _scopeProvider);

    public void Dispose()
    {
    }

    public void SetScopeProvider(IExternalScopeProvider scopeProvider)
        => _scopeProvider = scopeProvider;

    private sealed class LogSinkLogger : ILogger
    {
        private readonly string _category;
        private readonly ILogSink _sink;
        private readonly Func<IExternalScopeProvider?> _getScopes;

        public LogSinkLogger(string category, ILogSink sink, Func<IExternalScopeProvider?> getScopes)
        {
            _category = category;
            _sink = sink;
            _getScopes = getScopes;
        }

        public IDisposable BeginScope<TState>(TState state)
            where TState : notnull
        {
            var scopes = _getScopes();
            return scopes?.Push(state) ?? NullScope.Instance;
        }

        public bool IsEnabled(Microsoft.Extensions.Logging.LogLevel logLevel)
            => logLevel != Microsoft.Extensions.Logging.LogLevel.None;

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

            if (formatter is null)
            {
                throw new ArgumentNullException(nameof(formatter));
            }

            var message = formatter(state, exception);

            var props = new Dictionary<string, object?>();
            if (eventId.Id != 0)
            {
                props["event_id"] = eventId.Id;
            }

            if (!string.IsNullOrWhiteSpace(eventId.Name))
            {
                props["event_name"] = eventId.Name;
            }

            var entry = new LogEntry
            {
                Timestamp = DateTimeOffset.UtcNow,
                Level = MapLevel(logLevel),
                Category = _category,
                Message = message,
                Exception = exception,
                Properties = props,
            };

            // ILogger is synchronous; persist immediately for deterministic tests.
            _sink.WriteAsync(entry).GetAwaiter().GetResult();
        }

        private static Abstractions.LogLevel MapLevel(Microsoft.Extensions.Logging.LogLevel level)
            => level switch
            {
                Microsoft.Extensions.Logging.LogLevel.Trace => Abstractions.LogLevel.Trace,
                Microsoft.Extensions.Logging.LogLevel.Debug => Abstractions.LogLevel.Debug,
                Microsoft.Extensions.Logging.LogLevel.Information => Abstractions.LogLevel.Information,
                Microsoft.Extensions.Logging.LogLevel.Warning => Abstractions.LogLevel.Warning,
                Microsoft.Extensions.Logging.LogLevel.Error => Abstractions.LogLevel.Error,
                Microsoft.Extensions.Logging.LogLevel.Critical => Abstractions.LogLevel.Critical,
                _ => Abstractions.LogLevel.Information,
            };

        private sealed class NullScope : IDisposable
        {
            public static NullScope Instance { get; } = new();
            public void Dispose() { }
        }
    }
}
