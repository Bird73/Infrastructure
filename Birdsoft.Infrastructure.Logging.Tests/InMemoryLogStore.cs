using Birdsoft.Infrastructure.Logging.Abstractions;

namespace Birdsoft.Infrastructure.Logging.Tests;

internal sealed class InMemoryLogStore : ILogStore, ILogSink
{
    private readonly List<LogEntry> _entries = [];

    public Task WriteAsync(LogEntry entry, CancellationToken cancellationToken = default)
    {
        _entries.Add(entry);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<DateOnly>> GetLogDatesAsync(CancellationToken ct = default)
    {
        var dates = _entries
            .Select(x => DateOnly.FromDateTime(x.Timestamp.UtcDateTime))
            .Distinct()
            .OrderBy(x => x)
            .ToArray();

        return Task.FromResult<IReadOnlyList<DateOnly>>(dates);
    }

    public async IAsyncEnumerable<LogEntry> GetLogsAsync(LogQuery query, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
    {
        IEnumerable<LogEntry> items = _entries.Where(x => DateOnly.FromDateTime(x.Timestamp.UtcDateTime) == query.Date);

        if (query.MinLevel.HasValue)
        {
            items = items.Where(x => x.Level >= query.MinLevel.Value);
        }

        items = query.OrderByTimestampDescending
            ? items.OrderByDescending(x => x.Timestamp)
            : items.OrderBy(x => x.Timestamp);

        foreach (var item in items)
        {
            yield return item;
            await Task.Yield();
        }
    }

    public Task DeleteLogsAsync(DateOnly date, CancellationToken ct = default)
    {
        _entries.RemoveAll(x => DateOnly.FromDateTime(x.Timestamp.UtcDateTime) == date);
        return Task.CompletedTask;
    }

    public IReadOnlyList<LogEntry> Entries => _entries;
}