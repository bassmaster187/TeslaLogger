# TeslaLogger — Copilot Instructions

## Repo & Stack
bassmaster187/TeslaLogger · Branch: `master`
- **Target:** Pi 3B · ARM32 (ARMv7) · 1 GB RAM · SD card (slow I/O)
- **Framework:** .NET 8 (`net8.0`) · C# 12 · nullable ref types
- **Solution:** `TeslaLoggerNET8.sln` (PRIMARY) · `.sln` deprecated (.NET 4.8)
- **DB:** MariaDB 10.3/10.4 (InnoDB, Dynamic row format)
- **Pkgs:** Newtonsoft.Json, SkiaSharp (+ NativeAssets.Linux), MySql.Data, M2Mqtt, NetMQ, Exceptionless, Microsoft.AspNetCore.SystemWebAdapters
- **Ext:** Grafana/Kafka/Exceptionless/OSM (opt. via docker)

## Project Structure
```
TeslaLogger/              # Main app — Program.cs, WebServer.cs, DBHelper.cs, Car.cs, MQTT*.cs, TelemetryConnection*.cs, ElectricityMeter*.cs, www/
Logfile/                  # Logging lib — Logfile.cs
OSMMapGenerator/          # Map tiles — SkiaSharp
srtm/src/SRTM/            # Elevation data — SRTMData.cs
KafkaConnector/           # Kafka + Protobuf (.proto in protos/)
UnitTestsTeslalogger/     # Tests (MSTest+NUnit+Selenium)
TLUpdate/ MQTTClient/ KML_Import/ TeslaFi-Import/ Teslamate-Import/ MockServer/
```

## MariaDB Local Access
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
- Indexes on queried columns. Avoid N+1 patterns. Schema: `TeslaLogger/sqlschema.sql`.

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

### Protobuf (KafkaConnector)
- `.proto` in `KafkaConnector/protos/`. Code gen via `Grpc.Tools`.
- Topics: `vehicle_data`, `vehicle_alert`, `vehicle_metric`, `vehicle_connectivity`, `vehicle_error`.

## Style
- `PascalCase` methods/classes/constants. `_camelCase` fields. `IPascalCase` interfaces.
- One class/file. Namespace = project name. XML docs on public APIs. Inline comments for complex logic. TODO → GitHub issue ref.

## Testing
- **Framework:** MSTest + NUnit + Moq · **UI:** Selenium/ChromeDriver
- **Project:** `UnitTestsTeslalogger/UnitTestsTeslaloggerNET8.csproj`
- **Focus:** State machine, data parsing, API validation. Integration: MariaDB containers/MQTTnet mock/MockServer.

## Build & Run
```bash
dotnet restore TeslaLoggerNET8.sln && dotnet build -c Debug
dotnet publish TeslaLogger/TeslaLoggerNET8.csproj -c Release -r linux-arm --self-contained false -o ./publish
dotnet run --project TeslaLogger/TeslaLoggerNET8.csproj
dotnet test UnitTestsTeslalogger/UnitTestsTeslaloggerNET8.csproj
docker compose up -d
```

## Docker
- **Base:** `bassmaster187/teslalogger-base:1.0.0` · **Entry:** `dotnet ./TeslaLoggerNET8.dll`
- **Services:** teslalogger / database (MariaDB 10.4.7) / grafana / webserver
- Volumes for data/schema/plugins. Watchtower auto-update. Config via `.env`.

## Architecture Decisions
- ADR-001: .NET 8 LTS · ADR-002: Async-first · ADR-003: MariaDB over SQLite

## Available Tools — Usage Guidelines

### File Operations
| Tool | Purpose | Key Rules |
|------|---------|-----------|
| `read_file` | Read file (line range) | Large chunks preferred. 1-indexed lines. |
| `create_file` | Create new files | Auto-creates parent dirs. Edits only via `replace_string_in_file`. |
| `insert_edit_into_file` | Insert code w/ `...existing code...` | Fallback if `replace_string_in_file` fails. |
| `replace_string_in_file` | Edit existing files (exact match) | 3-5 lines context before/after. ONE occurrence per call. |
| `create_directory` | Recursively create dirs (`mkdir -p`) | Optional — `create_file` auto-creates dirs. |

### Search & Discovery
| Tool | Purpose | Key Rules |
|------|---------|-----------|
| `file_search` | Glob pattern matching (`**/*.cs`) | Returns paths only. Specific patterns avoid slow searches. |
| `grep_search` | Text/regex search across files | Fast. Use `\|` for alternation. Supports `includePattern`. |
| `semantic_search` | Semantic/code meaning (natural language) | Best when exact strings unknown. Searches comments + code. |

### Terminal & Execution
| Tool | Purpose | Key Rules |
|------|---------|-----------|
| `run_in_terminal` | Execute shell commands (sync/async) | Reuses zsh. `\|\|` for chaining. Pipelines > temp files. |
| `send_to_terminal` | Send input to terminal by ID | Interactive prompts. `waitForOutput: true` for REPLs/games. |
| `get_terminal_output` | Get output from async terminal | Call after async commands complete/time out. |
| `kill_terminal` | Terminate idle terminal by ID | Clean up servers/watchers when done. |
| `run_task` | Run VS Code tasks (`tasks.json`) | Prefer over `run_in_terminal` for build/run/test. |
| `create_and_run_task` | Create + run task (gen/updates `tasks.json`) | When no existing task matches. |

### Edit & Refactor
| Tool | Purpose | Key Rules |
|------|---------|-----------|
| `vscode_renameSymbol` | Rename symbol across workspace (LSP-aware) | Updates all refs. Needs exact symbol + file context. |
| `vscode_listCodeUsages` | Find all usages/references of symbol | Returns defs, impls, references. |

### Diagnostics & Errors
| Tool | Purpose | Key Rules |
|------|---------|-----------|
| `get_errors` | Get compile/lint errors (files or all) | Call after edits to validate. No filePath = all files. |
| `testFailure` | Get test failure details from last run | Before fixing tests to see exact failures. |

### Notebooks & Jupyter
| Tool | Purpose | Key Rules |
|------|---------|-----------|
| `run_notebook_cell` | Execute code cell by cellId (`TOP`/`BOTTOM`) | Code cells only (markdown unexecutable). Returns output. |
| `restart_notebook_kernel` | Restart kernel (after package installs) | When new packages require restart to be recognized. |
| `copilot_getNotebookSummary` | Get notebook cell list + exec info | cellIds, types, line ranges, outputs. Query after edits. |
| `configure_python_notebook` / `configure_non_python_notebook` | Select kernel and start it | After opening/creating notebooks. |

### Memory & Persistence
| Tool | Purpose | Key Rules |
|------|---------|-----------|
| `memory` (view/create/str_replace/insert/delete/rename) | Persistent notes across conversations | `/memories/` (user), `/memories/session/` (session), `/memories/repo/` (local). Short/bulleted. |

### GitHub & Git
| Tool | Purpose | Key Rules |
|------|---------|-----------|
| `github_repo` | Search repo source code semantically | Format: `<owner>/<repo>`. Specific repos only. |
| `github_text_search` | Keyword/regex search in GitHub repos | Language/path syntax. Org or single repo scope. |

### Browser & Automation (Playwright)
| Tool | Purpose | Key Rules |
|------|---------|-----------|
| `open_browser_page` | Open URL in integrated browser | Returns pageId + snapshot. Reuse pages when possible. |
| `read_page` | Get DOM/text snapshot of page | Better than screenshot for text extraction. Use `pageId`. |
| `click_element` / `type_in_page` / `hover_element` | Interact w/ web elements | Selectors or element refs. `pageId` required. |
| `navigate_page` | Navigate by URL/history/reload | Types: `url`, `back`, `forward`, `reload`. |
| `screenshot_page` | Capture visual screenshot | UI validation. Optional element ref/selector for cropping. |
| `drag_element` / `handle_dialog` / `run_playwright_code` | Advanced browser automation | Drag-and-drop, modal handling, custom Playwright JS execution. |

### VS Code API & Extensions
| Tool | Purpose | Key Rules |
|------|---------|-----------|
| `get_vscode_api` | VS Code extension API docs (stable + proposed) | Extension dev only — not general coding tasks. |
| `install_extension` | Install marketplace extensions by ID/name | Workspace setup. Format: `<publisher>.<extension>`. |
| `run_vscode_command` | Execute VS Code commands programmatically | Command IDs like `workbench.action.findInFiles`. |

### Other Utilities
| Tool | Purpose | Key Rules |
|------|---------|-----------|
| `list_dir` | List directory contents (files/folders) | Names w/ `/` suffix for folders. |
| `view_image` | View image files (png/jpg/jpeg/gif/webp) | Direct image viewing — not `read_file`. |

### Tool Selection Guidelines
- **File edits**: `replace_string_in_file` → fallback to `insert_edit_into_file`
- **Code search**: `grep_search` (exact text) or `semantic_search` (meaning/patterns)
- **Build/run**: `run_task` (if exists) → `create_and_run_task` → `run_in_terminal`
- **Debug errors**: `get_errors` after edits, `testFailure` for test issues
- **Refactoring**: `vscode_renameSymbol` (safe, LSP-aware), `vscode_listCodeUsages` (audit impact)
