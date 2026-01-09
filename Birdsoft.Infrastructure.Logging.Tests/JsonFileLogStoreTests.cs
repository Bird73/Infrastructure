using Birdsoft.Infrastructure.Logging.Abstractions;
using Birdsoft.Infrastructure.Logging.Json;
using Microsoft.Extensions.Options;

namespace Birdsoft.Infrastructure.Logging.Tests;

public sealed class JsonFileLogStoreTests
{
    [Fact]
    public async Task Write_Query_Delete_Works()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), "Birdsoft.Infrastructure.Logging.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempRoot);

        try
        {
            var options = new OptionsWrapper<JsonLoggingOptions>(new JsonLoggingOptions
            {
                RootDirectory = tempRoot,
            });

            var date = new DateOnly(2026, 1, 9);
            var pathProvider = new DefaultLogFilePathProvider(tempRoot);
            var store = new JsonFileLogStore(pathProvider, options);

            await store.WriteAsync(new LogEntry
            {
                Timestamp = new DateTimeOffset(2026, 1, 9, 10, 0, 0, TimeSpan.Zero),
                Level = LogLevel.Information,
                Category = "Test",
                Message = "Hello {Name}",
                Properties = new Dictionary<string, object?> { ["Name"] = "A" },
            });

            await store.WriteAsync(new LogEntry
            {
                Timestamp = new DateTimeOffset(2026, 1, 9, 11, 0, 0, TimeSpan.Zero),
                Level = LogLevel.Warning,
                Category = "Test",
                Message = "World",
            });

            var dates = await store.GetLogDatesAsync();
            Assert.Contains(date, dates);

            var entries = new List<LogEntry>();
            await foreach (var e in store.GetLogsAsync(date))
            {
                entries.Add(e);
            }

            Assert.Equal(2, entries.Count);
            Assert.Equal("Hello {Name}", entries[0].Message);
            Assert.Equal("World", entries[1].Message);

            var filePath = pathProvider.GetLogFilePath(date);
            Assert.True(File.Exists(filePath));

            await store.DeleteLogsAsync(date);
            Assert.False(File.Exists(filePath));
        }
        finally
        {
            if (Directory.Exists(tempRoot))
            {
                Directory.Delete(tempRoot, recursive: true);
            }
        }
    }
}
