using Birdsoft.Infrastructure.Logging.Abstractions;

namespace Birdsoft.Infrastructure.Logging;

public sealed class DefaultLogMaintenance : ILogMaintenance
{
    private readonly HashSet<DateOnly> _explicitDeleteDates = new();

    public int? RetentionDays { get; set; }

    public IReadOnlyCollection<DateOnly> ExplicitDeleteDates => _explicitDeleteDates;

    public void AddExplicitDeleteDate(DateOnly date) => _explicitDeleteDates.Add(date);

    public bool RemoveExplicitDeleteDate(DateOnly date) => _explicitDeleteDates.Remove(date);

    public async Task ExecuteAsync(ILogStore store, DateOnly today, CancellationToken cancellationToken = default)
    {
        if (store is null)
        {
            throw new ArgumentNullException(nameof(store));
        }

        var existingDates = await store.GetLogDatesAsync(cancellationToken).ConfigureAwait(false);

        var datesToDelete = new HashSet<DateOnly>(_explicitDeleteDates);

        if (RetentionDays is int retentionDays)
        {
            var cutoff = today.AddDays(-retentionDays);
            foreach (var d in existingDates)
            {
                if (d < cutoff)
                {
                    datesToDelete.Add(d);
                }
            }
        }

        foreach (var d in datesToDelete)
        {
            await store.DeleteLogsAsync(d, cancellationToken).ConfigureAwait(false);
        }
    }
}
