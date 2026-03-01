using Microsoft.Extensions.Options;

namespace Birdsoft.Infrastructure.Logging.Json;

public sealed class DefaultLogFilePathProvider : ILogFilePathProvider
{
    private readonly IOptions<JsonLoggingOptions> _options;

    public DefaultLogFilePathProvider(IOptions<JsonLoggingOptions> options)
    {
        _options = options;
    }

    public string GetPath(DateOnly date)
    {
        var fileName = $"{date:yyyy-MM-dd}.jsonl";
        return Path.Combine(_options.Value.RootDirectory, fileName);
    }
}