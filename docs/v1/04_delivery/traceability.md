# Birdsoft.Infrastructure.Logging — 追溯矩陣

> 版本：v1 | 狀態：Draft | 日期：2026-03-01

## REQ → 設計 → 測試 → 實作追溯

| REQ-ID | 需求摘要 | 設計參考 | 測試 | 實作入口 |
|--------|---------|---------|------|---------|
| REQ-ABS-01 | LogLevel 新增 None | architecture.md §3.2 | LevelMapperTests | `LogLevel.cs` |
| REQ-ABS-02 | LogEntry 模型重構 | architecture.md §3.1, contracts.md §1.2 | LogEntryRedactionTests | `LogEntry.cs` |
| REQ-ABS-03 | Redaction 敏感資料遮蔽 | architecture.md §3.4, contracts.md §2.4 | LogEntryRedactionTests | `LogEntryRedactor.cs` |
| REQ-ABS-04 | IAppLogger 介面 | contracts.md §1.4 | SerilogAppLoggerTests | `IAppLogger.cs` |
| REQ-ABS-05 | ILogSink 介面 | contracts.md §1.5 | JsonFileLogStoreTests, SqliteLogStoreTests | `ILogSink.cs` |
| REQ-ABS-06 | ILogStore 介面（GetLogsAsync(LogQuery)） | contracts.md §1.6 | JsonFileLogStoreTests, SqliteLogStoreTests | `ILogStore.cs` |
| REQ-ABS-07 | LogQuery 查詢模型（含排序） | architecture.md §3.3, contracts.md §1.3 | JsonFileLogStoreTests, SqliteLogStoreTests | `LogQuery.cs` |
| REQ-ABS-08 | ILogMaintenance（移除 RetentionDays） | contracts.md §1.7 | DefaultLogMaintenanceTests | `ILogMaintenance.cs` |
| REQ-CORE-01 | SerilogAppLogger 重構（sync+Activity） | architecture.md §3.7 | SerilogAppLoggerTests | `SerilogAppLogger.cs` |
| REQ-CORE-02 | LevelMapper 雙向映射 | architecture.md §3.6, contracts.md §2.2 | LevelMapperTests | `LevelMapper.cs` |
| REQ-CORE-03 | LogSinkLoggerProvider 重構（{OriginalFormat}+scope+Activity） | architecture.md §3.8 | LogSinkLoggerProviderTests | `LogSinkLoggerProvider.cs` |
| REQ-CORE-04 | DefaultLogMaintenance 重構 | architecture.md §3.9 | DefaultLogMaintenanceTests | `DefaultLogMaintenance.cs` |
| REQ-CORE-05 | LoggingOptions 全域選項 | architecture.md §3.9, contracts.md §2.1 | DefaultLogMaintenanceTests | `LoggingOptions.cs` |
| REQ-CORE-06 | DI 擴充方法更新 | architecture.md §4 | LoggingProjectSmokeTests | `LoggingServiceCollectionExtensions.cs` |
| REQ-CORE-07 | MessageTemplateParser | architecture.md §3.5, contracts.md §2.3 | MessageTemplateParserTests | `MessageTemplateParser.cs` |
| REQ-JSON-01 | ILogFilePathProvider 移入 Json | architecture.md §3.10 | — | `Logging.Json/ILogFilePathProvider.cs` |
| REQ-JSON-02 | DefaultLogFilePathProvider | architecture.md §3.10 | JsonFileLogStoreTests | `Logging.Json/DefaultLogFilePathProvider.cs` |
| REQ-JSON-03 | JsonFileLogStore 重構 | architecture.md §3.10 | JsonFileLogStoreTests | `JsonFileLogStore.cs` |
| REQ-JSON-04 | JsonLoggingOptions 移除 RetentionDays | contracts.md §3.2 | LoggingProjectSmokeTests | `JsonLoggingOptions.cs` |
| REQ-JSON-05 | Json DI 擴充方法 | architecture.md §4 | LoggingProjectSmokeTests | `JsonLoggingServiceCollectionExtensions.cs` |
| REQ-SQL-01 | SqliteLogStore 重構（Level INTEGER+排序） | architecture.md §3.11 | SqliteLogStoreTests | `SqliteLogStore.cs` |
| REQ-SQL-02 | SqliteLoggingOptions 移除 RetentionDays | contracts.md §4.1 | LoggingProjectSmokeTests | `SqliteLoggingOptions.cs` |
| REQ-SQL-03 | Sqlite DI 擴充方法 | architecture.md §4 | LoggingProjectSmokeTests | `SqliteLoggingServiceCollectionExtensions.cs` |
| REQ-TEST-01 | 測試覆蓋範圍 | requirements.md §2.5 | 全部測試類別 | `Logging.Tests/` |
