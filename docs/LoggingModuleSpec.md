# Birdsoft.Infrastructure.Logging 規格與架構說明

## 1. 目標與設計原則

- 專案：Birdsoft.Infrastructure
- 平台：.NET 10（C#）
- 模組：Birdsoft.Infrastructure.Logging
- 目標：
  - 將「記錄（ILogger API）」與「儲存（Json/Sqlite 等具體實作）」解耦。
  - 封裝 Serilog，對外只暴露自家介面，不直接耦合到 Serilog 型別。
  - 支援多種儲存方式（先實作 Json 檔案與 Sqlite）。
  - 檔案型 Log 支援「一日一檔」、「保留天數」、「指定日期刪除」等維護機制。
- 設計原則：
  - Clean architecture：抽象介面放在 Birdsoft.Infrastructure.Logging.Abstractions，實作放在其他專案。
  - 以 DI/Options 為主，使用者只透過擴充方法進行註冊與設定。
  - 儲存層以 ILogStore 與 ILogMaintenance 為中心協作。

---

## 2. Solution / 專案結構

建議 Solution 結構：

- Birdsoft.Infrastructure.sln  
  - Birdsoft.Infrastructure.Logging.Abstractions（class lib, net10.0）
    - 定義核心介面與共用模型（ILogStore、ILogMaintenance、ILogFilePathProvider、ILogger 封裝介面等）
  - Birdsoft.Infrastructure.Logging（class lib, net10.0）
    - 實作與 Serilog 的整合、共用實作、DI 擴充方法
  - Birdsoft.Infrastructure.Logging.Json（class lib, net10.0）
    - 檔案型 Json Log 實作（ILogStore、ILogFilePathProvider、ILogMaintenance 的預設實作）
  - Birdsoft.Infrastructure.Logging.Sqlite（class lib, net10.0）
    - Sqlite Log 儲存實作（ILogStore、ILogMaintenance 的預設實作）
  - Birdsoft.Infrastructure.Logging.Tests（xUnit, net10.0）
    - 單元測試與整合測試

命名空間對照：

- Birdsoft.Infrastructure.Logging.Abstractions
- Birdsoft.Infrastructure.Logging
- Birdsoft.Infrastructure.Logging.Json
- Birdsoft.Infrastructure.Logging.Sqlite
- Birdsoft.Infrastructure.Logging.Tests

---

## 3. 核心抽象（Abstractions 專案）

命名空間：Birdsoft.Infrastructure.Logging.Abstractions

### 3.1 LogLevel 與 LogEntry

說明：自家定義的 Log 等級與 LogEntry 結構，避免直接依賴 Microsoft.Extensions.Logging.LogLevel。

```csharp
namespace Birdsoft.Infrastructure.Logging.Abstractions;

public enum LogLevel
{
    Trace,
    Debug,
    Information,
    Warning,
    Error,
    Critical
}

public sealed class LogEntry
{
    public DateTimeOffset Timestamp { get; init; }
    public LogLevel Level { get; init; }
    public string Category { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public Exception? Exception { get; init; }
    public IReadOnlyDictionary<string, object?> Properties { get; init; }
        = new Dictionary<string, object?>();
}
```

### 3.2 IAppLogger（ILogger 封裝介面）

說明：對外公開的 Logger 介面，語意類似 Microsoft.Extensions.Logging.ILogger，但獨立且可被實作為 Serilog adapter。

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

public interface IAppLogger<T> : IAppLogger
{
}
```

### 3.3 ILogSink（記錄動作 → 寫入儲存）

說明：將「記錄行為」與「實際寫入儲存媒介」分開，Logger 負責建立 LogEntry，ILogSink 負責將 LogEntry 寫入到某一種儲存實作（Json、Sqlite 等）。

```csharp
namespace Birdsoft.Infrastructure.Logging.Abstractions;

public interface ILogSink
{
    Task WriteAsync(
        LogEntry entry,
        CancellationToken cancellationToken = default);
}
```

### 3.4 ILogStore（儲存查詢/刪除）

說明：提供查詢有哪些日期有 Log、查詢特定日期、刪除特定日期 Log 的介面。ILogSink 的具體實作通常也會同時實作 ILogStore。

```csharp
namespace Birdsoft.Infrastructure.Logging.Abstractions;

public interface ILogStore
{
    Task<IReadOnlyList<DateOnly>> GetLogDatesAsync(
        CancellationToken cancellationToken = default);

    IAsyncEnumerable<LogEntry> GetLogsAsync(
        DateOnly date,
        CancellationToken cancellationToken = default);

    Task DeleteLogsAsync(
        DateOnly date,
        CancellationToken cancellationToken = default);
}
```

### 3.5 ILogMaintenance（設定與執行維護條件）

說明：用來設定保留天數、指定刪除日期，並執行實際清理邏輯（呼叫 ILogStore）。

```csharp
namespace Birdsoft.Infrastructure.Logging.Abstractions;

public interface ILogMaintenance
{
    /// <summary>
    /// 以天為單位的保留天數；null 表示不啟用自動保留清理。
    /// </summary>
    int? RetentionDays { get; set; }

    /// <summary>
    /// 額外要刪除的指定日期集合。
    /// </summary>
    IReadOnlyCollection<DateOnly> ExplicitDeleteDates { get; }

    /// <summary>
    /// 新增指定刪除日期。
    /// </summary>
    void AddExplicitDeleteDate(DateOnly date);

    /// <summary>
    /// 移除指定刪除日期。
    /// </summary>
    bool RemoveExplicitDeleteDate(DateOnly date);

    /// <summary>
    /// 執行維護：依 RetentionDays 與 ExplicitDeleteDates 呼叫 ILogStore.DeleteLogsAsync。
    /// </summary>
    Task ExecuteAsync(
        ILogStore store,
        DateOnly today,
        CancellationToken cancellationToken = default);
}
```

### 3.6 ILogFilePathProvider（檔案型 Log 路徑提供者）

說明：提供檔案型 Log 的存放路徑，支援一日一檔。

```csharp
namespace Birdsoft.Infrastructure.Logging.Abstractions;

public interface ILogFilePathProvider
{
    /// <summary>
    /// 取得指定日期的 Log 檔完整路徑。
    /// 例如：logs/2026-01-09.jsonl
    /// </summary>
    string GetLogFilePath(DateOnly date);
}
```

---

## 4. Serilog 包裝與 Logging 核心（Logging 專案）

命名空間：Birdsoft.Infrastructure.Logging

### 4.1 SerilogAppLogger

說明：將 IAppLogger 與 Serilog 整合的具體實作。

- Serilog 仍作為實際 Log pipeline 實作。
- IAppLogger 呼叫 Serilog，並（可選）將 LogEntry 送往 ILogSink（例如 JsonFileLogSink 或 SqliteLogSink）。

概要介面（實際實作細節之後補上）：

```csharp
namespace Birdsoft.Infrastructure.Logging;

public sealed class SerilogAppLogger<T> : IAppLogger<T>
{
    private readonly Serilog.ILogger _logger;
    private readonly ILogSink? _logSink;

    public SerilogAppLogger(Serilog.ILogger logger, ILogSink? logSink = null)
    {
        _logger = logger;
        _logSink = logSink;
    }

    public bool IsEnabled(LogLevel level)
    {
        // 實作：將自訂 LogLevel 對應到 Serilog.Events.LogEventLevel
        throw new NotImplementedException();
    }

    public void Log(LogLevel level, Exception? exception, string messageTemplate, params object?[] args)
    {
        // 1. 呼叫 Serilog 寫入
        // 2. 組出 LogEntry 並透過 _logSink?.WriteAsync(...) 寫入儲存
        throw new NotImplementedException();
    }
}
```

### 4.2 DI 擴充方法

說明：提供簡單註冊方式，讓應用程式可透過 IServiceCollection 整合。

```csharp
namespace Birdsoft.Infrastructure.Logging;

public static class LoggingServiceCollectionExtensions
{
    public static IServiceCollection AddBirdsoftLoggingCore(
        this IServiceCollection services)
    {
        // 註冊 IAppLogger<T>、LogLevel mapping 等
        throw new NotImplementedException();
    }
}
```

---

## 5. Json 檔案儲存實作（Logging.Json 專案）

命名空間：Birdsoft.Infrastructure.Logging.Json

### 5.1 JsonFileLogStore

說明：以每日一檔 JSON Lines（每行一個 JSON 物件）形式儲存 Log。

- 實作 ILogStore 與 ILogSink。
- 檔名透過 ILogFilePathProvider 決定。
- 寫入策略：Append line，採用異步寫入（避免阻塞）。

```csharp
namespace Birdsoft.Infrastructure.Logging.Json;

public sealed class JsonFileLogStore : ILogStore, ILogSink
{
    private readonly ILogFilePathProvider _pathProvider;
    private readonly JsonSerializerOptions _serializerOptions;

    public JsonFileLogStore(ILogFilePathProvider pathProvider, JsonSerializerOptions? options = null)
    {
        _pathProvider = pathProvider;
        _serializerOptions = options ?? new JsonSerializerOptions(JsonSerializerDefaults.Web);
    }

    public Task WriteAsync(LogEntry entry, CancellationToken cancellationToken = default)
    {
        // 取 entry.Timestamp.Date，透過 _pathProvider.GetLogFilePath() 取得檔案
        // 以 append 方式寫入 JSON 一行
        throw new NotImplementedException();
    }

    public Task<IReadOnlyList<DateOnly>> GetLogDatesAsync(CancellationToken cancellationToken = default)
    {
        // 掃描 log 目錄，解析檔名日期
        throw new NotImplementedException();
    }

    public IAsyncEnumerable<LogEntry> GetLogsAsync(DateOnly date, CancellationToken cancellationToken = default)
    {
        // 讀取檔案，逐行反序列化為 LogEntry
        throw new NotImplementedException();
    }

    public Task DeleteLogsAsync(DateOnly date, CancellationToken cancellationToken = default)
    {
        // 刪除對應檔案
        throw new NotImplementedException();
    }
}
```

### 5.2 DefaultLogFilePathProvider

```csharp
namespace Birdsoft.Infrastructure.Logging.Json;

public sealed class DefaultLogFilePathProvider : ILogFilePathProvider
{
    public string RootDirectory { get; }
    public string FileNamePattern { get; } = "yyyy-MM-dd'.jsonl'";

    public DefaultLogFilePathProvider(string rootDirectory)
    {
        RootDirectory = rootDirectory;
    }

    public string GetLogFilePath(DateOnly date)
    {
        // Path.Combine(RootDirectory, date.ToString(FileNamePattern, CultureInfo.InvariantCulture))
        throw new NotImplementedException();
    }
}
```

### 5.3 Json Log 維護服務（可選）

```csharp
namespace Birdsoft.Infrastructure.Logging.Json;

public sealed class JsonLogMaintenanceService : BackgroundService
{
    private readonly ILogStore _store;
    private readonly ILogMaintenance _maintenance;
    private readonly TimeSpan _interval;

    public JsonLogMaintenanceService(ILogStore store, ILogMaintenance maintenance, TimeSpan interval)
    {
        _store = store;
        _maintenance = maintenance;
        _interval = interval;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await _maintenance.ExecuteAsync(_store, DateOnly.FromDateTime(DateTime.UtcNow), stoppingToken);
            await Task.Delay(_interval, stoppingToken);
        }
    }
}
```

### 5.4 DI 擴充方法（Json）

```csharp
namespace Birdsoft.Infrastructure.Logging.Json;

public static class JsonLoggingServiceCollectionExtensions
{
    public static IServiceCollection AddBirdsoftJsonLogging(
        this IServiceCollection services,
        Action<JsonLoggingOptions> configure)
    {
        // 註冊 ILogFilePathProvider, JsonFileLogStore (ILogStore + ILogSink), JsonLogMaintenanceService 等
        throw new NotImplementedException();
    }
}

public sealed class JsonLoggingOptions
{
    public string RootDirectory { get; set; } = "logs";
    public int? RetentionDays { get; set; }
}
```

---

## 6. Sqlite 儲存實作（Logging.Sqlite 專案）

命名空間：Birdsoft.Infrastructure.Logging.Sqlite

### 6.1 建議資料表結構

- Table: Logs  
  - Id (INTEGER PRIMARY KEY AUTOINCREMENT)
  - Date (TEXT, ISO yyyy-MM-dd)
  - Timestamp (TEXT, ISO 8601)
  - Level (TEXT)
  - Category (TEXT)
  - Message (TEXT)
  - Exception (TEXT, nullable)
  - PropertiesJson (TEXT, nullable)

### 6.2 SqliteLogStore（ILogStore + ILogSink）

```csharp
namespace Birdsoft.Infrastructure.Logging.Sqlite;

public sealed class SqliteLogStore : ILogStore, ILogSink
{
    private readonly string _connectionString;

    public SqliteLogStore(string connectionString)
    {
        _connectionString = connectionString;
    }

    public Task WriteAsync(LogEntry entry, CancellationToken cancellationToken = default)
    {
        // INSERT INTO Logs ...
        throw new NotImplementedException();
    }

    public Task<IReadOnlyList<DateOnly>> GetLogDatesAsync(CancellationToken cancellationToken = default)
    {
        // SELECT DISTINCT Date FROM Logs ...
        throw new NotImplementedException();
    }

    public IAsyncEnumerable<LogEntry> GetLogsAsync(DateOnly date, CancellationToken cancellationToken = default)
    {
        // SELECT * FROM Logs WHERE Date = ...
        throw new NotImplementedException();
    }

    public Task DeleteLogsAsync(DateOnly date, CancellationToken cancellationToken = default)
    {
        // DELETE FROM Logs WHERE Date = ...
        throw new NotImplementedException();
    }
}
```

### 6.3 Sqlite DI 擴充方法

```csharp
namespace Birdsoft.Infrastructure.Logging.Sqlite;

public static class SqliteLoggingServiceCollectionExtensions
{
    public static IServiceCollection AddBirdsoftSqliteLogging(
        this IServiceCollection services,
        Action<SqliteLoggingOptions> configure)
    {
        // 註冊 SqliteLogStore (ILogStore + ILogSink)、維護設定
        throw new NotImplementedException();
    }
}

public sealed class SqliteLoggingOptions
{
    public string ConnectionString { get; set; } = string.Empty;
    public int? RetentionDays { get; set; }
}
```

---

## 7. 使用情境 / API 規格摘要

### 7.1 一般應用程式註冊

```csharp
var builder = WebApplication.CreateBuilder(args);

// 核心 Logging（Serilog + IAppLogger）
builder.Services.AddBirdsoftLoggingCore();

// Json File Logging
builder.Services.AddBirdsoftJsonLogging(options =>
{
    options.RootDirectory = "logs";
    options.RetentionDays = 30;
});

// 或 Sqlite Logging
builder.Services.AddBirdsoftSqliteLogging(options =>
{
    options.ConnectionString = "Data Source=logs.db";
    options.RetentionDays = 90;
});
```

### 7.2 在程式碼中使用 IAppLogger

```csharp
public class SampleService
{
    private readonly IAppLogger<SampleService> _logger;

    public SampleService(IAppLogger<SampleService> logger)
    {
        _logger = logger;
    }

    public void DoWork()
    {
        _logger.Log(LogLevel.Information, null, "Start work at {Time}", DateTimeOffset.Now);
    }
}
```

### 7.3 使用 ILogStore 查詢 Log

```csharp
public class LogQueryService
{
    private readonly ILogStore _store;

    public LogQueryService(ILogStore store)
    {
        _store = store;
    }

    public async Task<IEnumerable<LogEntry>> GetLogsAsync(DateOnly date)
    {
        var result = new List<LogEntry>();
        await foreach (var entry in _store.GetLogsAsync(date))
        {
            result.Add(entry);
        }
        return result;
    }
}
```

### 7.4 使用 ILogMaintenance 設定保留天數與指定日期刪除

```csharp
public class MaintenanceConfiguration
{
    private readonly ILogMaintenance _maintenance;

    public MaintenanceConfiguration(ILogMaintenance maintenance)
    {
        _maintenance = maintenance;
    }

    public void Configure()
    {
        _maintenance.RetentionDays = 30;
        _maintenance.AddExplicitDeleteDate(new DateOnly(2025, 1, 1));
    }
}
```

---

## 8. 測試專案（Birdsoft.Infrastructure.Logging.Tests）

- 專案：Birdsoft.Infrastructure.Logging.Tests
- 測試框架：xUnit
- 覆蓋範圍：
  - Abstractions：
    - LogEntry/LogLevel 與 Serilog 等級 mapping 的測試（之後實作時補充）。
  - JsonFileLogStore：
    - 寫入 LogEntry 後，檔案存在且格式正確。
    - GetLogDatesAsync 正確讀到日期。
    - GetLogsAsync 能正確還原 LogEntry。
    - DeleteLogsAsync 會刪除對應檔案。
  - SqliteLogStore：
    - 寫入/讀取/刪除行為。
  - DefaultLogMaintenance（未在此文件內展開，實作時可新增）：
    - 設定 RetentionDays，模擬今日日期後，正確呼叫 ILogStore.DeleteLogsAsync。
    - 設定 ExplicitDeleteDates 後，正確刪除指定日期。
  - DI 與整合測試：
    - AddBirdsoftJsonLogging / AddBirdsoftSqliteLogging 可順利註冊並 resolve 相依物件。
    - 實際呼叫 IAppLogger<T> 會觸發 ILogSink 寫入。
