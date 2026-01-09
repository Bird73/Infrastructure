namespace Birdsoft.Infrastructure.Logging.Abstractions;

public interface ILogStore
{
    Task<IReadOnlyList<DateOnly>> GetLogDatesAsync(
        CancellationToken cancellationToken = default);

    IAsyncEnumerable<LogEntry> GetLogsAsync(
        DateOnly date,
        CancellationToken cancellationToken = default);

    Task DeleteLogsAsync(
        DateOnly date,
        CancellationToken cancellationToken = default);
}
