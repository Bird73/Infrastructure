# Birdsoft.Infrastructure.Logging — 需求規格

> 版本：v1 | 狀態：Implemented | 日期：2026-03-02

## 1. 概述

### 1.1 模組定位

Birdsoft.Infrastructure.Logging 是一套輕量的結構化日誌基礎設施，**專注於 Error / 異常日誌的記錄與偵錯輔助**。不追求高吞吐量、即時串流等複雜場景；設計上以「簡單、正確、可追蹤」為核心原則。

### 1.2 專案結構（目標）

| 專案 | 職責 |
|------|------|
| `Birdsoft.Infrastructure.Logging.Abstractions` | 公開契約：介面、模型、enum |
| `Birdsoft.Infrastructure.Logging` | Core 實作：Serilog bridge、MS Logging bridge、DI 註冊、Redaction |
| `Birdsoft.Infrastructure.Logging.Json` | JSONL 檔案式 store/sink 實作 |
| `Birdsoft.Infrastructure.Logging.Sqlite` | SQLite 式 store/sink 實作 |
| `Birdsoft.Infrastructure.Logging.Tests` | 測試專案 |

### 1.3 目標框架與版本

- TargetFramework：`net10.0`
- Package 版本：`0.1.0`（重構後重新起算）
- 尚未正式發行，相依專案將全部重啟

---

## 2. 功能需求（REQ）

### 2.1 Abstractions — 公開契約

#### REQ-ABS-01：LogLevel enum

定義日誌等級列舉，值域：

| 名稱 | 值 |
|------|-----|
| Trace | 0 |
| Debug | 1 |
| Information | 2 |
| Warning | 3 |
| Error | 4 |
| Critical | 5 |
| None | 6 |

- `None` 用於停用日誌，不得作為實際寫入等級。

#### REQ-ABS-02：LogEntry 模型

```
LogEntry:
  Timestamp     : DateTimeOffset           // 必須為 UTC
  Level         : LogLevel
  Category      : string
  MessageTemplate : string                 // 原始 template（如 "Error {Code}"）
  RenderedMessage : string                 // 渲染後訊息（如 "Error 500"）
  ExceptionDetail : string?               // Exception.ToString() 結果
  Properties    : IReadOnlyDictionary<string, object?>  // 命名佔位符 key-value
```

- 不再持有 `Exception` 物件引用，改以 `string? ExceptionDetail` 儲存。
- `Properties` 必須包含根據 `MessageTemplate` 佔位符名稱解析出的命名 key-value 對應。
- `Properties` 亦可包含 `trace_id`、`span_id`（來自 `Activity.Current`）與 scope 資料。
- **Properties value 型別約束**：Store 層只保證支援 `string`、數值型別（int/long/double/decimal）、`bool`、`DateTime`/`DateTimeOffset`、`Guid`、`null`。其他型別一律在建構階段轉為 `string`（經 `ToString()` 並通過 Redaction）。

#### REQ-ABS-03：Redaction — 敏感資料自動遮蔽

- 在 `LogEntry` 建構階段，自動掃描所有文字欄位（`RenderedMessage`、`ExceptionDetail`、`Properties` values）中的已知敏感 pattern。
- 已知 pattern 至少包含：
  - `access_token=...`
  - `refresh_token=...`
  - `client_secret=...`
  - Bearer token（`Bearer eyJ...`）
  - 密碼欄位（`password=...`、`pwd=...`）
  - 完整 OAuth 驗證連結
- 偵測到的敏感值以 `[REDACTED]` 取代。
- Redaction 邏輯應可擴充（允許追加自訂 pattern）。
- 提供「可重置」機制（`ResetToDefaults()`），專供測試使用，將 pattern 清單恢復為預設狀態，避免測試間靜態狀態汙染。

#### REQ-ABS-04：IAppLogger / IAppLogger\<T\>

```csharp
public interface IAppLogger
{
    bool IsEnabled(LogLevel level);
    void Log(LogLevel level, Exception? exception, string messageTemplate, params object?[] args);
}

public interface IAppLogger<T> : IAppLogger { }
```

#### REQ-ABS-05：ILogSink

```csharp
public interface ILogSink
{
    Task WriteAsync(LogEntry entry, CancellationToken cancellationToken = default);
}
```

#### REQ-ABS-06：ILogStore

```csharp
public interface ILogStore
{
    Task<IReadOnlyList<DateOnly>> GetLogDatesAsync(CancellationToken ct = default);
    IAsyncEnumerable<LogEntry> GetLogsAsync(LogQuery query, CancellationToken ct = default);
    Task DeleteLogsAsync(DateOnly date, CancellationToken ct = default);
}
```

#### REQ-ABS-07：LogQuery — 查詢篩選

```
LogQuery:
  Date        : DateOnly                  // 必要
  MinLevel    : LogLevel?                 // 可選，篩選 >= 此等級
  Category    : string?                   // 可選，包含比對
  Keyword     : string?                   // 可選，RenderedMessage 包含比對
  OrderByTimestampDescending : bool       // 預設 false（Timestamp ASC）；true 時 DESC
```

- `GetLogsAsync(LogQuery)` 在契約層面保證回傳結果依 `Timestamp` 排序。
- 預設為升冪（ASC），設定 `OrderByTimestampDescending = true` 時為降冪（DESC）。

#### REQ-ABS-08：ILogMaintenance

```csharp
public interface ILogMaintenance
{
    IReadOnlyCollection<DateOnly> ExplicitDeleteDates { get; }
    void AddExplicitDeleteDate(DateOnly date);
    bool RemoveExplicitDeleteDate(DateOnly date);
    Task ExecuteAsync(ILogStore store, DateOnly today, CancellationToken ct = default);
}
```

- `RetentionDays` **不再**由 `ILogMaintenance` 持有，改從全域 `LoggingOptions` 讀取。

---

### 2.2 Core — Logging 主專案

#### REQ-CORE-01：SerilogAppLogger\<T\>

- 透過 Serilog `ILogger` 執行寫入。
- `Log()` 方法中：
  - 使用 `DateTimeOffset.UtcNow` 作為 Timestamp。
  - 解析 `messageTemplate` 的佔位符名稱，與 `args` 建立命名 key-value 對應至 `Properties`。
  - 渲染 `messageTemplate` + `args` 為 `RenderedMessage`。
  - 將 `Exception?.ToString()` 存入 `ExceptionDetail`。
  - 若 `Activity.Current` 存在，將 `trace_id`、`span_id` 寫入 `Properties`。
  - 建構 `LogEntry` 時觸發 Redaction（含 Properties value 型別正規化）。
  - Sink 寫入採 `_sink.WriteAsync(entry).GetAwaiter().GetResult()`（與 MS ILogger bridge 同步策略一致），外層 `try-catch` 靜默忽略。

#### REQ-CORE-02：LevelMapper — 集中式雙向映射

- 提供單一靜態 mapper 類別，包含：
  - 自訂 `LogLevel` ↔ Serilog `LogEventLevel` 雙向映射。
  - 自訂 `LogLevel` ↔ `Microsoft.Extensions.Logging.LogLevel` 雙向映射。
- `LogLevel.None` 映射至 MS 的 `LogLevel.None`；Serilog 無對應值時不該寫入。

#### REQ-CORE-03：LogSinkLoggerProvider（MS ILogger bridge）

- 實作 `ILoggerProvider` + `ISupportExternalScope`。
- 內部 `LogSinkLogger.Log()` 方法：
  - 使用 `DateTimeOffset.UtcNow`。
  - **MessageTemplate 擷取**：若 `state` 為 `IReadOnlyList<KeyValuePair<string, object?>>`，從中取 `{OriginalFormat}` 作為 `MessageTemplate`；其餘 key-value 直接作為 `Properties`（經型別正規化與 Redaction）。
  - **RenderedMessage**：由 `formatter(state, exception)` 產出。
  - **Scope 與 Activity**：透過 `ISupportExternalScope` 拿到 scope 資料，合併至 `Properties`；若 `Activity.Current` 存在，將 `trace_id`、`span_id` 寫入 `Properties`。
  - Sink 寫入採 `_sink.WriteAsync(entry).GetAwaiter().GetResult()`，外層 `try-catch` 靜默忽略。
  - Level 映射使用 REQ-CORE-02 集中 mapper。
- 實作 `IDisposable`，在 Dispose 時持有的 sink 若為 `IDisposable` 則一併 Dispose。

#### REQ-CORE-04：DefaultLogMaintenance

- `ExplicitDeleteDates` 底層改用 `ConcurrentDictionary<DateOnly, byte>` 以確保執行緒安全。
- `RetentionDays` 從建構子注入的 `IOptions<LoggingOptions>` 讀取。

#### REQ-CORE-05：LoggingOptions — 全域選項

```
LoggingOptions:
  RetentionDays : int?    // null = 不自動清除
```

#### REQ-CORE-06：DI 擴充方法

- `AddBirdsoftLoggingCore()`：
  - 註冊 `ILogMaintenance`、`IAppLogger<T>`。
  - 使用 `TryAddSingleton`（不再 Replace）。
- `AddAppLogging(bool clearExistingProviders = false, Action<ILoggingBuilder>? configure = null)`：
  - 註冊 `LogSinkLoggerProvider` 至 MS Logging pipeline。
  - `clearExistingProviders` 預設為 `false`，不主動清除既有 provider（如 Console、Debug）。
  - 呼叫端可傳入 `true` 以清除既有 provider，只保留 Birdsoft sink。

#### REQ-CORE-07：MessageTemplateParser — 佔位符解析

- 解析 message template 中的 `{Name}` 佔位符。
- 輸入：`messageTemplate` + `args`。
- 輸出：
  - `renderedMessage`：將佔位符替換為 args 的字串表示。
  - `properties`：`Dictionary<string, object?>` 以佔位符名稱為 key。

---

### 2.3 Json — JSONL 檔案式 Store

#### REQ-JSON-01：ILogFilePathProvider

- 此介面從 Abstractions **移至** `Logging.Json` 專案（只有 Json store 需要）。

#### REQ-JSON-02：DefaultLogFilePathProvider

- 根據 `DateOnly` 產生 `{root}/{yyyy-MM-dd}.jsonl` 路徑。

#### REQ-JSON-03：JsonFileLogStore

- 同時實作 `ILogStore` + `ILogSink`。
- `WriteAsync`：SemaphoreSlim 保護、FileStream append、逐行 JSONL。
- `GetLogsAsync(LogQuery)`：支援 `MinLevel`、`Category`、`Keyword` 的記憶體端篩選；依 `OrderByTimestampDescending` 排序。
- `GetLogDatesAsync`：掃描 `*.jsonl` 檔名解析日期。
- `DeleteLogsAsync`：刪除對應日期的 `.jsonl` 檔。
- 實作 `IDisposable`：Dispose `SemaphoreSlim`。
- `ExceptionDetail` 作為 JSON 字串欄位序列化；不再序列化 `Exception` 物件。
- **JSONL 讀取容錯**：讀取時須容忍最後一行寫到一半（截斷的 JSON），遇到反序列化失敗的行一律跳過（不拋出例外）。

#### REQ-JSON-04：JsonLoggingOptions

```
JsonLoggingOptions:
  RootDirectory : string   // 預設 "logs"
```

- `RetentionDays` 不再於此 Options 持有，改由全域 `LoggingOptions` 統一管理。

#### REQ-JSON-05：DI 擴充方法

- `AddBirdsoftJsonLogging(Action<JsonLoggingOptions>)`：
  - 呼叫 `AddBirdsoftLoggingCore()`。
  - 註冊 `JsonFileLogStore` 為 `ILogStore` + `ILogSink`（`TryAddSingleton`）。
  - **不再** `Replace` `ILogMaintenance`。

---

### 2.4 Sqlite — SQLite 式 Store

#### REQ-SQL-01：SqliteLogStore

- 同時實作 `ILogStore` + `ILogSink`。
- **維護長期存活的連線**，而非每次操作開新連線。
- 啟用 WAL 模式（`PRAGMA journal_mode=WAL`）。
- 初始化時建立 `Logs` 表格 + 索引。
- **Level 存為 INTEGER（0~6）**，對應 `LogLevel` enum 值，利於 SQL 篩選（`WHERE Level >= @minLevel`）。
- `GetLogsAsync(LogQuery)`：使用 SQL `WHERE` 條件篩選（`MinLevel`、`Category`、`Keyword`）；依 `OrderByTimestampDescending` 在 SQL `ORDER BY` 中排序。
- `ExceptionDetail` 存為 TEXT 欄位。
- 增加 `RenderedMessage` 欄位、`MessageTemplate` 欄位。
- 實作 `IDisposable` / `IAsyncDisposable`：關閉並釋放持久連線與 SemaphoreSlim。

#### REQ-SQL-02：SqliteLoggingOptions

```
SqliteLoggingOptions:
  ConnectionString : string
```

- `RetentionDays` 不再於此 Options 持有，改由全域 `LoggingOptions` 統一管理。

#### REQ-SQL-03：DI 擴充方法

- `AddBirdsoftSqliteLogging(Action<SqliteLoggingOptions>)`：
  - 呼叫 `AddBirdsoftLoggingCore()`。
  - 註冊 `SqliteLogStore` 為 `ILogStore` + `ILogSink`（`TryAddSingleton`）。
  - **不再** `Replace` `ILogMaintenance`。

---

### 2.5 Tests

#### REQ-TEST-01：測試覆蓋範圍

重構完成後，測試應涵蓋：

| 測試類別 | 涵蓋目標 |
|---------|---------|
| `LogEntryRedactionTests` | Redaction 自動遮蔽各種 pattern、ResetToDefaults、Properties 型別正規化 |
| `MessageTemplateParserTests` | 佔位符解析、命名對應、邊界案例 |
| `LevelMapperTests` | 雙向映射完整性、None 處理、未知值 fallback |
| `SerilogAppLoggerTests` | Log 行為、sink 失敗靜默、IsEnabled、Activity trace_id/span_id、Activity trace_id/span_id |
| `LogSinkLoggerProviderTests` | MS ILogger → store 寫入、{OriginalFormat} 擷取、scope/Activity 落入 Properties、sink 拋錯保護 |
| `DefaultLogMaintenanceTests` | Retention、ExplicitDelete、邊界（0 天、無日期、並行安全） |
| `JsonFileLogStoreTests` | Write/Query/Delete、LogQuery 篩選、排序、截斷行容錯、Dispose |
| `SqliteLogStoreTests` | Write/Query/Delete、LogQuery 篩選（SQL WHERE）、Level INTEGER、排序、WAL、Dispose |
| `MicrosoftLoggerToStoreTests` | 端到端：ILogger → sink → store → query |
| `LoggingProjectSmokeTests` | DI 註冊整合驗證 |

---

## 3. 非功能需求

### NFR-01：不使用背景服務

所有寫入操作在呼叫端執行緒完成（同步或 await），不引入 `Channel`、`BackgroundService` 等背景排隊機制，以保持簡單性。

### NFR-02：Redaction 安全

日誌中不得出現 access_token、refresh_token、client_secret、密碼、Bearer token、完整 OAuth 驗證連結等敏感資訊。

### NFR-03：Timestamp 一致性

所有路徑一律使用 `DateTimeOffset.UtcNow`，不混用本機時區。

### NFR-04：資源管理

持有非託管或 disposable 資源的類別必須正確實作 `IDisposable` / `IAsyncDisposable`。

### NFR-05：執行緒安全

共用可變集合（如 `ExplicitDeleteDates`）必須使用 thread-safe 資料結構。

### NFR-06：可測試性

- 所有核心介面可 mock/stub。
- 不依賴外部服務（Serilog 靜態 Logger 以 DI 注入覆蓋）。
- `LogEntryRedactor` 提供 `ResetToDefaults()` 靜態方法，供測試重置靜態自訂 pattern，避免測試間汙染。

---

## 4. 與舊版的 Breaking Changes 摘要

| 項目 | 舊版（v0.5.0） | 新版（v0.1.0） |
|------|---------------|---------------|
| `LogEntry.Exception` | `Exception?` | 移除，改為 `string? ExceptionDetail` |
| `LogEntry.Message` | 單一欄位（語意混亂） | 拆為 `MessageTemplate` + `RenderedMessage` |
| `LogEntry.Properties` | `["Args"] = object[]` | 佔位符命名 key-value |
| `ILogStore.GetLogsAsync` | `GetLogsAsync(DateOnly)` | `GetLogsAsync(LogQuery)` |
| `ILogMaintenance.RetentionDays` | 介面屬性（可 set） | 移除，改由 `LoggingOptions` 提供 |
| `ILogFilePathProvider` 位置 | Abstractions | 移至 Logging.Json |
| `LogLevel` | 無 `None` | 新增 `None = 6` |
| DI `Replace` 語意 | Json/Sqlite 覆蓋 `ILogMaintenance` | 統一 `TryAddSingleton` |
| SQLite 連線 | 每次開新連線 | 長期存活 + WAL |
| SQLite Level 欄位 | TEXT（字串） | INTEGER（0~6） |
| Timestamp | Now / UtcNow 混用 | 統一 `UtcNow` |
| `IAppLogger.Log()` sink 寫入 | fire-and-forget | `GetAwaiter().GetResult()` + try-catch |
| `AddAppLogging()` | 內建 `ClearProviders()` | `clearExistingProviders` 參數（預設 false） |
| MS ILogger bridge | formatter → Message | `{OriginalFormat}` → MessageTemplate, state kv → Properties, formatter → RenderedMessage |
| Properties value | 無型別約束 | 僅支援基本型別，其餘轉 string |
| Observability | 無 | Activity.Current trace_id/span_id + scope → Properties |
| LogQuery 排序 | 未定義 | 契約保證 Timestamp 排序（ASC/DESC） |
| JSONL 讀取 | 截斷行會例外 | 容忍尾行截斷，skip 失敗行 |
