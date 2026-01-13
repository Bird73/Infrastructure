namespace Birdsoft.Infrastructure.Logging.Tests;

using Birdsoft.Infrastructure.Logging.Abstractions;
using Birdsoft.Infrastructure.Logging.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

public sealed class MicrosoftLoggerToStoreTests
{
    [Fact]
    public async Task ILogger_Writes_To_Store_Via_AddAppLogging_And_JsonSink()
    {
        var root = Path.Combine(Path.GetTempPath(), "Birdsoft.Infrastructure.Logging.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);

        try
        {
            var services = new ServiceCollection();
            services.AddBirdsoftJsonLogging(o =>
            {
                o.RootDirectory = root;
                o.RetentionDays = 7;
            });

            services.AddAppLogging();

            using var provider = services.BuildServiceProvider();

            var logger = provider.GetRequiredService<ILogger<MicrosoftLoggerToStoreTests>>();
            logger.LogInformation("hello {Value}", 123);

            var store = provider.GetRequiredService<ILogStore>();
            var today = DateOnly.FromDateTime(DateTimeOffset.Now.DateTime);

            var dates = await store.GetLogDatesAsync();
            Assert.Contains(today, dates);

            var found = false;
            await foreach (var entry in store.GetLogsAsync(today))
            {
                if (entry.Category.Contains(nameof(MicrosoftLoggerToStoreTests), StringComparison.Ordinal)
                    && entry.Message.Contains("hello", StringComparison.OrdinalIgnoreCase))
                {
                    found = true;
                    break;
                }
            }

            Assert.True(found);
        }
        finally
        {
            try
            {
                Directory.Delete(root, recursive: true);
            }
            catch
            {
                // best-effort cleanup on Windows file locks
            }
        }
    }
}
