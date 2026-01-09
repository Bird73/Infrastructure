using Birdsoft.Infrastructure.Logging;
using Birdsoft.Infrastructure.Logging.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Birdsoft.Infrastructure.Logging.Sqlite;

public static class SqliteLoggingServiceCollectionExtensions
{
    public static IServiceCollection AddBirdsoftSqliteLogging(
        this IServiceCollection services,
        Action<SqliteLoggingOptions> configure)
    {
        if (services is null)
        {
            throw new ArgumentNullException(nameof(services));
        }

        if (configure is null)
        {
            throw new ArgumentNullException(nameof(configure));
        }

        services.AddBirdsoftLoggingCore();

        services.AddOptions<SqliteLoggingOptions>().Configure(configure);

        services.TryAddSingleton<SqliteLogStore>();
        services.TryAddSingleton<ILogStore>(sp => sp.GetRequiredService<SqliteLogStore>());
        services.TryAddSingleton<ILogSink>(sp => sp.GetRequiredService<SqliteLogStore>());

        services.Replace(ServiceDescriptor.Singleton<ILogMaintenance>(sp =>
        {
            var options = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<SqliteLoggingOptions>>().Value;
            return new DefaultLogMaintenance
            {
                RetentionDays = options.RetentionDays,
            };
        }));

        return services;
    }
}
