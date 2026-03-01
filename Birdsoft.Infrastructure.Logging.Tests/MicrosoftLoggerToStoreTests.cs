using Birdsoft.Infrastructure.Logging.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Birdsoft.Infrastructure.Logging.Tests;

public sealed class MicrosoftLoggerToStoreTests
{
    [Fact]
    public async Task EndToEnd_ILoggerToStore_ShouldPersistAndQuery()
    {
        var root = Path.Combine(Path.GetTempPath(), "birdsoft-e2e", Guid.NewGuid().ToString("N"));
        var services = new ServiceCollection();
        services.AddBirdsoftJsonLogging(
            configureJson: options => options.RootDirectory = root,
            configureLogging: options => options.RetentionDays = 7);

        services.AddLogging(builder => builder.AddAppLogging());
        using var provider = services.BuildServiceProvider();
        var logger = provider.GetRequiredService<ILogger<MicrosoftLoggerToStoreTests>>();
        var store = provider.GetRequiredService<Abstractions.ILogStore>();

        logger.LogError("E2E {Code}", 501);

        var date = DateOnly.FromDateTime(DateTime.UtcNow);
        var entries = new List<Abstractions.LogEntry>();
        await foreach (var entry in store.GetLogsAsync(new Abstractions.LogQuery { Date = date }))
        {
            entries.Add(entry);
        }

        Assert.NotEmpty(entries);
    }
}