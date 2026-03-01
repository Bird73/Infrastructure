# Birdsoft.Infrastructure.Logging — 介面契約規格

> 版本：v1 | 狀態：Draft | 日期：2026-03-01

本文件定義所有公開介面與模型的完整簽章，作為實作的 contracts-first 參考。

---

## 1. Abstractions 專案

### 1.1 LogLevel

```csharp
namespace Birdsoft.Infrastructure.Logging.Abstractions;

public enum LogLevel
{
    Trace = 0,
    Debug = 1,
    Information = 2,
    Warning = 3,
    Error = 4,
    Critical = 5,
    None = 6,
}
```

### 1.2 LogEntry

```csharp
namespace Birdsoft.Infrastructure.Logging.Abstractions;

public sealed class LogEntry
{
    public DateTimeOffset Timestamp { get; init; }

    public LogLevel Level { get; init; }

    public string Category { get; init; } = string.Empty;

    public string MessageTemplate { get; init; } = string.Empty;

    public string RenderedMessage { get; init; } = string.Empty;

    public string? ExceptionDetail { get; init; }

    public IReadOnlyDictionary<string, object?> Properties { get; init; }
        = new Dictionary<string, object?>();
}
```

### 1.3 LogQuery

```csharp
namespace Birdsoft.Infrastructure.Logging.Abstractions;

public sealed class LogQuery
{
    public required DateOnly Date { get; init; }

    public LogLevel? MinLevel { get; init; }

    public string? Category { get; init; }

    public string? Keyword { get; init; }

    /// <summary>
    /// 預設 false（Timestamp ASC）。設為 true 時為 Timestamp DESC。
    /// </summary>
    public bool OrderByTimestampDescending { get; init; }
}
```

### 1.4 IAppLogger / IAppLogger\<T\>

```csharp
namespace Birdsoft.Infrastructure.Logging.Abstractions;

public interface IAppLogger
{
    bool IsEnabled(LogLevel level);

    void Log(
        LogLevel level,
        Exception? exception,
        string messageTemplate,
        params object?[] args);
}

public interface IAppLogger<T> : IAppLogger { }
```

### 1.5 ILogSink

```csharp
namespace Birdsoft.Infrastructure.Logging.Abstractions;

public interface ILogSink
{
    Task WriteAsync(LogEntry entry, CancellationToken cancellationToken = default);
}
```

### 1.6 ILogStore

```csharp
namespace Birdsoft.Infrastructure.Logging.Abstractions;

public interface ILogStore
{
    Task<IReadOnlyList<DateOnly>> GetLogDatesAsync(
        CancellationToken cancellationToken = default);

    IAsyncEnumerable<LogEntry> GetLogsAsync(
        LogQuery query,
        CancellationToken cancellationToken = default);

    Task DeleteLogsAsync(
        DateOnly date,
        CancellationToken cancellationToken = default);
}
```

### 1.7 ILogMaintenance

```csharp
namespace Birdsoft.Infrastructure.Logging.Abstractions;

public interface ILogMaintenance
{
    IReadOnlyCollection<DateOnly> ExplicitDeleteDates { get; }

    void AddExplicitDeleteDate(DateOnly date);

    bool RemoveExplicitDeleteDate(DateOnly date);

    Task ExecuteAsync(
        ILogStore store,
        DateOnly today,
        CancellationToken cancellationToken = default);
}
```

---

## 2. Core 專案

### 2.1 LoggingOptions

```csharp
namespace Birdsoft.Infrastructure.Logging;

public sealed class LoggingOptions
{
    public int? RetentionDays { get; set; }
}
```

### 2.2 LevelMapper（靜態）

```csharp
namespace Birdsoft.Infrastructure.Logging;

public static class LevelMapper
{
    public static LogEventLevel ToSerilogLevel(LogLevel level);
    public static LogLevel FromSerilogLevel(LogEventLevel level);
    public static Microsoft.Extensions.Logging.LogLevel ToMicrosoftLevel(LogLevel level);
    public static LogLevel FromMicrosoftLevel(Microsoft.Extensions.Logging.LogLevel level);
}
```

### 2.3 MessageTemplateParser（靜態）

```csharp
namespace Birdsoft.Infrastructure.Logging;

public static class MessageTemplateParser
{
    public static (string RenderedMessage, Dictionary<string, object?> Properties)
        Parse(string messageTemplate, object?[] args);
}
```

### 2.4 LogEntryRedactor（靜態）

```csharp
namespace Birdsoft.Infrastructure.Logging;

public static class LogEntryRedactor
{
    public static string? Redact(string? input);

    public static IReadOnlyDictionary<string, object?> RedactProperties(
        IReadOnlyDictionary<string, object?> properties);

    public static void AddPattern(Regex pattern, string replacement = "[REDACTED]");

    /// <summary>
    /// 將 pattern 清單恢復為預設狀態。專供測試使用，避免測試間靜態狀態汙染。
    /// </summary>
    public static void ResetToDefaults();
}
```

### 2.5 DI 擴充方法

```csharp
namespace Birdsoft.Infrastructure.Logging;

public static class LoggingServiceCollectionExtensions
{
    public static IServiceCollection AddBirdsoftLoggingCore(
        this IServiceCollection services,
        Action<LoggingOptions>? configure = null);

    /// <summary>
    /// 註冊 LogSinkLoggerProvider 至 MS Logging pipeline。
    /// </summary>
    /// <param name="clearExistingProviders">
    /// 預設 false。設為 true 時會先清除既有 provider（如 Console、Debug）。
    /// </param>
    public static IServiceCollection AddAppLogging(
        this IServiceCollection services,
        bool clearExistingProviders = false,
        Action<ILoggingBuilder>? configure = null);
}
```

---

## 3. Json 專案

### 3.1 ILogFilePathProvider（從 Abstractions 移入）

```csharp
namespace Birdsoft.Infrastructure.Logging.Json;

public interface ILogFilePathProvider
{
    string GetLogFilePath(DateOnly date);
}
```

### 3.2 JsonLoggingOptions

```csharp
namespace Birdsoft.Infrastructure.Logging.Json;

public sealed class JsonLoggingOptions
{
    public string RootDirectory { get; set; } = "logs";
}
```

### 3.3 DI 擴充方法

```csharp
namespace Birdsoft.Infrastructure.Logging.Json;

public static class JsonLoggingServiceCollectionExtensions
{
    public static IServiceCollection AddBirdsoftJsonLogging(
        this IServiceCollection services,
        Action<JsonLoggingOptions> configure);
}
```

---

## 4. Sqlite 專案

### 4.1 SqliteLoggingOptions

```csharp
namespace Birdsoft.Infrastructure.Logging.Sqlite;

public sealed class SqliteLoggingOptions
{
    public string ConnectionString { get; set; } = string.Empty;
}
```

### 4.2 DI 擴充方法

```csharp
namespace Birdsoft.Infrastructure.Logging.Sqlite;

public static class SqliteLoggingServiceCollectionExtensions
{
    public static IServiceCollection AddBirdsoftSqliteLogging(
        this IServiceCollection services,
        Action<SqliteLoggingOptions> configure);
}
```
