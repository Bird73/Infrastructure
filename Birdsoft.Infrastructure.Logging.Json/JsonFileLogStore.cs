using System.Collections.Generic;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Birdsoft.Infrastructure.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Birdsoft.Infrastructure.Logging.Json;

public sealed class JsonFileLogStore : ILogStore, ILogSink
{
    private readonly ILogFilePathProvider _pathProvider;
    private readonly string _rootDirectory;
    private readonly JsonSerializerOptions _serializerOptions;
    private readonly SemaphoreSlim _writeLock = new(1, 1);

    public JsonFileLogStore(
        ILogFilePathProvider pathProvider,
        IOptions<JsonLoggingOptions> options,
        JsonSerializerOptions? serializerOptions = null)
    {
        _pathProvider = pathProvider ?? throw new ArgumentNullException(nameof(pathProvider));

        if (options is null)
        {
            throw new ArgumentNullException(nameof(options));
        }

        _rootDirectory = options.Value.RootDirectory;
        _serializerOptions = serializerOptions ?? new JsonSerializerOptions(JsonSerializerDefaults.Web);
    }

    public async Task WriteAsync(LogEntry entry, CancellationToken cancellationToken = default)
    {
        if (entry is null)
        {
            throw new ArgumentNullException(nameof(entry));
        }

        var date = DateOnly.FromDateTime(entry.Timestamp.LocalDateTime);
        var filePath = _pathProvider.GetLogFilePath(date);

        var directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var line = JsonSerializer.Serialize(entry, _serializerOptions);

        await _writeLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await using var stream = new FileStream(
                filePath,
                FileMode.Append,
                FileAccess.Write,
                FileShare.Read,
                bufferSize: 64 * 1024,
                useAsync: true);

            await using var writer = new StreamWriter(stream);
            await writer.WriteLineAsync(line.AsMemory(), cancellationToken).ConfigureAwait(false);
            await writer.FlushAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _writeLock.Release();
        }
    }

    public Task<IReadOnlyList<DateOnly>> GetLogDatesAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var root = _rootDirectory;
        if (!Path.IsPathRooted(root))
        {
            root = Path.Combine(AppContext.BaseDirectory, root);
        }

        if (!Directory.Exists(root))
        {
            return Task.FromResult<IReadOnlyList<DateOnly>>(Array.Empty<DateOnly>());
        }

        var dates = new List<DateOnly>();

        foreach (var file in Directory.EnumerateFiles(root, "*.jsonl", SearchOption.TopDirectoryOnly))
        {
            var name = Path.GetFileNameWithoutExtension(file);
            if (DateOnly.TryParseExact(name, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
            {
                dates.Add(date);
            }
        }

        dates.Sort();
        return Task.FromResult<IReadOnlyList<DateOnly>>(dates);
    }

    public async IAsyncEnumerable<LogEntry> GetLogsAsync(
        DateOnly date,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var filePath = _pathProvider.GetLogFilePath(date);
        if (!File.Exists(filePath))
        {
            yield break;
        }

        await using var stream = new FileStream(
            filePath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.ReadWrite,
            bufferSize: 64 * 1024,
            useAsync: true);

        using var reader = new StreamReader(stream);

        while (!reader.EndOfStream)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var line = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false);
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            LogEntry? entry;
            try
            {
                entry = JsonSerializer.Deserialize<LogEntry>(line, _serializerOptions);
            }
            catch
            {
                continue;
            }

            if (entry is not null)
            {
                yield return entry;
            }
        }
    }

    public Task DeleteLogsAsync(DateOnly date, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var filePath = _pathProvider.GetLogFilePath(date);
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }

        return Task.CompletedTask;
    }
}
