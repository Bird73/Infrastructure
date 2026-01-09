namespace Birdsoft.Infrastructure.Logging.Abstractions;

public interface ILogMaintenance
{
    int? RetentionDays { get; set; }

    IReadOnlyCollection<DateOnly> ExplicitDeleteDates { get; }

    void AddExplicitDeleteDate(DateOnly date);

    bool RemoveExplicitDeleteDate(DateOnly date);

    Task ExecuteAsync(
        ILogStore store,
        DateOnly today,
        CancellationToken cancellationToken = default);
}
