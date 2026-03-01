namespace Birdsoft.Infrastructure.Logging.Abstractions;

public sealed class LogQuery
{
    public required DateOnly Date { get; init; }
    public LogLevel? MinLevel { get; init; }
    public string? Category { get; init; }
    public string? Keyword { get; init; }
    public bool OrderByTimestampDescending { get; init; }
}