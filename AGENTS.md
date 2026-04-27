# Development Context

## Tech Stack
- .NET 8
- Windows (Visual Studio 2022 recommended)
- Docker & Docker Compose for local environment
- MariaDB (via Docker)
- Grafana (via Docker)

## Project Structure
- `TeslaLogger/`: Main application (`TeslaLoggerNET8.csproj`)
- `Logfile/`: Logging utility (`LogfileNET8.csproj`)
- `srtm/`: SRTM logic (`SRTMNET8.csproj`)
- `OSMMapGenerator/`: Map generation (`OSMMapGeneratorNET8.csproj`)
- `KafkaConnector/`: Kafka integration
- `UnitTestsTeslalogger/`: Test project

## Development Workflow

### Local Environment
- Run the infrastructure via Docker: `docker compose up -d`.
- Use `host.docker.internal` on Windows when a container needs to reach the host.

### Testing
- **Unit Tests**: Managed via MSTest/NUnit in the `UnitTestsTeslalogger` project. Run via Visual Studio Test Explorer or `dotnet test`.
- **UI Tests**: Uses Selenium with ChromeDriver. Requires a Chrome browser installed.
- *Caution*: Tests may modify local data; use backups if working with important datasets.

## Key Architecture Notes
- This is a multi-project solution (`TeslaLoggerNET8.sln`).
- The application relies on external services (MariaDB, Grafana) which are typically orchestrated via Docker.
