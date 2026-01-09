using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Birdsoft.Infrastructure.Logging.Abstractions;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Options;

namespace Birdsoft.Infrastructure.Logging.Sqlite;

public sealed class SqliteLogStore : ILogStore, ILogSink
{
    private readonly string _connectionString;
    private readonly SemaphoreSlim _initLock = new(1, 1);
    private bool _initialized;

    public SqliteLogStore(IOptions<SqliteLoggingOptions> options)
    {
        if (options is null)
        {
            throw new ArgumentNullException(nameof(options));
        }

        _connectionString = options.Value.ConnectionString;
        if (string.IsNullOrWhiteSpace(_connectionString))
        {
            throw new ArgumentException("ConnectionString is required.", nameof(options));
        }
    }

    public async Task WriteAsync(LogEntry entry, CancellationToken cancellationToken = default)
    {
        if (entry is null)
        {
            throw new ArgumentNullException(nameof(entry));
        }

        await EnsureInitializedAsync(cancellationToken).ConfigureAwait(false);

        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        var cmd = connection.CreateCommand();
        cmd.CommandText = @"
INSERT INTO Logs (Date, Timestamp, Level, Category, Message, Exception, PropertiesJson)
VALUES ($date, $timestamp, $level, $category, $message, $exception, $propertiesJson);
";

        var date = DateOnly.FromDateTime(entry.Timestamp.LocalDateTime).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        cmd.Parameters.AddWithValue("$date", date);
        cmd.Parameters.AddWithValue("$timestamp", entry.Timestamp.ToString("O", CultureInfo.InvariantCulture));
        cmd.Parameters.AddWithValue("$level", entry.Level.ToString());
        cmd.Parameters.AddWithValue("$category", entry.Category);
        cmd.Parameters.AddWithValue("$message", entry.Message);
        cmd.Parameters.AddWithValue("$exception", (object?)entry.Exception?.ToString() ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$propertiesJson", JsonSerializer.Serialize(entry.Properties));

        await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<DateOnly>> GetLogDatesAsync(CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync(cancellationToken).ConfigureAwait(false);

        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT DISTINCT Date FROM Logs ORDER BY Date ASC;";

        var dates = new List<DateOnly>();
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            var dateText = reader.GetString(0);
            if (DateOnly.TryParseExact(dateText, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
            {
                dates.Add(date);
            }
        }

        return dates;
    }

    public async IAsyncEnumerable<LogEntry> GetLogsAsync(
        DateOnly date,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync(cancellationToken).ConfigureAwait(false);

        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        var cmd = connection.CreateCommand();
        cmd.CommandText = @"
SELECT Timestamp, Level, Category, Message, Exception, PropertiesJson
FROM Logs
WHERE Date = $date
ORDER BY Timestamp ASC;
";
        cmd.Parameters.AddWithValue("$date", date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));

        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            cancellationToken.ThrowIfCancellationRequested();

            var timestampText = reader.GetString(0);
            _ = DateTimeOffset.TryParse(timestampText, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var timestamp);

            var levelText = reader.GetString(1);
            _ = Enum.TryParse<LogLevel>(levelText, ignoreCase: true, out var level);

            var category = reader.GetString(2);
            var message = reader.GetString(3);
            var exception = reader.IsDBNull(4) ? null : reader.GetString(4);
            var propertiesJson = reader.IsDBNull(5) ? null : reader.GetString(5);

            IReadOnlyDictionary<string, object?> properties = new Dictionary<string, object?>();
            if (!string.IsNullOrWhiteSpace(propertiesJson))
            {
                try
                {
                    var dict = JsonSerializer.Deserialize<Dictionary<string, object?>>(propertiesJson);
                    if (dict is not null)
                    {
                        properties = dict;
                    }
                }
                catch
                {
                    // ignore
                }
            }

            yield return new LogEntry
            {
                Timestamp = timestamp,
                Level = level,
                Category = category,
                Message = message,
                Exception = exception is null ? null : new Exception(exception),
                Properties = properties,
            };
        }
    }

    public async Task DeleteLogsAsync(DateOnly date, CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync(cancellationToken).ConfigureAwait(false);

        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        var cmd = connection.CreateCommand();
        cmd.CommandText = "DELETE FROM Logs WHERE Date = $date;";
        cmd.Parameters.AddWithValue("$date", date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));

        await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task EnsureInitializedAsync(CancellationToken cancellationToken)
    {
        if (_initialized)
        {
            return;
        }

        await _initLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_initialized)
            {
                return;
            }

            await using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

            var cmd = connection.CreateCommand();
            cmd.CommandText = @"
CREATE TABLE IF NOT EXISTS Logs (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Date TEXT NOT NULL,
    Timestamp TEXT NOT NULL,
    Level TEXT NOT NULL,
    Category TEXT NOT NULL,
    Message TEXT NOT NULL,
    Exception TEXT NULL,
    PropertiesJson TEXT NULL
);
CREATE INDEX IF NOT EXISTS IX_Logs_Date ON Logs(Date);
";

            await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
            _initialized = true;
        }
        finally
        {
            _initLock.Release();
        }
    }
}
