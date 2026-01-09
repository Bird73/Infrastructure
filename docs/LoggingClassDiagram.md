# Logging — 類別與關係圖

下圖使用 Mermaid 類別圖（classDiagram）描述目前 Logging 模組中介面（interface）與實作（class）之間的關係與依賴。

```mermaid
classDiagram
    class LogEntry {
      +DateTimeOffset Timestamp
      +LogLevel Level
      +string Category
      +string Message
      +Exception? Exception
      +IReadOnlyDictionary<string, object?> Properties
    }

    class LogLevel {
    }

    interface IAppLogger {
      +bool IsEnabled(LogLevel)
      +void Log(LogLevel, Exception?, string, params object?[])
    }

    interface IAppLogger~T~ {
    }

    interface ILogSink {
      +Task WriteAsync(LogEntry, CancellationToken)
    }

    interface ILogStore {
      +Task<IReadOnlyList~DateOnly~> GetLogDatesAsync()
      +IAsyncEnumerable~LogEntry~ GetLogsAsync(DateOnly)
      +Task DeleteLogsAsync(DateOnly)
    }

    interface ILogMaintenance {
      +int? RetentionDays
      +IReadOnlyCollection~DateOnly~ ExplicitDeleteDates
      +void AddExplicitDeleteDate(DateOnly)
      +Task ExecuteAsync(ILogStore, DateOnly)
    }

    interface ILogFilePathProvider {
      +string GetLogFilePath(DateOnly)
    }

    class SerilogAppLogger~T~ {
    }

    class DefaultLogMaintenance {
    }

    class JsonFileLogStore {
    }

    class DefaultLogFilePathProvider {
    }

    class SqliteLogStore {
    }

    %% Relationships
    IAppLogger <|.. IAppLogger~T~
    IAppLogger <|-- SerilogAppLogger~T~

    ILogSink <|.. JsonFileLogStore
    ILogStore <|.. JsonFileLogStore

    ILogSink <|.. SqliteLogStore
    ILogStore <|.. SqliteLogStore

    ILogMaintenance <|.. DefaultLogMaintenance
    ILogFilePathProvider <|.. DefaultLogFilePathProvider

    SerilogAppLogger~T~ ..> ILogSink : writes to
    JsonFileLogStore o-- DefaultLogFilePathProvider : uses
    DefaultLogMaintenance ..> ILogStore : deletes via
    SqliteLogStore ..> ILogMaintenance : optionally used by

    note for SerilogAppLogger~T~ "Depends on Serilog ILogger\nMaps LogLevel to Serilog"
    note for JsonFileLogStore "File-per-day (jsonl)\nImplements storage and sink"
    note for SqliteLogStore "Stores logs in SQLite table\nImplements storage and sink"
```

說明：
- 箭頭與標註代表：實作 (`<|--`)、介面實作 (`<|..`)、依賴 (`..>`)、組合 (`o--`)。
- 若要在 VS Code 直接預覽，請安裝 "Markdown Preview Mermaid Support" 或使用 PlantUML / Mermaid 擴充。

若要，我可以把同內容產生為 PlantUML (`.puml`) 或直接輸出 PNG/SVG 圖檔並加入 `docs/`。