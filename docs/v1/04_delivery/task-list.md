# Birdsoft.Infrastructure.Logging — 工作項目清單

> 版本：v1 | 狀態：Draft | 日期：2026-03-01

## 里程碑規劃

本次重構採**三個里程碑**分階段交付，確保每個里程碑結束時模組可編譯、可測試。

| 里程碑 | 名稱 | 範圍 | 完成標準 |
|--------|------|------|---------|
| M1 | Abstractions + Core 重構 | 所有公開契約定義 + Core 內部元件 | 編譯通過、Core 單元測試通過 |
| M2 | Store 實作重構 | Json / Sqlite store 重構 | 編譯通過、Store 整合測試通過 |
| M3 | 整合驗證 + 收尾 | 端到端測試、DI 煙霧測試、文件完成 | 全部測試通過、文件更新 |

---

## M1：Abstractions + Core 重構

### Abstractions 專案

- [ ] REQ-ABS-01 LogLevel enum 新增 `None = 6`
- [ ] REQ-ABS-02 LogEntry 模型重構
  - [ ] 移除 `Exception? Exception`，新增 `string? ExceptionDetail`
  - [ ] `Message` 拆為 `MessageTemplate` + `RenderedMessage`
  - [ ] `Properties` 改為命名 key-value（移除 `["Args"]` 慣例）
- [ ] REQ-ABS-07 新增 `LogQuery` 類別
  - [ ] 包含 `OrderByTimestampDescending` 屬性（預設 false = ASC）
- [ ] REQ-ABS-06 `ILogStore.GetLogsAsync` 簽章改為接受 `LogQuery`
- [ ] REQ-ABS-04 `IAppLogger` / `IAppLogger<T>` 介面定義
- [ ] REQ-ABS-05 `ILogSink` 介面定義
- [ ] REQ-ABS-08 `ILogMaintenance` 移除 `RetentionDays` 屬性
- [ ] REQ-JSON-01 移除 `ILogFilePathProvider`（移至 Json 專案）
- [ ] 更新 Abstractions csproj 版本為 `0.1.0`

### Core 專案

- [ ] REQ-CORE-05 新增 `LoggingOptions`（含 `RetentionDays`）
- [ ] REQ-CORE-07 新增 `MessageTemplateParser` 靜態類別
- [ ] REQ-ABS-03 新增 `LogEntryRedactor` 靜態類別
  - [ ] 預設 pattern：access_token、refresh_token、client_secret、password、pwd、Bearer token、OAuth 連結
  - [ ] `AddPattern()` 擴充方法
  - [ ] `ResetToDefaults()` 可重置方法（專供測試）
  - [ ] `RedactProperties()` 含型別正規化（不支援型別 → ToString() + Redact）
- [ ] REQ-CORE-02 `LevelMapper`，加入雙向映射（Serilog + MS）
- [ ] REQ-CORE-01 `SerilogAppLogger<T>` 重構
  - [ ] Timestamp 統一 `UtcNow`
  - [ ] 使用 `MessageTemplateParser` 解析 template → properties
  - [ ] 使用 `LogEntryRedactor` 遮蔽（含 Properties 型別正規化）
  - [ ] `ExceptionDetail = exception?.ToString()`
  - [ ] Sink 寫入改為 `GetAwaiter().GetResult()` + try-catch（與 MS bridge 一致）
  - [ ] 若 `Activity.Current` 存在，將 trace_id/span_id 寫入 Properties
- [ ] REQ-CORE-03 `LogSinkLoggerProvider` 重構
  - [ ] Timestamp 統一 `UtcNow`
  - [ ] Level 映射使用 `LevelMapper`
  - [ ] 從 state 取 `{OriginalFormat}` → MessageTemplate，其餘 kv → Properties
  - [ ] formatter 產出 → RenderedMessage
  - [ ] 透過 ISupportExternalScope 取得 scope 資料合併至 Properties
  - [ ] 若 `Activity.Current` 存在，將 trace_id/span_id 寫入 Properties
  - [ ] Properties 型別正規化 + `LogEntryRedactor` 遮蔽
  - [ ] 實作 `IDisposable`
- [ ] REQ-CORE-04 `DefaultLogMaintenance` 重構
  - [ ] `ConcurrentDictionary<DateOnly, byte>` 取代 `HashSet`
  - [ ] `RetentionDays` 從 `IOptions<LoggingOptions>` 注入
- [ ] REQ-CORE-06 DI 擴充方法更新
  - [ ] `AddBirdsoftLoggingCore` 加入 `LoggingOptions` 註冊
  - [ ] 移除 `Replace` 語意，統一 `TryAddSingleton`
  - [ ] `AddAppLogging` 加入 `clearExistingProviders` 參數（預設 false）
- [ ] 更新 Core csproj 版本為 `0.1.0`

### M1 測試

- [ ] `MessageTemplateParserTests`（REQ-TEST-01）
- [ ] `LogEntryRedactionTests`（REQ-TEST-01）
- [ ] `LevelMapperTests`（雙向、None、未知值）（REQ-TEST-01）
- [ ] `SerilogAppLoggerTests`（REQ-TEST-01）
- [ ] `LogSinkLoggerProviderTests`（REQ-TEST-01）
- [ ] `DefaultLogMaintenanceTests`（含並行安全、邊界）（REQ-TEST-01）

---

## M2：Store 實作重構

### Json 專案

- [ ] REQ-JSON-01 `ILogFilePathProvider` 介面移入
- [ ] REQ-JSON-02 `DefaultLogFilePathProvider` 移入（命名空間調整）
- [ ] REQ-JSON-03 `JsonFileLogStore` 重構
  - [ ] 配合新 `LogEntry` 結構（ExceptionDetail, MessageTemplate, RenderedMessage）
  - [ ] `GetLogsAsync(LogQuery)` 記憶體端篩選 + OrderByTimestampDescending 排序
  - [ ] JSONL 讀取容錯：尾行截斷 / JSON 損壞時 skip
  - [ ] 實作 `IDisposable`（釋放 SemaphoreSlim）
- [ ] REQ-JSON-04 `JsonLoggingOptions` 移除 `RetentionDays`
- [ ] REQ-JSON-05 DI 擴充方法移除 `Replace ILogMaintenance`
- [ ] 更新 Json csproj 版本為 `0.1.0`

### Sqlite 專案

- [ ] REQ-SQL-01 `SqliteLogStore` 重構
  - [ ] 長期存活連線 + WAL 模式
  - [ ] Level 欄位改為 INTEGER（0~6）
  - [ ] 資料表結構增加 `RenderedMessage`、`MessageTemplate`、`ExceptionDetail`
  - [ ] `GetLogsAsync(LogQuery)` 使用 SQL WHERE 篩選 + ORDER BY 排序
  - [ ] 實作 `IDisposable` / `IAsyncDisposable`
- [ ] REQ-SQL-02 `SqliteLoggingOptions` 移除 `RetentionDays`
- [ ] REQ-SQL-03 DI 擴充方法移除 `Replace ILogMaintenance`
- [ ] 更新 Sqlite csproj 版本為 `0.1.0`

### M2 測試

- [ ] `JsonFileLogStoreTests`（Write/Query/Delete + LogQuery 篩選 + 排序 + 截斷行容錯 + Dispose）（REQ-TEST-01）
- [ ] `SqliteLogStoreTests`（Write/Query/Delete + LogQuery SQL 篩選 + Level INTEGER + 排序 + WAL + Dispose）（REQ-TEST-01）

---

## M3：整合驗證 + 收尾

### 整合測試

- [ ] `MicrosoftLoggerToStoreTests`（ILogger → sink → store → LogQuery）（REQ-TEST-01）
- [ ] `LoggingProjectSmokeTests`（DI 註冊完整性）（REQ-TEST-01）
- [ ] Redaction 端到端驗證（寫入 → 讀出確認已遮蔽）（REQ-TEST-01）

### 文件收尾

- [ ] 更新各專案 `README.md`
- [ ] 填寫 `traceability.md`
- [ ] 填寫 `acceptance-checklist.md`
- [ ] 填寫 `verification-log.md`
- [ ] 更新 `decision-log.md` 確認所有決策已記錄

### Solution 收尾

- [ ] 建立 `Birdsoft.Infrastructure.sln`（含所有專案）
- [ ] 建立 `Directory.Build.props`（共用屬性）
- [ ] 全部專案編譯通過
- [ ] 全部測試通過（`dotnet test -c Release`）
