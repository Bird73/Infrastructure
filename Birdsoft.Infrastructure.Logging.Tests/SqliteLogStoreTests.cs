using Birdsoft.Infrastructure.Logging.Abstractions;
using Birdsoft.Infrastructure.Logging.Sqlite;
using Microsoft.Extensions.Options;

namespace Birdsoft.Infrastructure.Logging.Tests;

public sealed class SqliteLogStoreTests
{
    [Fact]
    public async Task GetLogsAsync_ShouldFilterByMinLevel_AndOrder()
    {
        await using var store = new SqliteLogStore(Options.Create(new SqliteLoggingOptions { ConnectionString = "Data Source=:memory:" }));
        var day = DateOnly.FromDateTime(DateTime.UtcNow);

        await store.WriteAsync(new LogEntry
        {
            Timestamp = day.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc).AddHours(1),
            Level = LogLevel.Warning,
            Category = "S",
            MessageTemplate = "warn",
            RenderedMessage = "warn",
            Properties = new Dictionary<string, object?>()
        });

        await store.WriteAsync(new LogEntry
        {
            Timestamp = day.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc).AddHours(2),
            Level = LogLevel.Error,
            Category = "S",
            MessageTemplate = "err",
            RenderedMessage = "err",
            Properties = new Dictionary<string, object?>()
        });

        var list = new List<LogEntry>();
        await foreach (var item in store.GetLogsAsync(new LogQuery
                       {
                           Date = day,
                           MinLevel = LogLevel.Error,
                           OrderByTimestampDescending = true
                       }))
        {
            list.Add(item);
        }

        Assert.Single(list);
        Assert.Equal(LogLevel.Error, list[0].Level);
    }
}