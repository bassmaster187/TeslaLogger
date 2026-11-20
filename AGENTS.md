# Development Context

## Build & Run

- **Solution**: `TeslaLoggerNET8.sln` (the active .NET 8 solution). `TeslaFi-Import.sln` is legacy.
- **Build**: `dotnet build TeslaLoggerNET8.sln`
- **Run (VS Code)**: Debug config in `.vscode/launch.json` uses preLaunchTask `build TeslaLoggerNET8`.
- **Run (CLI)**: `dotnet run --project TeslaLogger/TeslaLoggerNET8.csproj`

## Docker

- **Start**: `docker compose up -d`
- **Config**: `.env` controls ports (`TESLALOGGER_PORT=5010`, `GRAFANA_PORT=3000`, `WEBSERVER_PORT=8888`), `APPDATA_PATH`, and `TZ`.
- **Services**: `teslalogger` (main app), `database` (MariaDB), `grafana`, `webserver` (nginx), `watchtower`.
- **MariaDB creds**: user `teslalogger`, password `teslalogger`, database `teslalogger`.
- **Grafana**: `http://localhost:3000` / admin / teslalogger
- **Admin panel**: `http://localhost:8888/admin/`
- **Debugging webserver from host**: Change `GetTeslaloggerURL()` in `www/admin/tools.php` and `teslaloggerstream.php` to use `host.docker.internal` (Windows).
- **DB init slowness**: MariaDB may take 10-30 min on slow hardware. Wait for `running /docker-entrypoint-initdb.d/sqlschema.sql` in logs before the app connects.
- **Docker rebuilds**: Release CI builds 3 images (`Dockerfile`, `Dockerfile-Webserver`, `Dockerfile-Grafana`) for `linux/amd64,linux/arm64`.

## Tests

- **Project**: `UnitTestsTeslalogger\UnitTestsTeslaloggerNET8.csproj` (MSTest + NUnit + Playwright).
- **Run all**: `dotnet test UnitTestsTeslalogger\UnitTestsTeslaloggerNET8.csproj`
- **CI filter**: `--filter "UnitTestsGeocode|UnitTestGrafana|UnitTestBase|UnitTestTelemetryParser|UnitTestCO2"` (some tests need DB).
- **Playwright UI tests**: `PlaywrightGeofencingTests.cs`, `PlaywrightFatalErrorTests.cs` — require Chrome (`PLAYWRIGHT_HEADLESS=1` for headless) and a running Teslalogger (Admin `http://localhost:8888/admin/`, API `http://localhost:5010/`, override with `TESLALOGGER_ADMIN_URL` / `TESLALOGGER_API_URL`). They report `Inconclusive` when the app is unreachable. Screenshots: `TestResults/PlaywrightScreenshots/`. Admin panel protected by a password: set `TESLALOGGER_ADMIN_USER` / `TESLALOGGER_ADMIN_PASSWORD`.
- **Caution**: Tests may modify local data. Back up before running.

## Translations

- **ALWAYS translate a new or changed text into every language.** When a `t("...")` string is added or changed in an admin page, add the key to **all 15 files** `TeslaLogger/bin/language-*.txt` (cn, da, de, en, es, fr, it, nl, no, pl, pt, ru, sv, ta, tr) with a real translation in that language. Never leave the value empty, never reuse the English or German text as a placeholder, and never rely on the English fallback — the fallback only covers keys that genuinely do not exist in a file, not missing translations.
- The key is always the **English source text** exactly as written in `t("...")`; the part after `=` is the translation. `language-en.txt` repeats the key as its value.
- Insert the line inside the section comment of the page that uses it (e.g. `#geoadd.php`), keep the file's existing position order, and do not reformat anything else: these files are UTF-8 **with BOM** and CRLF — a whole-file rewrite that drops the BOM or the CRLF shows up as a diff of the complete file.
- Values containing a leading/trailing space must be quoted (`Key="Value"`), embedded quotes are written as `"_QQ_"`. The parser is `explode("=")` on the first `=`, so keys must not contain `=`.
- Verify after editing: each language file must return the new key, e.g. `Select-String -Path TeslaLogger\bin\language-*.txt -Pattern '^New key='` — 15 hits expected.
- The running webserver container serves `/tmp/language-*.txt`, so `docker cp` the changed files into `teslalogger-webserver:/tmp/` to preview them without a rebuild.

## Project Structure

| Directory | Purpose |
|---|---|
| `TeslaLogger/` | Main app — `Program.cs` entry point, `WebServer.cs` (HttpListener), PHP admin pages in `www/admin/` |
| `Logfile/` | Logging utility library |
| `srtm/` | SRTM elevation data |
| `OSMMapGenerator/` | Map image generation |
| `KafkaConnector/` | Kafka integration |
| `MQTTClient/` | MQTT client library |
| `TeslaFi-Import/` | TeslaFi CSV importer (mono/.NET Framework) |
| `Teslamate-Import/` | Teslamate data importer |
| `KML_Import/` | KML import tool |
| `TLUpdate/` | Online update logic |
| `UnitTestsTeslalogger/` | Unit + integration tests |
| `C:\dev\telemetryconnector` | **External project** — the fleet telemetry server (separate repo, not in this solution) |

## Telemetry Connector (external project)

Location: `C:\dev\telemetryconnector` (own git repo, own `AGENTS.md`). This is the fleet telemetry server the `TelemetryConnectionWS.cs` clients connect to (login via WS, server pushes fleet telemetry config to Tesla Fleet API, streams Kafka data back).

- **Solution**: `telemetryconnector.sln` — main project `telemetryconnector.csproj` (net8.0, Exe, version in csproj) + `telemetryconnector.Tests/` (xunit).
- **Build**: `dotnet build telemetryconnector.sln` in `C:\dev\telemetryconnector`.
- **Tests**: `dotnet test telemetryconnector/telemetryconnector.Tests` — currently only `IsRavenTests.cs`.
- **Run (Docker)**: `docker compose up -d` — port `4500:4500`, volume `./bin/Debug/net8.0:/app`; `Dockerfile` builds librdkafka from source (Confluent.Kafka native dep).
- **Entry point**: `Program.cs` — loads `appsettings.json` (Kafka `BootstrapServers`, `GroupID`, `KafkaSubscribe` topics, `waitForClients`, `DEBUG_LOG_VIN`) and `fleet_telemetry_config_*.txt`, starts WS server + Kafka consumer.
- **`TelemetryServer.cs`** — WebSocket server. `Handle_Login` maps VIN→client, picks config variant (free/paid + `2`/`3` firmware suffix, `raven-` prefix), `SendConfigAsync` pushes config to Tesla `api/1/vehicles/fleet_telemetry_config` using the client's `accesstoken` + `regionurl`. On Tesla 401 `token expired (401)` it sends a `LoginRespone` error message to clients ≥1.60.2.0 and disposes the client (close without handshake). `SentConfig` (per-VIN) only tracks successfully sent configs.
- **`KafkaConnector.cs`** — consumes `tesla_telemetry_V`, `tesla_telemetry_alerts`, `tesla_telemetry_errors`; protobuf payloads (protos in `protos/`, C# generated by Grpc.Tools at build time) forwarded to WS clients by VIN.
- **`WebserverMapping.cs`** — HTTP: `/ws`, `/status`, `/metrics`, `/SignalCounter`, `/UpdateSubscriptions`.
- **Gotchas**: Kafka needs SSH tunnel (`Tunnel_Rootserver_Kafka.bat`) or `BootstrapServers`; `cert/fullchain.pem` required at startup; `waitForClients: true` blocks consumer start; `hsSubscription` whitelist forces `free` config; subscription list + Tasker token updates come from `rootserver.teslalogger.de`.

## Architecture Notes

- **DB schema**: `TeslaLogger/sqlschema.sql` — initialized into MariaDB on first Docker run.
- **Settings**: `settings.json` at runtime, `ApplicationSettings.settings` (Designer) at build time.
- **Car threads**: Each Tesla/Lucid car runs on its own `Thread` (see `Program.StartCarThread`).
- **Background tasks**: Housekeeping, geofence updates, DB migrations, telemetry sharing all run in background threads/tasks.
- **Multi-platform**: Runs on Windows, Linux (Raspberry Pi), and Docker. `Tools.RunOnLinux()` and `Tools.IsDocker()` branch logic.
- **Exception tracking**: Exceptionless integration (configurable via settings).

## Import Tools

- **TeslaFi**: `TeslaFi-Import/README.md` — mono-based, reads CSV, writes to DB.
- **Teslamate**: `Teslamate-Import/` — separate importer.
- Import stops at first Teslalogger data point. Geocoding runs after import (slow, rate-limited).

## CI/CD

- **Release** (`.github/workflows/dotnet8.yml`): .NET 8 build → Docker images → Docker Hub. Triggered on `NET8` branch.
- **Desktop** (`.github/workflows/dotnet-desktop.yml`): .NET Framework 4.8 build on `windows-2022`. Runs filtered unit tests.

## Gotchas

- No global `dotnet.json` or `.editorconfig` — project files use SDK-style with legacy properties mixed in.
- `.runsettings-old` exists but no active `.runsettings` — test results go to `TestResults/` by default.
- `packages/` directory exists (legacy NuGet restore). Modern restores go to `~/.nuget/packages`.
- Solution items include Grafana dashboard JSON files and `sqlschema.sql`.
- `TeslaLogger.csproj` (non-NET8) and `TeslaLogger.sln` still exist but are not the active build targets.
- `.vs/` directory — Visual Studio solution state, ignore in non-Windows environments.
