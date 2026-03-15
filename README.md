# Stadium Gate Analytics API

Event-driven .NET 8 Web API for stadium gate people-flow analytics. Simulated gate sensor events are consumed asynchronously via an in-process channel, persisted to SQLite, and queryable through a REST endpoint.

## Quick Start

```bash
# Build
dotnet build

# Run (seeds DB with 5 minutes of data, starts simulator)
dotnet run --project src/StadiumAnalytics.Api

# Open Swagger UI
# https://localhost:5001/swagger

# Run tests (35 tests: unit + integration)
dotnet test
```

## Architecture

```
Sensor Simulation ──> Channel<GateSensorEvent> ──> Consumer ──> SQLite
                        (bounded, backpressure)     (per-event     │
                                                    error isolation) │
                                                                     │
                    Dashboard  <──  AnalyticsController  <───────────┘
                                    GET /api/v1/analytics/summary
```

**Write path** (decoupled): Simulator produces sensor events → bounded channel → background consumer persists to DB. Duplicates are caught by the composite unique key and logged to the `FailedEvents` table.

**Read path** (decoupled): REST endpoint queries the DB directly with GROUP BY + SUM. Never touches the channel or consumer.

## API

### `GET /api/v1/analytics/summary`

Returns sensor data aggregated by gate and type.

**Query parameters** (all optional):


| Parameter   | Type     | Description                         |
| ----------- | -------- | ----------------------------------- |
| `gate`      | string   | Filter by gate name (e.g. `Gate A`) |
| `type`      | string   | `enter` or `leave`                  |
| `startTime` | ISO 8601 | Inclusive start of time range       |
| `endTime`   | ISO 8601 | Inclusive end of time range         |


**Response** (200 OK):

```json
[
  { "gate": "Gate A", "type": "enter", "numberOfPeople": 142 },
  { "gate": "Gate A", "type": "leave", "numberOfPeople": 98 },
  { "gate": "Gate B", "type": "enter", "numberOfPeople": 210 }
]
```

**Error responses**: 400 Bad Request with RFC 7807 Problem Details for invalid filters.

### Health Checks

- `GET /health/live` — process is alive (always 200)
- `GET /health/ready` — DB is reachable (200 or 503)

## Project Structure

```
StadiumAnalytics.sln
src/
  StadiumAnalytics.Core/           Zero dependencies. Domain models, enums, DTOs, IGateEventChannel.
  StadiumAnalytics.Infrastructure/ EF Core, channel impl, consumer, simulator, seeder.
  StadiumAnalytics.Api/            Thin host: DI wiring, middleware, controller.
tests/
  StadiumAnalytics.Tests/          Unit tests + integration tests.
```

## Design Decisions

### Composite Key Deduplication

The natural key `(Gate, Timestamp, Type)` uniquely identifies a sensor reading. A sensor cannot produce two readings for the same gate, timestamp, and type. On unique constraint violation:

1. Log a warning with the payload
2. Write the event to the `FailedEvents` audit table with `Reason = "Duplicate"`
3. Continue processing

### FailedEvents Table (Deliberately Loose)

Stores raw sensor data including invalid values — nullable columns, no CHECK constraints, plus the full JSON payload. If a sensor sent `"Gate Z"` or `"type": "jump"`, we preserve it verbatim for debugging.

### Event Channel (`System.Threading.Channels`)

- Bounded (capacity 1000), `BoundedChannelFullMode.Wait` — correctness over throughput
- High-water mark warning at 80% capacity
- In production, swap to RabbitMQ/Azure Service Bus via `IGateEventChannel` abstraction

### Graceful Shutdown

On SIGTERM, the consumer drains remaining queued events with a 10-second timeout, logging how many were flushed. Prevents silent data loss.

### Seed Data

On every startup, 50 events are seeded for the past 5 whole minutes (5 min × 5 gates × 2 types). Idempotent via the composite unique key — restarting the app doesn't create duplicates.

## What I Would Add With More Time

- **Authentication**: JWT Bearer or API key middleware
- **Rate limiting**: .NET 8 `Microsoft.AspNetCore.RateLimiting`
- **Structured logging**: Serilog with JSON sink
- **Docker**: Multi-stage Dockerfile
- **Data retention**: Background job to purge events older than N days
- **FailedEvents endpoint**: Query endpoint for ops to review failures
- **External broker**: Replace in-process channel with RabbitMQ/Azure Service Bus for horizontal scaling
- **CORS**: Per-environment configuration for frontend dashboard

