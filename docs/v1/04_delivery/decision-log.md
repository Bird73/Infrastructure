# Birdsoft.Infrastructure.Logging — 設計決策記錄

> 版本：v1 | 狀態：Finalized | 日期：2026-03-02

| # | 決策 | 選項 | 決定 | 理由 | 日期 |
|---|------|------|------|------|------|
| D-01 | 是否使用 Channel / BackgroundService | A) 引入背景佇列 B) 維持同步寫入 | B | 本模組專注 Error log，吞吐量低，避免複雜化 | 2026-03-01 |
| D-02 | Exception 欄位處理 | A) 保留 Exception 物件 B) 改為 string ExceptionDetail | B | 避免 System.Text.Json 序列化問題，字串可正確儲存與讀回 | 2026-03-01 |
| D-03 | Timestamp 來源 | A) DateTimeOffset.Now B) DateTimeOffset.UtcNow C) 注入 TimeProvider | B | 統一時區，避免不同路徑產生不一致 Timestamp | 2026-03-01 |
| D-04 | Redaction 時機 | A) 寫入時（在 sink 內） B) 建構時（LogEntry 建構階段） | B | 確保任何寫入路徑的資料都已遮蔽 | 2026-03-01 |
| D-05 | LogSinkLogger 同步 vs 非同步 | A) fire-and-forget B) GetAwaiter().GetResult() + try-catch | B | ILogger.Log 是同步介面，維持同步確保測試可預測 | 2026-03-01 |
| D-06 | RetentionDays 歸屬 | A) ILogMaintenance 屬性 B) 全域 LoggingOptions | B | 避免多 store 共存時 Replace 衝突 | 2026-03-01 |
| D-07 | ILogFilePathProvider 位置 | A) Abstractions B) Logging.Json | B | 僅 Json store 使用，不污染 Abstractions | 2026-03-01 |
| D-08 | SQLite 連線策略 | A) 每次開新連線 B) 長期存活 + WAL | B | 避免頻繁開關，提升寫入效能 | 2026-03-01 |
| D-09 | Message 欄位設計 | A) 單一 Message 欄位 B) MessageTemplate + RenderedMessage | B | 同時保留結構化查詢能力與人可讀性 | 2026-03-01 |
| D-10 | 查詢 API 設計 | A) GetLogsAsync(DateOnly) B) GetLogsAsync(LogQuery) | B | 提供 level/category/keyword 複合篩選 | 2026-03-01 |
| D-11 | ExplicitDeleteDates 集合 | A) HashSet B) ConcurrentDictionary | B | 確保執行緒安全 | 2026-03-01 |
| D-12 | Level mapper 統一 | A) 各處各自映射 B) 集中 LevelMapper 類別 | B | 減少重複，雙向映射可統一測試 | 2026-03-01 |
| D-13 | 結構化屬性解析 | A) args 存為 object[] B) 解析佔位符名稱建立命名 key-value | B | 提升查詢與偵錯可用性 | 2026-03-01 |
| D-14 | 版本號 | A) 延續 0.5.0 B) 重新起算 0.1.0 | B | 尚未正式發行，相依專案全部重啟 | 2026-03-01 |
| D-15 | IAppLogger.Log() sink 寫入策略 | A) fire-and-forget B) try-catch await C) GetAwaiter().GetResult() + try-catch | C | 與 MS ILogger bridge 統一做法，簡化兩條路徑的同步語意 | 2026-03-01 |
| D-16 | SQLite Level 欄位型別 | A) TEXT（字串） B) INTEGER（0~6） | B | 整數比較利於 SQL 篩選（WHERE Level >= @min），效能優於字串 | 2026-03-01 |
| D-17 | MS ILogger bridge MessageTemplate 來源 | A) 僅用 formatter 結果 B) 從 state 取 {OriginalFormat} | B | 正確取得原始 template，其餘 state kv 直接作為結構化 Properties | 2026-03-01 |
| D-18 | AddAppLogging() ClearProviders 行為 | A) 內建 ClearProviders B) 參數化（預設 false） | B | 避免意外清除宗主既有 provider，由呼叫端決定 | 2026-03-01 |
| D-19 | LogEntryRedactor 可重置機制 | A) 不提供 B) ResetToDefaults() 靜態方法 | B | 靜態類別在測試間可能汙染，提供重置確保測試隔離 | 2026-03-01 |
| D-20 | Properties value 型別約束 | A) 無約束 B) Store 層只支援基本型別 | B | 避免不可序列化型別進入 store，其餘型別經 ToString() + Redaction | 2026-03-01 |
| D-21 | Observability：Activity + scope | A) 不納入 B) trace_id/span_id + scope 落入 Properties | B | 提升可追蹤性，運用既有 ISupportExternalScope 實作 | 2026-03-01 |
| D-22 | GetLogsAsync 排序契約 | A) 未定義 B) 契約保證 Timestamp 排序（ASC/DESC） | B | 確保消費端取得確定性排序結果 | 2026-03-01 |
| D-23 | JSONL 讀取容錯 | A) 截斷行拋例外 B) skip 失敗行 | B | 行程序崩潰時尾行可能不完整，讀取端應 graceful skip | 2026-03-01 |
