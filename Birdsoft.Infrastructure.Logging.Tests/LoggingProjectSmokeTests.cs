using Birdsoft.Infrastructure.Logging.Abstractions;
using Birdsoft.Infrastructure.Logging.Json;
using Birdsoft.Infrastructure.Logging.Sqlite;
using Microsoft.Extensions.DependencyInjection;

namespace Birdsoft.Infrastructure.Logging.Tests;

public sealed class LoggingProjectSmokeTests
{
    [Fact]
    public void JsonRegistration_ShouldResolveCoreServices()
    {
        var services = new ServiceCollection();
        services.AddBirdsoftJsonLogging();
        using var provider = services.BuildServiceProvider();

        Assert.NotNull(provider.GetService<ILogSink>());
        Assert.NotNull(provider.GetService<ILogStore>());
        Assert.NotNull(provider.GetService<ILogMaintenance>());
    }

    [Fact]
    public void SqliteRegistration_ShouldResolveCoreServices()
    {
        var services = new ServiceCollection();
        services.AddBirdsoftSqliteLogging(options => options.ConnectionString = "Data Source=:memory:");
        using var provider = services.BuildServiceProvider();

        Assert.NotNull(provider.GetService<ILogSink>());
        Assert.NotNull(provider.GetService<ILogStore>());
        Assert.NotNull(provider.GetService<ILogMaintenance>());
    }
}