namespace Birdsoft.Infrastructure.Logging.Abstractions;

public interface ILogFilePathProvider
{
    string GetLogFilePath(DateOnly date);
}
