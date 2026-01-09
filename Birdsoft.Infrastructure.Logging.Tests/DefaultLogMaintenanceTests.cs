using Birdsoft.Infrastructure.Logging;
using Birdsoft.Infrastructure.Logging.Abstractions;

namespace Birdsoft.Infrastructure.Logging.Tests;

public sealed class DefaultLogMaintenanceTests
{
    [Fact]
    public async Task Retention_And_Explicit_Delete_Are_Applied()
    {
        var store = new FakeStore(new[]
        {
            new DateOnly(2026, 1, 4),
            new DateOnly(2026, 1, 5),
            new DateOnly(2026, 1, 6),
            new DateOnly(2026, 1, 7),
        });

        var maintenance = new DefaultLogMaintenance
        {
            RetentionDays = 3,
        };
        maintenance.AddExplicitDeleteDate(new DateOnly(2026, 1, 7));

        await maintenance.ExecuteAsync(store, today: new DateOnly(2026, 1, 9));

        // RetentionDays=3 => cutoff = 2026-01-06, delete dates < cutoff
        Assert.Contains(new DateOnly(2026, 1, 4), store.Deleted);
        Assert.Contains(new DateOnly(2026, 1, 5), store.Deleted);
        Assert.DoesNotContain(new DateOnly(2026, 1, 6), store.Deleted);

        // Explicit
        Assert.Contains(new DateOnly(2026, 1, 7), store.Deleted);
    }

    private sealed class FakeStore : ILogStore
    {
        private readonly IReadOnlyList<DateOnly> _dates;

        public FakeStore(IReadOnlyList<DateOnly> dates)
        {
            _dates = dates;
        }

        public List<DateOnly> Deleted { get; } = new();

        public Task<IReadOnlyList<DateOnly>> GetLogDatesAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(_dates);

        public IAsyncEnumerable<LogEntry> GetLogsAsync(DateOnly date, CancellationToken cancellationToken = default)
            => AsyncEnumerable.Empty<LogEntry>();

        public Task DeleteLogsAsync(DateOnly date, CancellationToken cancellationToken = default)
        {
            Deleted.Add(date);
            return Task.CompletedTask;
        }
    }
}
