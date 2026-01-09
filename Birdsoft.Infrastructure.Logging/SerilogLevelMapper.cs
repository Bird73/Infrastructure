using Birdsoft.Infrastructure.Logging.Abstractions;
using Serilog.Events;

namespace Birdsoft.Infrastructure.Logging;

internal static class SerilogLevelMapper
{
    public static LogEventLevel ToSerilogLevel(LogLevel level) => level switch
    {
        LogLevel.Trace => LogEventLevel.Verbose,
        LogLevel.Debug => LogEventLevel.Debug,
        LogLevel.Information => LogEventLevel.Information,
        LogLevel.Warning => LogEventLevel.Warning,
        LogLevel.Error => LogEventLevel.Error,
        LogLevel.Critical => LogEventLevel.Fatal,
        _ => LogEventLevel.Information,
    };
}
