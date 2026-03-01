using Birdsoft.Infrastructure.Logging;
using Microsoft.Extensions.Logging;

namespace Birdsoft.Infrastructure.Logging.Tests;

public sealed class LogSinkLoggerProviderTests
{
    [Fact]
    public void Logger_ShouldCaptureOriginalFormat_AndStateProperties()
    {
        var sink = new InMemoryLogStore();
        using var provider = new LogSinkLoggerProvider(sink);
        var logger = provider.CreateLogger("TestCategory");

        logger.LogInformation("Hello {User}", "Bird");

        var entry = Assert.Single(sink.Entries);
        Assert.Equal("Hello {User}", entry.MessageTemplate);
        Assert.Equal("Bird", entry.Properties["User"]?.ToString());
    }
}