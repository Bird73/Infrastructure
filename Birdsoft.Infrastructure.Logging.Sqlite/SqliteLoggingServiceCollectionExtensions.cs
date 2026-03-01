using Birdsoft.Infrastructure.Logging;
using Birdsoft.Infrastructure.Logging.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Birdsoft.Infrastructure.Logging.Sqlite;

public static class SqliteLoggingServiceCollectionExtensions
{
    public static IServiceCollection AddBirdsoftSqliteLogging(
        this IServiceCollection services,
        Action<SqliteLoggingOptions>? configureSqlite = null,
        Action<LoggingOptions>? configureLogging = null)
    {
        services.AddBirdsoftLoggingCore(configureLogging);

        if (configureSqlite is not null)
        {
            services.Configure(configureSqlite);
        }
        else
        {
            services.Configure<SqliteLoggingOptions>(_ => { });
        }

        services.TryAddSingleton<SqliteLogStore>();
        services.TryAddSingleton<ILogStore>(sp => sp.GetRequiredService<SqliteLogStore>());
        services.TryAddSingleton<ILogSink>(sp => sp.GetRequiredService<SqliteLogStore>());
        return services;
    }
}