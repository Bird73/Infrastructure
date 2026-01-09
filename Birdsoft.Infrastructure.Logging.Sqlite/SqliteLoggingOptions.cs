namespace Birdsoft.Infrastructure.Logging.Sqlite;

public sealed class SqliteLoggingOptions
{
    public string ConnectionString { get; set; } = string.Empty;

    public int? RetentionDays { get; set; }
}
