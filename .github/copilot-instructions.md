# TeslaLogger — Copilot Instructions

## Repo & Stack
bassmaster187/TeslaLogger · Branch: `master`
- **Target:** RasPi 3B · ARM32 (ARMv7) · 1 GB RAM · SD card (slow I/O) or Docker
- **Framework:** .NET 8 (`net8.0`) · C# 12 · nullable ref types
- **DB:** MariaDB 10.3 (RasPi) / 10.4 (Docker) with InnoDB & Dynamic row format
- **Pkgs:** Newtonsoft.Json, SkiaSharp (+ NativeAssets.Linux), MySql.Data, M2Mqtt, NetMQ, Exceptionless, Microsoft.AspNetCore.SystemWebAdapters
- **Ext:** Grafana/Exceptionless

## MariaDB Access on RasPi
`/opt/homebrew/bin/mysql -h teslalogger -u root -pteslalogger --skip-ssl teslalogger`

## Coding Rules

### Async-First (CRITICAL)
- All I/O: `async/await`. NO `.Result` / `.Wait()` / sync I/O on request threads.
- Every async method: `CancellationToken ct = default`, propagated downstream.
- Suffix: `Async`. Dispose: `await using`. Locks: `SemaphoreSlim`.

### Memory (1 GB RAM)
- Stream/batch large datasets. NO `.ToList()` on unbounded queries.
- `ArrayPool<T>` for buffers. Bound collection capacities.
- Unsubscribe event handlers / weak events / CancellationToken.
- Prefer deferred LINQ; avoid multiple enumerations.

### I/O (SD Card)
- Batch DB writes. Connection pooling via MySql.Data.
- Cache hot data: `MemoryCache` + expiration.
- Async logging: `Logfile.LogAsync()`. Minimize SD writes.
- HTTP: pooled `HttpClient`.

### Database
- Parameterized queries ONLY. NO string concat for SQL.
- Pattern: `await using var conn = new MySqlConnection(...); await conn.OpenAsync(ct); await using var cmd = new MySqlCommand(sql, conn);`
- Indexes on queried columns. Avoid N+1 patterns.
- Get schema directly from Database.

### Null Safety & .NET 8
- Nullable ref types. Use `?` / `??`. Pattern matching preferred.
- Collection expressions: `["a", "b"]`. Target-typed `new()`: `_cache = new();`

### Security
- Admin passwords: bcrypt. Tesla tokens: encrypted at rest. DB creds: `.env` only.
- API commands: whitelist (`AllowedTeslaAPICommands`). File paths: validate traversal.
- HTTP listener: specific ports only. No external exposure w/o reverse proxy.
- MQTT: auth enabled.

### Error Handling & Logging
- Try-catch + `Logfile.Log()` / Exceptionless. Never swallow silently.
- Levels: Error, Warning, Info, Debug, SQL. Exceptions → `Exception/` folder.

## Style
- `PascalCase` methods/classes/constants. `_camelCase` fields. `IPascalCase` interfaces.
- One class/file. Namespace = project name. Inline comments for complex logic.

## Docker
- **Base:** `bassmaster187/teslalogger-base:1.0.0` · **Entry:** `dotnet ./TeslaLoggerNET8.dll`
- **Services:** teslalogger / database (MariaDB 10.4.7) / grafana / webserver
- Volumes for data/schema/plugins. Watchtower auto-update. Config via `.env`.

## When Analyzing Code
- Always complete the investigation in one continuous sequence.
- Do not announce steps without executing them.
- If a method is referenced, immediately open and analyze it.
- Do not stop after stating intent.

## Tool Usage Notes
- File edits: `replace_string_in_file` → fallback to `insert_edit_into_file`
- Code search: `grep_search` (exact text) or `semantic_search` (meaning/patterns)
- Build/run: `run_task` (if exists) → `create_and_run_task` → `run_in_terminal`
