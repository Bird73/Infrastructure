using Birdsoft.Infrastructure.Logging.Abstractions;
using Birdsoft.Infrastructure.Logging.Json;
using Microsoft.Extensions.Options;

namespace Birdsoft.Infrastructure.Logging.Tests;

public sealed class JsonFileLogStoreTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "birdsoft-json-tests", Guid.NewGuid().ToString("N"));

    [Fact]
    public async Task GetLogsAsync_ShouldSkipInvalidJsonLine()
    {
        var pathProvider = new DefaultLogFilePathProvider(Options.Create(new JsonLoggingOptions { RootDirectory = _root }));
        var store = new JsonFileLogStore(pathProvider);
        var date = DateOnly.FromDateTime(DateTime.UtcNow);
        var path = pathProvider.GetPath(date);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);

        var validEntry = new LogEntry
        {
            Timestamp = DateTimeOffset.UtcNow,
            Level = LogLevel.Error,
            Category = "Json",
            MessageTemplate = "A",
            RenderedMessage = "A",
            Properties = new Dictionary<string, object?>()
        };

        await File.WriteAllTextAsync(path, System.Text.Json.JsonSerializer.Serialize(validEntry) + Environment.NewLine + "{bad-json");

        var result = new List<LogEntry>();
        await foreach (var item in store.GetLogsAsync(new LogQuery { Date = date }))
        {
            result.Add(item);
        }

        Assert.Single(result);
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, true);
        }
    }
}