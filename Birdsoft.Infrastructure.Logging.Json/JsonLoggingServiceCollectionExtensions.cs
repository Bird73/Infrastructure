using Birdsoft.Infrastructure.Logging;
using Birdsoft.Infrastructure.Logging.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Birdsoft.Infrastructure.Logging.Json;

public static class JsonLoggingServiceCollectionExtensions
{
    public static IServiceCollection AddBirdsoftJsonLogging(
        this IServiceCollection services,
        Action<JsonLoggingOptions>? configureJson = null,
        Action<LoggingOptions>? configureLogging = null)
    {
        services.AddBirdsoftLoggingCore(configureLogging);

        if (configureJson is not null)
        {
            services.Configure(configureJson);
        }
        else
        {
            services.Configure<JsonLoggingOptions>(_ => { });
        }

        services.TryAddSingleton<ILogFilePathProvider, DefaultLogFilePathProvider>();
        services.TryAddSingleton<JsonFileLogStore>();
        services.TryAddSingleton<ILogStore>(sp => sp.GetRequiredService<JsonFileLogStore>());
        services.TryAddSingleton<ILogSink>(sp => sp.GetRequiredService<JsonFileLogStore>());
        return services;
    }
}