using System.Collections.Concurrent;
using Birdsoft.Infrastructure.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Birdsoft.Infrastructure.Logging;

public sealed class DefaultLogMaintenance : ILogMaintenance
{
    private readonly ConcurrentDictionary<DateOnly, byte> _explicitDeleteDates = new();
    private readonly IOptions<LoggingOptions> _options;

    public DefaultLogMaintenance(IOptions<LoggingOptions> options)
    {
        _options = options;
    }

    public IReadOnlyCollection<DateOnly> ExplicitDeleteDates => _explicitDeleteDates.Keys.ToArray();

    public void AddExplicitDeleteDate(DateOnly date)
    {
        _explicitDeleteDates.TryAdd(date, 0);
    }

    public bool RemoveExplicitDeleteDate(DateOnly date)
    {
        return _explicitDeleteDates.TryRemove(date, out _);
    }

    public async Task ExecuteAsync(ILogStore store, DateOnly today, CancellationToken ct = default)
    {
        var retentionDays = _options.Value.RetentionDays;
        var allDates = await store.GetLogDatesAsync(ct);

        if (retentionDays.HasValue && retentionDays.Value >= 0)
        {
            var threshold = today.AddDays(-retentionDays.Value);
            foreach (var date in allDates.Where(d => d < threshold))
            {
                await store.DeleteLogsAsync(date, ct);
            }
        }

        foreach (var date in _explicitDeleteDates.Keys)
        {
            await store.DeleteLogsAsync(date, ct);
            _explicitDeleteDates.TryRemove(date, out _);
        }
    }
}