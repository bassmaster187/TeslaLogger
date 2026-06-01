# TeslaLogger — Copilot Instructions

## Repository
- **Owner:** bassmaster187 · **Default branch:** `master`
- **GitHub:** github.com/bassmaster187/TeslaLogger

## Platform & Stack
- **Target:** Raspberry Pi 3B · ARM32 (ARMv7) · 1 GB RAM · SD card (slow I/O)
- **Framework:** .NET 8 (`net8.0`) · C# 12 · nullable reference types enabled
- **Solution:** `TeslaLoggerNET8.sln` (PRIMARY) · `TeslaLogger.sln` deprecated (.NET 4.8)
- **Database:** MariaDB 10.3.39 (InnoDB, Dynamic row format) on RasPi
- **Database:** MariaDB 10.4.7 (InnoDB, Dynamic row format) on Docker
- **Key packages:** Newtonsoft.Json, SkiaSharp (+ NativeAssets.Linux), MySql.Data, M2Mqtt, NetMQ, Exceptionless, Microsoft.AspNetCore.SystemWebAdapters
- **External:** Grafana (docker), Apache Kafka (optional), Exceptionless (self-hosted), OpenStreetMap/MapQuest (optional)

## Project Structure
```
TeslaLogger/                    # Main app (TeslaLoggerNET8.csproj) — Program.cs, WebServer.cs, DBHelper.cs, Car.cs, MQTT*.cs, TelemetryConnection*.cs, ElectricityMeter*.cs, www/
TeslaLogger/TeslaLogger.csproj  # DEPRECATED — .NET Framework 4.8
Logfile/                        # Logging lib (LogfileNET8.csproj) — Logfile.cs
OSMMapGenerator/                # Map tiles (OSMMapGeneratorNET8.csproj) — SkiaSharp
srtm/src/SRTM/                  # Elevation data (SRTMNET8.csproj) — SRTMData.cs
KafkaConnector/                 # Kafka + Protobuf integration (KafkaConnector.csproj) — Confluent.Kafka, .proto files
UnitTestsTeslalogger/           # Tests (UnitTestsTeslaloggerNET8.csproj) — MSTest + NUnit + Selenium
TLUpdate/                       # Self-update utility (TLUpdate.csproj)
MQTTClient/                     # MQTT client lib (MQTTClientNET8.csproj)
KML_Import/                     # KML import tool (KML_Import.csproj / KML_Import.sln)
TeslaFi-Import/                 # TeslaFi data import (TeslaFi-Import.csproj / TeslaFi-Import.sln)
Teslamate-Import/               # Teslamate data import (Teslamate-Import.csproj / Teslamate-Import.sln)
MockServer/                     # Mock server for testing
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
- Schema: `TeslaLogger/sqlschema.sql`

### Null Safety
- Nullable reference types enabled. Use `?` for nullable returns, `??` for safe defaults.
- Prefer pattern matching: `if (car is { State: TeslaState.Charge, BatteryLevel: >= 80 })`.

### .NET 8 Features
- Collection expressions: `["a", "b"]` instead of `new List<string>() { "a", "b" }`.
- Target-typed `new()`: `Dictionary<string, int> _cache = new();`

### Security
- Admin passwords: bcrypt or equivalent.
- Tesla API tokens: encrypted at rest.
- DB credentials: environment variables only (`.env` file).
- API commands: validate against whitelist (`AllowedTeslaAPICommands`).
- File paths: validate to prevent directory traversal.
- HTTP listener: bind to specific ports. No external exposure without reverse proxy.
- MQTT: authentication enabled.

### Error Handling & Logging
- Wrap in try-catch, log with `Logfile.Log()` / Exceptionless. Never swallow exceptions silently.
- Log levels: Error, Warning, Info, Debug, SQL.
- Exception files written to `Exception/` folder.

### Protobuf (KafkaConnector)
- `.proto` files in `KafkaConnector/protos/` — use `Grpc.Tools` for code generation.
- Topics: `vehicle_data`, `vehicle_alert`, `vehicle_metric`, `vehicle_connectivity`, `vehicle_error`.

## Naming & Style
- Classes/methods: `PascalCase`. Fields: `_camelCase`. Constants: `PascalCase`. Interfaces: `IPascalCase`.
- One class per file. Namespace = project name.
- XML docs on public APIs. Inline comments for complex logic. TODO → GitHub issue ref.

## Testing
- **Framework:** Mixed — MSTest (`MSTest.TestAdapter`, `MSTest.TestFramework`) + NUnit (`NUnit3TestAdapter`) + Moq
- **UI:** Selenium with ChromeDriver
- **Test project:** `UnitTestsTeslalogger/UnitTestsTeslaloggerNET8.csproj`
- **Focus:** State machine (`Car.TeslaState`), data parsing, API command validation
- **Integration:** MariaDB test containers, MQTTnet mock, MockServer for Tesla API
- **Performance:** Memory leak checks, DB write throughput, concurrent connections
- **Test data:** `UnitTestsTeslalogger/testdata/`

## Build & Run
```bash
dotnet restore TeslaLoggerNET8.sln
dotnet build TeslaLoggerNET8.sln -c Debug
dotnet publish TeslaLogger/TeslaLoggerNET8.csproj -c Release -r linux-arm --self-contained false -o ./publish
dotnet run --project TeslaLogger/TeslaLoggerNET8.csproj
dotnet test UnitTestsTeslalogger/UnitTestsTeslaloggerNET8.csproj
docker compose up -d
```

## Docker
- **Base image:** `bassmaster187/teslalogger-base:1.0.0` (custom)
- **Services (docker-compose.yml):**
  - `teslalogger` — main app (`bassmaster187/teslalogger:latest`)
  - `database` — MariaDB 10.4.7
  - `grafana` — dashboards (`bassmaster187/teslalogger-grafana:latest`)
  - `webserver` — reverse proxy (`bassmaster187/teslalogger-webserver:latest`)
- **Entry point:** `dotnet ./TeslaLoggerNET8.dll`
- **Volumes:** named volumes for data, SQL schema, Grafana dashboards/plugins
- **Auto-update:** Watchtower labels on all containers
- **Config:** `.env` file for ports, paths, timezone

## Architecture Decisions
- **ADR-001:** .NET 8 LTS (ARM32 support, performance, reduced footprint)
- **ADR-002:** Async-first (prevent thread pool exhaustion on constrained hardware)
- **ADR-003:** MariaDB over SQLite (concurrent writes, network access, Grafana integration)
