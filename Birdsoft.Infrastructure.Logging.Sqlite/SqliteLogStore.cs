using System.Data;
using System.Text.Json;
using Birdsoft.Infrastructure.Logging.Abstractions;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Options;

namespace Birdsoft.Infrastructure.Logging.Sqlite;

public sealed class SqliteLogStore : ILogStore, ILogSink, IDisposable, IAsyncDisposable
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    private readonly SqliteConnection _connection;
    private readonly SemaphoreSlim _dbLock = new(1, 1);
    private bool _disposed;

    public SqliteLogStore(IOptions<SqliteLoggingOptions> options)
    {
        _connection = new SqliteConnection(options.Value.ConnectionString);
        _connection.Open();
        InitializeSchema();
    }

    public async Task WriteAsync(LogEntry entry, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        await _dbLock.WaitAsync(cancellationToken);
        try
        {
            await using var cmd = _connection.CreateCommand();
            cmd.CommandText = @"
INSERT INTO Logs (Date, Timestamp, Level, Category, MessageTemplate, RenderedMessage, ExceptionDetail, PropertiesJson)
VALUES ($date, $timestamp, $level, $category, $template, $rendered, $exception, $properties);";
            cmd.Parameters.AddWithValue("$date", DateOnly.FromDateTime(entry.Timestamp.UtcDateTime).ToString("yyyy-MM-dd"));
            cmd.Parameters.AddWithValue("$timestamp", entry.Timestamp.UtcDateTime.ToString("O"));
            cmd.Parameters.AddWithValue("$level", (int)entry.Level);
            cmd.Parameters.AddWithValue("$category", entry.Category);
            cmd.Parameters.AddWithValue("$template", entry.MessageTemplate);
            cmd.Parameters.AddWithValue("$rendered", entry.RenderedMessage);
            cmd.Parameters.AddWithValue("$exception", (object?)entry.ExceptionDetail ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$properties", JsonSerializer.Serialize(entry.Properties, SerializerOptions));
            await cmd.ExecuteNonQueryAsync(cancellationToken);
        }
        finally
        {
            _dbLock.Release();
        }
    }

    public async Task<IReadOnlyList<DateOnly>> GetLogDatesAsync(CancellationToken ct = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        await _dbLock.WaitAsync(ct);
        try
        {
            await using var cmd = _connection.CreateCommand();
            cmd.CommandText = "SELECT DISTINCT Date FROM Logs ORDER BY Date;";

            var dates = new List<DateOnly>();
            await using var reader = await cmd.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
            {
                var text = reader.GetString(0);
                if (DateOnly.TryParse(text, out var date))
                {
                    dates.Add(date);
                }
            }

            return dates;
        }
        finally
        {
            _dbLock.Release();
        }
    }

    public async IAsyncEnumerable<LogEntry> GetLogsAsync(LogQuery query, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        var rows = new List<LogEntry>();

        await _dbLock.WaitAsync(ct);
        try
        {
            await using var cmd = _connection.CreateCommand();
            var whereClauses = new List<string> { "Date = $date" };
            cmd.Parameters.AddWithValue("$date", query.Date.ToString("yyyy-MM-dd"));

            if (query.MinLevel.HasValue)
            {
                whereClauses.Add("Level >= $minLevel");
                cmd.Parameters.AddWithValue("$minLevel", (int)query.MinLevel.Value);
            }

            if (!string.IsNullOrWhiteSpace(query.Category))
            {
                whereClauses.Add("Category LIKE $category");
                cmd.Parameters.AddWithValue("$category", $"%{query.Category}%");
            }

            if (!string.IsNullOrWhiteSpace(query.Keyword))
            {
                whereClauses.Add("RenderedMessage LIKE $keyword");
                cmd.Parameters.AddWithValue("$keyword", $"%{query.Keyword}%");
            }

            var order = query.OrderByTimestampDescending ? "DESC" : "ASC";
            cmd.CommandText = $@"
SELECT Timestamp, Level, Category, MessageTemplate, RenderedMessage, ExceptionDetail, PropertiesJson
FROM Logs
WHERE {string.Join(" AND ", whereClauses)}
ORDER BY Timestamp {order};";

            await using var reader = await cmd.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
            {
                var propertiesRaw = reader.IsDBNull(6) ? "{}" : reader.GetString(6);
                var properties = JsonSerializer.Deserialize<Dictionary<string, object?>>(propertiesRaw, SerializerOptions)
                                 ?? new Dictionary<string, object?>();

                rows.Add(new LogEntry
                {
                    Timestamp = DateTimeOffset.Parse(reader.GetString(0)),
                    Level = (Abstractions.LogLevel)reader.GetInt32(1),
                    Category = reader.GetString(2),
                    MessageTemplate = reader.GetString(3),
                    RenderedMessage = reader.GetString(4),
                    ExceptionDetail = reader.IsDBNull(5) ? null : reader.GetString(5),
                    Properties = properties
                });
            }
        }
        finally
        {
            _dbLock.Release();
        }

        foreach (var row in rows)
        {
            yield return row;
        }
    }

    public async Task DeleteLogsAsync(DateOnly date, CancellationToken ct = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        await _dbLock.WaitAsync(ct);
        try
        {
            await using var cmd = _connection.CreateCommand();
            cmd.CommandText = "DELETE FROM Logs WHERE Date = $date;";
            cmd.Parameters.AddWithValue("$date", date.ToString("yyyy-MM-dd"));
            await cmd.ExecuteNonQueryAsync(ct);
        }
        finally
        {
            _dbLock.Release();
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _dbLock.Dispose();
        _connection.Dispose();
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _dbLock.Dispose();
        await _connection.DisposeAsync();
    }

    private void InitializeSchema()
    {
        using var pragma = _connection.CreateCommand();
        pragma.CommandText = "PRAGMA journal_mode=WAL;";
        pragma.ExecuteNonQuery();

        using var create = _connection.CreateCommand();
        create.CommandText = @"
CREATE TABLE IF NOT EXISTS Logs (
    Id               INTEGER PRIMARY KEY AUTOINCREMENT,
    Date             TEXT    NOT NULL,
    Timestamp        TEXT    NOT NULL,
    Level            INTEGER NOT NULL,
    Category         TEXT    NOT NULL,
    MessageTemplate  TEXT    NOT NULL,
    RenderedMessage  TEXT    NOT NULL,
    ExceptionDetail  TEXT    NULL,
    PropertiesJson   TEXT    NULL
);
CREATE INDEX IF NOT EXISTS IX_Logs_Date ON Logs(Date);
CREATE INDEX IF NOT EXISTS IX_Logs_Level ON Logs(Level);";
        create.ExecuteNonQuery();
    }
}