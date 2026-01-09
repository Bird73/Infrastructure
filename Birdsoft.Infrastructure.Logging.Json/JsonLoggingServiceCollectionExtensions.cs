using Birdsoft.Infrastructure.Logging.Abstractions;
using Birdsoft.Infrastructure.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Birdsoft.Infrastructure.Logging.Json;

public static class JsonLoggingServiceCollectionExtensions
{
    public static IServiceCollection AddBirdsoftJsonLogging(
        this IServiceCollection services,
        Action<JsonLoggingOptions> configure)
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

        services.AddOptions<JsonLoggingOptions>().Configure(configure);

        services.TryAddSingleton<ILogFilePathProvider>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<JsonLoggingOptions>>().Value;
            var root = options.RootDirectory;

            if (!Path.IsPathRooted(root))
            {
                root = Path.Combine(AppContext.BaseDirectory, root);
            }

            return new DefaultLogFilePathProvider(root);
        });

        services.TryAddSingleton<JsonFileLogStore>();
        services.TryAddSingleton<ILogStore>(sp => sp.GetRequiredService<JsonFileLogStore>());
        services.TryAddSingleton<ILogSink>(sp => sp.GetRequiredService<JsonFileLogStore>());

        services.Replace(ServiceDescriptor.Singleton<ILogMaintenance>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<JsonLoggingOptions>>().Value;
            return new DefaultLogMaintenance
            {
                RetentionDays = options.RetentionDays,
            };
        }));

        return services;
    }
}
