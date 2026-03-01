# Birdsoft.Infrastructure

Birdsoft 基礎建設層（Infrastructure）目前提供 `Birdsoft.Infrastructure.Logging` v1 實作，包含：

- `Birdsoft.Infrastructure.Logging.Abstractions`
- `Birdsoft.Infrastructure.Logging`
- `Birdsoft.Infrastructure.Logging.Json`
- `Birdsoft.Infrastructure.Logging.Sqlite`
- `Birdsoft.Infrastructure.Logging.Tests`

## Logging 快速使用

### 1) 使用 JSONL Store

```csharp
using Birdsoft.Infrastructure.Logging;
using Birdsoft.Infrastructure.Logging.Json;

var services = new ServiceCollection();
services.AddBirdsoftJsonLogging(
    configureJson: options => options.RootDirectory = "logs",
    configureLogging: options => options.RetentionDays = 7);

services.AddLogging(builder =>
{
    builder.AddAppLogging(clearExistingProviders: false);
});
```

### 2) 使用 SQLite Store

```csharp
using Birdsoft.Infrastructure.Logging;
using Birdsoft.Infrastructure.Logging.Sqlite;

var services = new ServiceCollection();
services.AddBirdsoftSqliteLogging(
    configureSqlite: options => options.ConnectionString = "Data Source=logs.db",
    configureLogging: options => options.RetentionDays = 7);

services.AddLogging(builder =>
{
    builder.AddAppLogging(clearExistingProviders: false);
});
```

## 核心特性

- `LogEntry` 使用 `MessageTemplate` + `RenderedMessage` + `ExceptionDetail`
- 敏感資料 Redaction（token/password/bearer/oauth）
- `LogQuery` 支援等級/分類/關鍵字篩選與時間排序
- JSONL 讀取容錯（尾行截斷 skip）
- SQLite 使用單一長期連線與 WAL

## 驗證命令

```bash
dotnet test Birdsoft.Infrastructure.slnx -c Release
```
