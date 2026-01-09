using Birdsoft.Infrastructure.Logging.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Serilog;

namespace Birdsoft.Infrastructure.Logging;

public static class LoggingServiceCollectionExtensions
{
    public static IServiceCollection AddBirdsoftLoggingCore(this IServiceCollection services)
    {
        if (services is null)
        {
            throw new ArgumentNullException(nameof(services));
        }

        services.TryAddSingleton<ILogger>(_ => Log.Logger);

        services.TryAddSingleton<ILogMaintenance, DefaultLogMaintenance>();

        services.TryAddTransient(typeof(IAppLogger<>), typeof(SerilogAppLogger<>));

        return services;
    }
}
