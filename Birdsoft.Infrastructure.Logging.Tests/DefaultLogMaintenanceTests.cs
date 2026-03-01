using Birdsoft.Infrastructure.Logging;
using Birdsoft.Infrastructure.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Birdsoft.Infrastructure.Logging.Tests;

public sealed class DefaultLogMaintenanceTests
{
    [Fact]
    public async Task ExecuteAsync_ShouldDeleteOldDates_ByRetention()
    {
        var store = new InMemoryLogStore();
        var now = DateTimeOffset.UtcNow;
        await store.WriteAsync(new LogEntry
        {
            Timestamp = now.AddDays(-10),
            Level = LogLevel.Error,
            Category = "A",
            MessageTemplate = "Old",
            RenderedMessage = "Old",
            Properties = new Dictionary<string, object?>()
        });

        await store.WriteAsync(new LogEntry
        {
            Timestamp = now.AddDays(-1),
            Level = LogLevel.Error,
            Category = "A",
            MessageTemplate = "New",
            RenderedMessage = "New",
            Properties = new Dictionary<string, object?>()
        });

        var options = Options.Create(new LoggingOptions { RetentionDays = 3 });
        var maintenance = new DefaultLogMaintenance(options);

        await maintenance.ExecuteAsync(store, DateOnly.FromDateTime(now.UtcDateTime));

        var remainingDates = await store.GetLogDatesAsync();
        Assert.Single(remainingDates);
    }
}