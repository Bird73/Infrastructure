using System.Text.Json;
using Birdsoft.Infrastructure.Logging.Abstractions;

namespace Birdsoft.Infrastructure.Logging.Json;

public sealed class JsonFileLogStore : ILogStore, ILogSink, IDisposable
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = false
    };

    private readonly ILogFilePathProvider _pathProvider;
    private readonly SemaphoreSlim _writeLock = new(1, 1);
    private bool _disposed;

    public JsonFileLogStore(ILogFilePathProvider pathProvider)
    {
        _pathProvider = pathProvider;
    }

    public async Task WriteAsync(LogEntry entry, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var date = DateOnly.FromDateTime(entry.Timestamp.UtcDateTime);
        var path = _pathProvider.GetPath(date);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var line = JsonSerializer.Serialize(entry, SerializerOptions);

        await _writeLock.WaitAsync(cancellationToken);
        try
        {
            await File.AppendAllTextAsync(path, line + Environment.NewLine, cancellationToken);
        }
        finally
        {
            _writeLock.Release();
        }
    }

    public async Task<IReadOnlyList<DateOnly>> GetLogDatesAsync(CancellationToken ct = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (_pathProvider is not DefaultLogFilePathProvider defaultProvider)
        {
            return [];
        }

        var root = Path.GetDirectoryName(defaultProvider.GetPath(DateOnly.FromDateTime(DateTime.UtcNow)))!;
        if (!Directory.Exists(root))
        {
            return [];
        }

        var dates = new List<DateOnly>();
        foreach (var file in Directory.EnumerateFiles(root, "*.jsonl"))
        {
            ct.ThrowIfCancellationRequested();
            var fileName = Path.GetFileNameWithoutExtension(file);
            if (DateOnly.TryParse(fileName, out var date))
            {
                dates.Add(date);
            }
        }

        return dates.OrderBy(x => x).ToArray();
    }

    public async IAsyncEnumerable<LogEntry> GetLogsAsync(LogQuery query, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        var path = _pathProvider.GetPath(query.Date);
        if (!File.Exists(path))
        {
            yield break;
        }

        var entries = new List<LogEntry>();
        using var stream = File.OpenRead(path);
        using var reader = new StreamReader(stream);
        while (true)
        {
            ct.ThrowIfCancellationRequested();
            var line = await reader.ReadLineAsync(ct);
            if (line is null)
            {
                break;
            }

            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            try
            {
                var entry = JsonSerializer.Deserialize<LogEntry>(line, SerializerOptions);
                if (entry is not null)
                {
                    entries.Add(entry);
                }
            }
            catch (JsonException)
            {
            }
        }

        IEnumerable<LogEntry> filtered = entries;
        if (query.MinLevel.HasValue)
        {
            filtered = filtered.Where(x => x.Level >= query.MinLevel.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.Category))
        {
            filtered = filtered.Where(x => x.Category.Contains(query.Category, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(query.Keyword))
        {
            filtered = filtered.Where(x => x.RenderedMessage.Contains(query.Keyword, StringComparison.OrdinalIgnoreCase));
        }

        filtered = query.OrderByTimestampDescending
            ? filtered.OrderByDescending(x => x.Timestamp)
            : filtered.OrderBy(x => x.Timestamp);

        foreach (var entry in filtered)
        {
            yield return entry;
        }
    }

    public Task DeleteLogsAsync(DateOnly date, CancellationToken ct = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        var path = _pathProvider.GetPath(date);
        if (File.Exists(path))
        {
            File.Delete(path);
        }

        return Task.CompletedTask;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _writeLock.Dispose();
    }
}