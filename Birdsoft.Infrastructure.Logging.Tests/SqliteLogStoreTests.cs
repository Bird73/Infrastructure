using Birdsoft.Infrastructure.Logging.Abstractions;
using Birdsoft.Infrastructure.Logging.Sqlite;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Options;

namespace Birdsoft.Infrastructure.Logging.Tests;

public sealed class SqliteLogStoreTests
{
    [Fact]
    public async Task Write_Query_Delete_Works()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), "Birdsoft.Infrastructure.Logging.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempRoot);

        var dbPath = Path.Combine(tempRoot, "logs.db");

        try
        {
            var options = new OptionsWrapper<SqliteLoggingOptions>(new SqliteLoggingOptions
            {
                ConnectionString = $"Data Source={dbPath};Pooling=False",
            });

            var store = new SqliteLogStore(options);
            var date = new DateOnly(2026, 1, 9);

            await store.WriteAsync(new LogEntry
            {
                Timestamp = new DateTimeOffset(2026, 1, 9, 12, 0, 0, TimeSpan.Zero),
                Level = LogLevel.Error,
                Category = "Test",
                Message = "Db test",
            });

            var dates = await store.GetLogDatesAsync();
            Assert.Contains(date, dates);

            var entries = new List<LogEntry>();
            await foreach (var e in store.GetLogsAsync(date))
            {
                entries.Add(e);
            }

            Assert.Single(entries);
            Assert.Equal("Db test", entries[0].Message);

            await store.DeleteLogsAsync(date);

            var datesAfter = await store.GetLogDatesAsync();
            Assert.DoesNotContain(date, datesAfter);
        }
        finally
        {
            SqliteConnection.ClearAllPools();

            if (Directory.Exists(tempRoot))
            {
                Directory.Delete(tempRoot, recursive: true);
            }
        }
    }
}
