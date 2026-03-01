namespace Birdsoft.Infrastructure.Logging.Abstractions;

public sealed class LogEntry
{
    public required DateTimeOffset Timestamp { get; init; }
    public required LogLevel Level { get; init; }
    public required string Category { get; init; }
    public required string MessageTemplate { get; init; }
    public required string RenderedMessage { get; init; }
    public string? ExceptionDetail { get; init; }
    public required IReadOnlyDictionary<string, object?> Properties { get; init; }
}