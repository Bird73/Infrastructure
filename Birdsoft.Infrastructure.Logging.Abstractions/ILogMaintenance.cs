namespace Birdsoft.Infrastructure.Logging.Abstractions;

public interface ILogMaintenance
{
    IReadOnlyCollection<DateOnly> ExplicitDeleteDates { get; }
    void AddExplicitDeleteDate(DateOnly date);
    bool RemoveExplicitDeleteDate(DateOnly date);
    Task ExecuteAsync(ILogStore store, DateOnly today, CancellationToken ct = default);
}