namespace Birdsoft.Infrastructure.Logging.Abstractions;

public interface IAppLogger
{
    bool IsEnabled(LogLevel level);
    void Log(LogLevel level, Exception? exception, string messageTemplate, params object?[] args);
}

public interface IAppLogger<T> : IAppLogger
{
}