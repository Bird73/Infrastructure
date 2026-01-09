namespace Birdsoft.Infrastructure.Logging.Json;

public sealed class JsonLoggingOptions
{
    public string RootDirectory { get; set; } = "logs";

    public int? RetentionDays { get; set; }
}
