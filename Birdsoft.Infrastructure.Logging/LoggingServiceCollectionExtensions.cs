using Birdsoft.Infrastructure.Logging.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
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

        services.TryAddSingleton<Serilog.ILogger>(_ => Log.Logger);

        services.TryAddSingleton<ILogMaintenance, DefaultLogMaintenance>();

        services.TryAddTransient(typeof(IAppLogger<>), typeof(SerilogAppLogger<>));

        return services;
    }

    /// <summary>
    /// Registers an ILogger&lt;T&gt; pipeline that forwards log events into the Birdsoft logging sink/store.
    /// Use together with AddBirdsoftJsonLogging(...) or AddBirdsoftSqliteLogging(...) so ILogSink/ILogStore are available.
    /// </summary>
    public static IServiceCollection AddAppLogging(
        this IServiceCollection services,
        Action<ILoggingBuilder>? configure = null)
    {
        if (services is null)
        {
            throw new ArgumentNullException(nameof(services));
        }

        services.AddBirdsoftLoggingCore();

        services.AddLogging(builder =>
        {
            builder.ClearProviders();
            builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<ILoggerProvider, LogSinkLoggerProvider>());
            configure?.Invoke(builder);
        });

        return services;
    }
}
