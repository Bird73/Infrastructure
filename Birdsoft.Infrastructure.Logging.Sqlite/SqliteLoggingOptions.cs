namespace Birdsoft.Infrastructure.Logging.Sqlite;

public sealed class SqliteLoggingOptions
{
    public string ConnectionString { get; set; } = "Data Source=logs.db";
}