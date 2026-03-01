# Birdsoft.Infrastructure.Logging — 架構設計

> 版本：v1 | 狀態：Implemented | 日期：2026-03-02

## 1. 模組分層架構

```
┌─────────────────────────────────────────────────────────┐
│                    消費者 / 宿主應用程式                    │
│          (IAppLogger<T> 或 ILogger<T> 皆可使用)           │
└────────────┬──────────────────────┬──────────────────────┘
             │                      │
   IAppLogger<T>            ILogger<T> (MS)
             │                      │
             ▼                      ▼
┌────────────────────┐  ┌──────────────────────┐
│ SerilogAppLogger<T>│  │ LogSinkLoggerProvider │
│  (Serilog bridge)  │  │  (MS ILogger bridge)  │
└────────┬───────────┘  └──────────┬────────────┘
         │                         │
         │    ┌────────────────┐   │
         └───►│  ILogSink      │◄──┘
              │  .WriteAsync() │
              └───────┬────────┘
                      │
         ┌────────────┼────────────┐
         ▼                         ▼
┌─────────────────┐     ┌──────────────────┐
│ JsonFileLogStore │     │  SqliteLogStore   │
│ (ILogStore+Sink) │     │ (ILogStore+Sink)  │
└─────────────────┘     └──────────────────┘
```

---

## 2. 專案相依圖

```
Logging.Abstractions          ← 零外部相依
       ▲
       │
Logging (Core)                ← Serilog, MS.Ext.Logging, MS.Ext.DI
       ▲
       │
  ┌────┴────┐
  │         │
Logging.Json    Logging.Sqlite
  │                │
  ├ MS.Ext.Options ├ Microsoft.Data.Sqlite
  └ MS.Ext.DI     └ MS.Ext.Options, MS.Ext.DI

Logging.Tests ─────────► 引用以上所有專案
```

---

## 3. 關鍵類別設計

### 3.1 LogEntry（Abstractions）

```csharp
public sealed class LogEntry
{
    public DateTimeOffset Timestamp { get; init; }      // 必須 UTC
    public LogLevel Level { get; init; }
    public string Category { get; init; } = string.Empty;
    public string MessageTemplate { get; init; } = string.Empty;
    public string RenderedMessage { get; init; } = string.Empty;
    public string? ExceptionDetail { get; init; }
    public IReadOnlyDictionary<string, object?> Properties { get; init; }
        = new Dictionary<string, object?>();
}
```

- `Exception` 物件不再被持有，避免序列化問題。
- `MessageTemplate` 保留原始 template 以供結構化查詢。
- `RenderedMessage` 為人可讀的渲染結果。

### 3.2 LogLevel（Abstractions）

```csharp
public enum LogLevel
{
    Trace = 0,
    Debug = 1,
    Information = 2,
    Warning = 3,
    Error = 4,
    Critical = 5,
    None = 6,        // 新增：用於停用日誌
}
```

### 3.3 LogQuery（Abstractions）

```csharp
public sealed class LogQuery
{
    public required DateOnly Date { get; init; }
    public LogLevel? MinLevel { get; init; }
    public string? Category { get; init; }
    public string? Keyword { get; init; }
    public bool OrderByTimestampDescending { get; init; }  // 預設 false (ASC)
}
```

- `GetLogsAsync(LogQuery)` 在契約層面保證回傳結果依 `Timestamp` 排序。
- 預設為升冪（ASC），設定 `OrderByTimestampDescending = true` 時為降冪（DESC）。

### 3.4 LogEntryRedactor（Core）

```
職責：在 LogEntry 建構階段自動遮蔽敏感資訊

設計：
  - 靜態類別，提供 Redact(string?) 方法
  - 內部維護一組預設 Regex pattern
  - 提供 AddPattern(string pattern) 靜態方法以擴充
  - 提供 ResetToDefaults() 靜態方法，將 pattern 清單恢復為預設狀態（專供測試使用）
  - 掃描 RenderedMessage、ExceptionDetail、Properties values
  - RedactProperties 同時執行型別正規化：
    支援型別：string / int / long / double / decimal / bool /
               DateTime / DateTimeOffset / Guid / null
    其他型別 → ToString() 並經 Redact()

預設 patterns：
  - access_token=<value>  → access_token=[REDACTED]
  - refresh_token=<value> → refresh_token=[REDACTED]
  - client_secret=<value> → client_secret=[REDACTED]
  - password=<value>      → password=[REDACTED]
  - pwd=<value>           → pwd=[REDACTED]
  - Bearer eyJ...         → Bearer [REDACTED]
  - OAuth 完整連結中的 token 參數
```

### 3.5 MessageTemplateParser（Core）

```
職責：解析 message template 的佔位符，建立命名 key-value 對應

輸入：
  - messageTemplate: string  (如 "Error {Code} at {Endpoint}")
  - args: object?[]          (如 [500, "/api/users"])

輸出：
  - MessageTemplateParseResult
      - RenderedMessage: string  (如 "Error 500 at /api/users")
      - Properties: IReadOnlyDictionary<string, object?>
          { "Code": 500, "Endpoint": "/api/users" }

規則：
  - 佔位符格式：{Name} 或 {Name:format}
  - 佔位符數量 > args 數量時，多餘佔位符保留原文
  - args 數量 > 佔位符數量時，多餘 args 忽略
  - 嵌套大括號 {{ }} 視為字面值
```

### 3.6 LevelMapper（Core）

```
職責：集中管理三方 LogLevel 的雙向映射

方法：
  - ToSerilogLevel(LogLevel) → LogEventLevel
  - ToAppLevel(LogEventLevel) → LogLevel
  - ToMicrosoftLevel(LogLevel) → MS.LogLevel
  - ToAppLevel(MS.LogLevel) → LogLevel

特殊處理：
  - LogLevel.None → MS.LogLevel.None（不寫入 Serilog）
  - 未知值 → LogLevel.Information（fallback）
```

### 3.7 SerilogAppLogger\<T\>（Core）

```
建構子：(ILogger serilogLogger, ILogSink logSink)

Log() 流程：
  1. 呼叫 Serilog 寫入
    2. 使用 MessageTemplateParser 解析 template + args
    3. 建構 LogEntry（Timestamp = UtcNow, ExceptionDetail = exception?.ToString()）
    4. 若 Activity.Current 存在，將 trace_id、span_id 寫入 Properties
    5. Properties 值型別正規化（不支援的型別 → ToString()）
    6. LogEntryRedactor 遮蔽
    7. try { _logSink.WriteAsync(entry).GetAwaiter().GetResult(); }
      catch { /* 靜默 */ }
```

### 3.8 LogSinkLoggerProvider（Core）

```
LogSinkLogger.Log<TState>() 流程：
  1. 若 state 為 IReadOnlyList<KeyValuePair<string, object?>>:
     a. 取 "{OriginalFormat}" → MessageTemplate
     b. 其餘 key-value → Properties（經型別正規化）
     否則：
     a. MessageTemplate = "";
     b. Properties = 空
  2. formatter(state, exception) → RenderedMessage
  3. 建構 LogEntry（Timestamp = UtcNow）
  4. Level 映射使用 LevelMapper.ToAppLevel()
  5. 透過 ISupportExternalScope 取得 scope 資料，合併至 Properties
  6. 若 Activity.Current 存在，將 trace_id、span_id 寫入 Properties
  7. Properties 值型別正規化
  8. LogEntryRedactor 遮蔽
  9. try { _sink.WriteAsync(entry).GetAwaiter().GetResult(); }
     catch { /* 靜默 */ }

Dispose()：
  - 若 _sink 為 IDisposable，一併 Dispose
```

### 3.9 DefaultLogMaintenance（Core）

```
建構子：(IOptions<LoggingOptions> options)

欄位：
  - _retentionDays: int?  ← 從 options.Value.RetentionDays 讀取
  - _explicitDeleteDates: ConcurrentDictionary<DateOnly, byte>

ExecuteAsync() 流程不變，但 RetentionDays 來源為建構子注入。
```

### 3.10 JsonFileLogStore（Json）

```
實作：ILogStore, ILogSink, IDisposable

變更摘要：
  - ILogFilePathProvider 介面移入本專案
  - GetLogsAsync(LogQuery) 支援記憶體端篩選 + OrderByTimestampDescending 排序
  - 反序列化使用新 LogEntry 結構（ExceptionDetail, RenderedMessage）
  - JSONL 讀取容錯：尾行截斷或 JSON 損壞時 skip，不拋出例外
  - Dispose() 釋放 SemaphoreSlim
  - JsonLoggingOptions 移除 RetentionDays
```

### 3.11 SqliteLogStore（Sqlite）

```
實作：ILogStore, ILogSink, IDisposable, IAsyncDisposable

變更摘要：
  - 維護單一長期存活的 SqliteConnection
  - 初始化時執行 PRAGMA journal_mode=WAL
  - 資料表結構：Level 欄位改為 INTEGER（0~6）
  - 增加 RenderedMessage、MessageTemplate、ExceptionDetail 欄位
  - GetLogsAsync(LogQuery) 使用 SQL WHERE 條件篩選 + ORDER BY 排序
  - Dispose/DisposeAsync 關閉持久連線 + 釋放 SemaphoreSlim
  - SqliteLoggingOptions 移除 RetentionDays
```

#### Sqlite 資料表結構

```sql
CREATE TABLE IF NOT EXISTS Logs (
    Id               INTEGER PRIMARY KEY AUTOINCREMENT,
    Date             TEXT    NOT NULL,
    Timestamp        TEXT    NOT NULL,
    Level            INTEGER NOT NULL,    -- 0=Trace, 1=Debug, 2=Information, 3=Warning, 4=Error, 5=Critical, 6=None
    Category         TEXT    NOT NULL,
    MessageTemplate  TEXT    NOT NULL,
    RenderedMessage  TEXT    NOT NULL,
    ExceptionDetail  TEXT    NULL,
    PropertiesJson   TEXT    NULL
);
CREATE INDEX IF NOT EXISTS IX_Logs_Date ON Logs(Date);
CREATE INDEX IF NOT EXISTS IX_Logs_Level ON Logs(Level);
```

---

## 4. DI 註冊流程

### 4.1 AddBirdsoftLoggingCore()

```
services.TryAddSingleton<Serilog.ILogger>(_ => new LoggerConfiguration().MinimumLevel.Verbose().CreateLogger());
services.TryAddSingleton<ILogMaintenance, DefaultLogMaintenance>();
services.TryAddSingleton(typeof(IAppLogger<>), typeof(SerilogAppLogger<>));
```

### 4.2 AddBirdsoftJsonLogging(configure)

```
services.AddBirdsoftLoggingCore();
services.AddOptions<JsonLoggingOptions>().Configure(configure);
services.TryAddSingleton<ILogFilePathProvider>(sp => ...);
services.TryAddSingleton<JsonFileLogStore>();
services.TryAddSingleton<ILogStore>(sp => sp.GetRequiredService<JsonFileLogStore>());
services.TryAddSingleton<ILogSink>(sp => sp.GetRequiredService<JsonFileLogStore>());
// 不再 Replace ILogMaintenance
```

### 4.3 AddBirdsoftSqliteLogging(configure)

```
services.AddBirdsoftLoggingCore();
services.AddOptions<SqliteLoggingOptions>().Configure(configure);
services.TryAddSingleton<SqliteLogStore>();
services.TryAddSingleton<ILogStore>(sp => sp.GetRequiredService<SqliteLogStore>());
services.TryAddSingleton<ILogSink>(sp => sp.GetRequiredService<SqliteLogStore>());
// 不再 Replace ILogMaintenance
```

### 4.4 AddAppLogging(clearExistingProviders, configure?)

```
builder.AddAppLogging(clearExistingProviders, configure);
if (clearExistingProviders)
{
  builder.ClearProviders();
}
builder.Services.TryAddEnumerable(
  ServiceDescriptor.Singleton<ILoggerProvider, LogSinkLoggerProvider>());
configure?.Invoke(builder);
```

---

## 5. Redaction 流程圖

```
呼叫端
  │
  ▼
SerilogAppLogger.Log() / LogSinkLogger.Log()
  │
  ├─ 解析 template → renderedMessage + properties
  │
  ├─ 建構 LogEntry {
  │      RenderedMessage = LogEntryRedactor.Redact(renderedMessage),
  │      ExceptionDetail = LogEntryRedactor.Redact(exception?.ToString()),
  │      Properties      = LogEntryRedactor.RedactProperties(properties),
  │  }
  │
  ▼
ILogSink.WriteAsync(entry)  ← 已遮蔽的 LogEntry
```

---

## 6. 設計決策記錄

| # | 決策 | 理由 |
|---|------|------|
| D-01 | 不使用 Channel / BackgroundService | 本模組專注 Error log，吞吐量低，避免複雜化 |
| D-02 | Exception 改為 string ExceptionDetail | 避免 System.Text.Json 序列化問題 |
| D-03 | 統一 UtcNow | 避免不同路徑產生不同時區 Timestamp |
| D-04 | 在建構階段 Redact | 確保任何寫入路徑的資料都已遮蔽 |
| D-05 | LogSinkLogger / SerilogAppLogger 同步寫入 | ILogger.Log 與 IAppLogger.Log 皆為同步介面，統一採 GetAwaiter().GetResult() + try-catch，確保測試可預測 |
| D-06 | RetentionDays 提升至全域 Options | 避免多 store 共存時 Replace 衝突 |
| D-07 | ILogFilePathProvider 移至 Json 專案 | 僅 Json store 使用，不污染 Abstractions |
| D-08 | SQLite 長期連線 + WAL | 避免頻繁開關連線，提升寫入效能 |
| D-09 | MessageTemplate + RenderedMessage 分欄 | 同時保留結構化查詢能力與人可讀性 |
| D-10 | LogQuery 複合篩選 + 排序 | 提供 date/level/category/keyword 篩選，契約保證 Timestamp 排序 |
| D-11 | SQLite Level 存為 INTEGER | 整數比較利於 SQL 篩選（WHERE Level >= @min），效能優於字串 |
| D-12 | MS ILogger bridge 用 {OriginalFormat} | 正確取得 MessageTemplate，其餘 state kv 直接作為結構化 Properties |
| D-13 | AddAppLogging 不預設 ClearProviders | 避免意外清除宗主的既有 provider，提供參數由呼叫端決定 |
| D-14 | LogEntryRedactor 可重置 | 靜態類別在測試間可能汙染，提供 ResetToDefaults() 確保測試隔離 |
| D-15 | Properties value 型別約束 | Store 層只保證基本型別，其餘轉 string 並經 Redaction |
| D-16 | Activity.Current + scope 落入 Properties | 提升 Observability，運用既有 ISupportExternalScope 實作 |
| D-17 | JSONL 讀取容忍截斷行 | 行程序崩潰時尾行可能不完整，讀取端應 graceful skip |
