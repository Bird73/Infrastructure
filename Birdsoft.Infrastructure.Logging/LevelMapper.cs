using Birdsoft.Infrastructure.Logging.Abstractions;
using Serilog.Events;
using MsLogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace Birdsoft.Infrastructure.Logging;

public static class LevelMapper
{
    public static LogEventLevel ToSerilogLevel(LogLevel level)
    {
        return level switch
        {
            LogLevel.Trace => LogEventLevel.Verbose,
            LogLevel.Debug => LogEventLevel.Debug,
            LogLevel.Information => LogEventLevel.Information,
            LogLevel.Warning => LogEventLevel.Warning,
            LogLevel.Error => LogEventLevel.Error,
            LogLevel.Critical => LogEventLevel.Fatal,
            _ => LogEventLevel.Information
        };
    }

    public static LogLevel ToAppLevel(LogEventLevel level)
    {
        return level switch
        {
            LogEventLevel.Verbose => LogLevel.Trace,
            LogEventLevel.Debug => LogLevel.Debug,
            LogEventLevel.Information => LogLevel.Information,
            LogEventLevel.Warning => LogLevel.Warning,
            LogEventLevel.Error => LogLevel.Error,
            LogEventLevel.Fatal => LogLevel.Critical,
            _ => LogLevel.Information
        };
    }

    public static MsLogLevel ToMicrosoftLevel(LogLevel level)
    {
        return level switch
        {
            LogLevel.Trace => MsLogLevel.Trace,
            LogLevel.Debug => MsLogLevel.Debug,
            LogLevel.Information => MsLogLevel.Information,
            LogLevel.Warning => MsLogLevel.Warning,
            LogLevel.Error => MsLogLevel.Error,
            LogLevel.Critical => MsLogLevel.Critical,
            LogLevel.None => MsLogLevel.None,
            _ => MsLogLevel.Information
        };
    }

    public static LogLevel ToAppLevel(MsLogLevel level)
    {
        return level switch
        {
            MsLogLevel.Trace => LogLevel.Trace,
            MsLogLevel.Debug => LogLevel.Debug,
            MsLogLevel.Information => LogLevel.Information,
            MsLogLevel.Warning => LogLevel.Warning,
            MsLogLevel.Error => LogLevel.Error,
            MsLogLevel.Critical => LogLevel.Critical,
            MsLogLevel.None => LogLevel.None,
            _ => LogLevel.Information
        };
    }
}