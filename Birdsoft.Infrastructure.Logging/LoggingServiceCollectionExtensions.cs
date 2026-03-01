using Birdsoft.Infrastructure.Logging.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Serilog;

namespace Birdsoft.Infrastructure.Logging;

public static class LoggingServiceCollectionExtensions
{
    public static IServiceCollection AddBirdsoftLoggingCore(this IServiceCollection services, Action<LoggingOptions>? configure = null)
    {
        if (configure is not null)
        {
            services.Configure(configure);
        }
        else
        {
            services.Configure<LoggingOptions>(_ => { });
        }

        services.TryAddSingleton<Serilog.ILogger>(_ => new LoggerConfiguration().MinimumLevel.Verbose().CreateLogger());
        services.TryAddSingleton<ILogMaintenance, DefaultLogMaintenance>();
        services.TryAddSingleton(typeof(IAppLogger<>), typeof(SerilogAppLogger<>));

        return services;
    }

    public static ILoggingBuilder AddAppLogging(this ILoggingBuilder builder, bool clearExistingProviders = false, Action<ILoggingBuilder>? configure = null)
    {
        if (clearExistingProviders)
        {
            builder.ClearProviders();
        }

        builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<ILoggerProvider, LogSinkLoggerProvider>());
        configure?.Invoke(builder);
        return builder;
    }
}