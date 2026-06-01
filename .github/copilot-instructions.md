# TeslaLogger — Copilot Instructions

## Platform & Stack
- **Target:** Raspberry Pi 3B · ARM32 (ARMv7) · 1 GB RAM · SD card (slow I/O)
- **Framework:** .NET 8 (`net8.0`) · C# 12 · nullable reference types enabled
- **Solution:** `TeslaLoggerNET8.sln` (PRIMARY) · `TeslaLogger.sln` deprecated (.NET 4.8)
- **Database:** MariaDB 10.3.39 (InnoDB, Dynamic row format)
- **Key packages:** Newtonsoft.Json, SkiaSharp (+ NativeAssets.Linux), MySql.Data, M2Mqtt, NetMQ, Exceptionless, Microsoft.AspNetCore.SystemWebAdapters
- **External:** Grafana (docker), Apache Kafka (optional), Exceptionless (self-hosted), OpenStreetMap/MapQuest (optional)

## Project Structure
```
TeslaLogger/          # Main app (TeslaLoggerNET8.csproj) — Program.cs, WebServer.cs, DBHelper.cs, Car.cs, MQTT*.cs, TelemetryConnection*.cs, ElectricityMeter*.cs, www/
Logfile/              # Logging lib (LogfileNET8.csproj) — Logfile.cs
OSMMapGenerator/      # Map tiles (OSMMapGeneratorNET8.csproj)
srtm/src/SRTM/        # Elevation data (SRTMNET8.csproj) — SRTMData.cs
KafkaConnector/       # Kafka integration
TLNUnit/              # NUnit tests
```

## MariaDB — Local Access
```bash
/opt/homebrew/bin/mysql -h teslalogger -u root -pteslalogger --skip-ssl teslalogger
```

## Mandatory Coding Rules

### Async-First (CRITICAL)
- All I/O must be `async/await`. Never use `.Result` / `.Wait()` / sync I/O on request threads.
- Every async method carries `CancellationToken ct = default` and propagates it.
- Async methods end with `Async` suffix.
- Use `await using` for all `IDisposable` async resources (connections, commands, readers).
- Use `SemaphoreSlim` instead of `lock`.

### Memory (1 GB RAM)
- Stream/batch large datasets — never `.ToList()` unbounded queries.
- Use `ArrayPool<T>` for buffers. Bound collection capacities.
- Unsubscribe event handlers or use weak events / CancellationToken.
- Prefer deferred LINQ; avoid multiple enumerations.

### I/O (SD Card)
- Batch DB writes. Use connection pooling (built-in to MySql.Data).
- Cache hot data with `MemoryCache` + expiration.
- Buffered/async logging via `Logfile.LogAsync()`. Minimize SD card writes.
- Use `HttpClient` with pooling for network calls.

### Database
- Parameterized queries only (never string concatenation for SQL).
- Standard pattern: `await using var conn = new MySqlConnection(...); await conn.OpenAsync(ct); await using var cmd = new MySqlCommand(sql, conn);`
- Ensure indexes on queried columns. Avoid N+1 patterns.

### Null Safety
- Nullable reference types enabled. Use `?` for nullable returns, `??` for safe defaults.
- Prefer pattern matching: `if (car is { State: TeslaState.Charge, BatteryLevel: >= 80 })`.

### .NET 8 Features
- Collection expressions: `["a", "b"]` instead of `new List<string>() { "a", "b" }`.
- Target-typed `new()`: `Dictionary<string, int> _cache = new();`

### Security
- Admin passwords: bcrypt or equivalent.
- Tesla API tokens: encrypted at rest.
- DB credentials: environment variables only.
- API commands: validate against whitelist (`AllowedTeslaAPICommands`).
- File paths: validate to prevent directory traversal.
- HTTP listener: bind to specific ports. No external exposure without reverse proxy.
- MQTT: authentication enabled.

### Error Handling & Logging
- Wrap in try-catch, log with `Logfile.Log()` / Exceptionless. Never swallow exceptions silently.
- Log levels: Error, Warning, Info, Debug, SQL.

## Naming & Style
- Classes/methods: `PascalCase`. Fields: `_camelCase`. Constants: `PascalCase`. Interfaces: `IPascalCase`.
- One class per file. Namespace = project name.
- XML docs on public APIs. Inline comments for complex logic. TODO → GitHub issue ref.

## Testing
- **Unit:** NUnit in `TLNUnit/`. Target >60% coverage on critical paths.
- **Focus:** State machine (`Car.TeslaState`), data parsing, API command validation.
- **Integration:** MariaDB test containers, MQTTnet mock, MockServer for Tesla API.
- **Performance:** Memory leak checks, DB write throughput, concurrent connections.

## Build & Run
```bash
dotnet restore TeslaLoggerNET8.sln
dotnet build TeslaLoggerNET8.sln -c Debug
dotnet publish TeslaLogger/TeslaLoggerNET8.csproj -c Release -r linux-arm --self-contained false -o ./publish
dotnet run --project TeslaLogger/TeslaLoggerNET8.csproj
dotnet test TLNUnit/TLNUnit.csproj
docker compose up -d
```

## Docker
- Multi-stage build: `mcr.microsoft.com/dotnet/sdk:8.0` → `mcr.microsoft.com/dotnet/aspnet:8.0`
- Services: teslalogger, mariadb:10.3, grafana:latest
- Volume: `./data:/app/data`

## Architecture Decisions
- **ADR-001:** .NET 8 LTS (ARM32 support, performance, reduced footprint)
- **ADR-002:** Async-first (prevent thread pool exhaustion on constrained hardware)
- **ADR-003:** MariaDB over SQLite (concurrent writes, network access, Grafana integration)
