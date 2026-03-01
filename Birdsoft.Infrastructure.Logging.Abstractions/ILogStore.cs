namespace Birdsoft.Infrastructure.Logging.Abstractions;

public interface ILogStore
{
    Task<IReadOnlyList<DateOnly>> GetLogDatesAsync(CancellationToken ct = default);
    IAsyncEnumerable<LogEntry> GetLogsAsync(LogQuery query, CancellationToken ct = default);
    Task DeleteLogsAsync(DateOnly date, CancellationToken ct = default);
}