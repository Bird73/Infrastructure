namespace Birdsoft.Infrastructure.Logging.Tests;

using Birdsoft.Infrastructure.Logging.Abstractions;
using Microsoft.Extensions.DependencyInjection;

public sealed class LoggingProjectSmokeTests
{
    [Fact]
    public void AddBirdsoftLoggingCore_Registers_Core_Services_And_Can_Log()
    {
        var services = new ServiceCollection();
        services.AddBirdsoftLoggingCore();

        using var provider = services.BuildServiceProvider();

        var maintenance = provider.GetRequiredService<ILogMaintenance>();
        Assert.NotNull(maintenance);

        var logger = provider.GetRequiredService<IAppLogger<LoggingProjectSmokeTests>>();
        Assert.NotNull(logger);

        logger.Log(LogLevel.Information, exception: null, messageTemplate: "logging-smoke-test");
    }
}
