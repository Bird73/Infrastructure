namespace Birdsoft.Infrastructure.Logging.Abstractions;

public interface ILogSink
{
    Task WriteAsync(LogEntry entry, CancellationToken cancellationToken = default);
}