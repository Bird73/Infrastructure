namespace Birdsoft.Infrastructure.Logging.Abstractions;

public sealed class LogEntry
{
    public DateTimeOffset Timestamp { get; init; }

    public LogLevel Level { get; init; }

    public string Category { get; init; } = string.Empty;

    public string Message { get; init; } = string.Empty;

    public Exception? Exception { get; init; }

    public IReadOnlyDictionary<string, object?> Properties { get; init; }
        = new Dictionary<string, object?>();
}
