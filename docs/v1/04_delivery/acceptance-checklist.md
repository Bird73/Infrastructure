# Birdsoft.Infrastructure.Logging — 驗收檢查清單

> 版本：v1 | 狀態：Draft | 日期：2026-03-01

## M1：Abstractions + Core 重構

| # | 驗收項目 | REQ-ID | 通過 |
|---|---------|--------|------|
| 1 | `LogLevel` enum 包含 Trace~Critical + None = 6 | REQ-ABS-01 | [ ] |
| 2 | `LogEntry` 包含 MessageTemplate、RenderedMessage、ExceptionDetail；無 Exception 物件 | REQ-ABS-02 | [ ] |
| 3 | `LogQuery` 包含 Date（必要）、MinLevel、Category、Keyword、OrderByTimestampDescending | REQ-ABS-07 | [ ] |
| 4 | `ILogStore.GetLogsAsync` 簽章接受 `LogQuery` | REQ-ABS-06 | [ ] |
| 5 | `IAppLogger` / `IAppLogger<T>` 介面已定義，包含 `IsEnabled` 與 `Log` 方法 | REQ-ABS-04 | [ ] |
| 6 | `ILogSink` 介面已定義，包含 `WriteAsync` 方法 | REQ-ABS-05 | [ ] |
| 7 | `ILogMaintenance` 無 `RetentionDays` 屬性 | REQ-ABS-08 | [ ] |
| 8 | Abstractions 不含 `ILogFilePathProvider` | REQ-JSON-01 | [ ] |
| 9 | `LoggingOptions` 有 `RetentionDays` 屬性 | REQ-CORE-05 | [ ] |
| 10 | `MessageTemplateParser.Parse()` 正確解析佔位符並產生命名 properties | REQ-CORE-07 | [ ] |
| 11 | `LogEntryRedactor.Redact()` 遮蔽各種 pattern + `ResetToDefaults()` 可重置 + `RedactProperties()` 含型別正規化 | REQ-ABS-03 | [ ] |
| 12 | `LevelMapper` 提供四個雙向映射方法、None 正確處理 | REQ-CORE-02 | [ ] |
| 13 | `SerilogAppLogger.Log()` 使用 UtcNow、解析 template、Redact、`GetAwaiter().GetResult()` + try-catch、Activity trace_id/span_id | REQ-CORE-01 | [ ] |
| 14 | `LogSinkLoggerProvider` 使用 UtcNow、LevelMapper、{OriginalFormat}→MessageTemplate、state kv→Properties、scope+Activity、Redact、IDisposable | REQ-CORE-03 | [ ] |
| 15 | `DefaultLogMaintenance` 用 ConcurrentDictionary、RetentionDays 從 IOptions 讀取 | REQ-CORE-04 | [ ] |
| 16 | DI 不使用 Replace，統一 TryAddSingleton；`AddAppLogging` 預設不 ClearProviders | REQ-CORE-06 | [ ] |
| 17 | M1 所有單元測試通過 | REQ-TEST-01 | [ ] |

## M2：Store 實作重構

| # | 驗收項目 | REQ-ID | 通過 |
|---|---------|--------|------|
| 18 | `ILogFilePathProvider` 位於 Logging.Json 命名空間 | REQ-JSON-01 | [ ] |
| 19 | `DefaultLogFilePathProvider` 根據 DateOnly 產生 `{root}/{yyyy-MM-dd}.jsonl` 路徑 | REQ-JSON-02 | [ ] |
| 20 | `JsonFileLogStore.GetLogsAsync(LogQuery)` 支援篩選 + 排序 + 截斷行容錯 | REQ-JSON-03 | [ ] |
| 21 | `JsonFileLogStore` 實作 IDisposable | REQ-JSON-03 | [ ] |
| 22 | `JsonLoggingOptions` 無 RetentionDays | REQ-JSON-04 | [ ] |
| 23 | Json DI 不 Replace ILogMaintenance | REQ-JSON-05 | [ ] |
| 24 | `SqliteLogStore` 使用長期連線 + WAL | REQ-SQL-01 | [ ] |
| 25 | `SqliteLogStore.GetLogsAsync(LogQuery)` 使用 SQL WHERE + ORDER BY | REQ-SQL-01 | [ ] |
| 26 | `SqliteLogStore` 實作 IDisposable + IAsyncDisposable | REQ-SQL-01 | [ ] |
| 27 | Sqlite 資料表 Level 為 INTEGER；含 MessageTemplate、RenderedMessage、ExceptionDetail | REQ-SQL-01 | [ ] |
| 28 | `SqliteLoggingOptions` 無 RetentionDays | REQ-SQL-02 | [ ] |
| 29 | Sqlite DI 不 Replace ILogMaintenance | REQ-SQL-03 | [ ] |
| 30 | M2 所有 Store 測試通過 | REQ-TEST-01 | [ ] |

## M3：整合驗證 + 收尾

| # | 驗收項目 | REQ-ID | 通過 |
|---|---------|--------|------|
| 31 | ILogger → JsonSink → Store → LogQuery 端到端通過 | REQ-TEST-01 | [ ] |
| 32 | DI 煙霧測試通過 | REQ-TEST-01 | [ ] |
| 33 | Redaction 端到端：寫入含敏感資訊 → 讀出確認 [REDACTED] | REQ-TEST-01 | [ ] |
| 34 | 所有 csproj 版本為 0.1.0 | — | [ ] |
| 35 | `dotnet build -c Release` 零警告 | — | [ ] |
| 36 | `dotnet test -c Release` 全部通過 | — | [ ] |
| 37 | README.md 更新反映新 API | — | [ ] |
| 38 | traceability.md 完整 | — | [ ] |
| 39 | verification-log.md 已填寫 | — | [ ] |
