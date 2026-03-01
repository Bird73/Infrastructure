using Birdsoft.Infrastructure.Logging;
using Birdsoft.Infrastructure.Logging.Abstractions;
using Serilog;

namespace Birdsoft.Infrastructure.Logging.Tests;

public sealed class SerilogAppLoggerTests
{
    [Fact]
    public void Log_ShouldWriteToSink_WithRedactedContent()
    {
        var sink = new InMemoryLogStore();
        var logger = new LoggerConfiguration().MinimumLevel.Verbose().CreateLogger();
        var appLogger = new SerilogAppLogger<SerilogAppLoggerTests>(logger, sink);

        appLogger.Log(LogLevel.Error, null, "Token {Token}", "access_token=abc");

        var entry = Assert.Single(sink.Entries);
        Assert.Equal(LogLevel.Error, entry.Level);
        Assert.Contains("[REDACTED]", entry.RenderedMessage);
        Assert.DoesNotContain("abc", entry.RenderedMessage);
    }
}