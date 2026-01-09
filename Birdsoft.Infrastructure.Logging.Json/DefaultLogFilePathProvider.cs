using System.Globalization;
using Birdsoft.Infrastructure.Logging.Abstractions;

namespace Birdsoft.Infrastructure.Logging.Json;

public sealed class DefaultLogFilePathProvider : ILogFilePathProvider
{
    private readonly string _rootDirectory;

    public DefaultLogFilePathProvider(string rootDirectory)
    {
        if (string.IsNullOrWhiteSpace(rootDirectory))
        {
            throw new ArgumentException("Root directory is required.", nameof(rootDirectory));
        }

        _rootDirectory = rootDirectory;
    }

    public string GetLogFilePath(DateOnly date)
    {
        var fileName = date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) + ".jsonl";
        return Path.Combine(_rootDirectory, fileName);
    }
}
