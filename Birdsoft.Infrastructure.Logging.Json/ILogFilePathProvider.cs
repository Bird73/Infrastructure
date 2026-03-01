namespace Birdsoft.Infrastructure.Logging.Json;

public interface ILogFilePathProvider
{
    string GetPath(DateOnly date);
}