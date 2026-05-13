# Stadium Analytics

A production-quality event-driven stadium sensor analytics API built with .NET 8, demonstrating Clean Architecture-lite, RabbitMQ messaging, EF Core + PostgreSQL persistence, and a controller-based REST API. The sensor simulator generates random gate movement events, which are published to RabbitMQ, drained by a separately deployable consumer and persisted to PostgreSQL, then made queryable via the API.

## Architecture

```
┌─────────────────┐    ┌──────────┐    ┌──────────────────┐    ┌─────────────────┐
│   Simulator     │───▶│ RabbitMQ │───▶│    Consumer      │───▶│   PostgreSQL    │
│  (Worker)       │    │          │    │   (Worker)       │    │  (EF Core)      │
└─────────────────┘    └──────────┘    └──────────────────┘    └────────┬────────┘
                                                                        │
                                                              ┌─────────▼────────┐
                                                              │  API             │
                                                              │ (Controllers)    │
                                                              └──────────────────┘
```

Each box is its own deployable. The API only reads from PostgreSQL; the Consumer only writes. Both speak to the same DB so the API can serve historical queries while the Consumer keeps draining the queue. PostgreSQL runs as its own container with a named volume so data survives container restarts and rebuilds. RabbitMQ decouples the writer from the producer so additional consumers can be added side-by-side for horizontal scaling.

## Projects

The solution is split into **libraries** (pure logic, no `Main`) and **services** (runnable hosts), so it's immediately obvious what is deployable vs reusable.

```
src/
├── libraries/
│   ├── StadiumAnalytics.Domain          ← entities, enums (zero deps)
│   ├── StadiumAnalytics.Application     ← abstractions, query services, validators
│   └── StadiumAnalytics.Infrastructure  ← EF Core, RabbitMQ adapters
└── services/
    ├── StadiumAnalytics.Api             ← controller-based read API
    ├── StadiumAnalytics.Consumer        ← worker: queue → DB
    └── StadiumAnalytics.Simulator       ← worker: event producer (dev/demo only)
tests/
├── StadiumAnalytics.Domain.UnitTests
├── StadiumAnalytics.Application.UnitTests
├── StadiumAnalytics.Infrastructure.UnitTests
├── StadiumAnalytics.Consumer.UnitTests
├── StadiumAnalytics.Simulator.UnitTests
└── StadiumAnalytics.IntegrationTests    ← in-process HTTP tests via WebApplicationFactory
```

| Project | Responsibility |
|---|---|
| `StadiumAnalytics.Domain` | Core entities (`SensorEvent`) and enums (`MovementType`). Zero dependencies. |
| `StadiumAnalytics.Application` | Abstractions (`IEventBus`, `IAppDbContext`), query service, DTOs, FluentValidation. |
| `StadiumAnalytics.Infrastructure` | EF Core + PostgreSQL (`AppDbContext`, migrations), RabbitMQ adapters. Pure adapter library — no hosted services. |
| `StadiumAnalytics.Simulator` | **Standalone Worker** publisher. Generates random sensor events. Depends on messaging only. |
| `StadiumAnalytics.Consumer` | **Standalone Worker** subscriber. Drains the queue and persists events. Split out so additional consumer workers can scale independently. |
| `StadiumAnalytics.Api` | Controller-based read API. No hosted services; talks only to persistence. |

Each library owns its own composition extension method, named with the standard `XxxServiceCollectionExtensions` convention: `AddApplication`, `AddPersistence`, `AddMessaging`, `AddSensorEventConsumer`, `AddSimulator`.

## Composition

Infrastructure exposes two focused registration helpers:

- `AddPersistence(IConfiguration)` — registers `AppDbContext` + `IAppDbContext`.
- `AddMessaging(IConfiguration)` — registers `RabbitMqConnection`, `RabbitMqOptions`, and the publisher-side `IEventBus`.

Each host wires only what it needs:

| Host | `AddPersistence` | `AddMessaging` | Hosted service |
|---|:-:|:-:|---|
| API | ✓ | ✓ | — |
| Consumer | ✓ | ✓ | `SensorEventConsumer` (via `AddSensorEventConsumer`) |
| Simulator | — | ✓ | `SensorSimulatorService` (via `AddSimulator`) |

## Run with Docker (one command)

Prerequisites: **Docker** with Docker Compose v2 (any recent Docker Desktop, or `docker-compose-plugin` on Linux). Nothing else — the .NET SDK is not required, the images build inside Docker.

```bash
docker compose up --build -d
```

That's it. Compose will:

1. Build the API, Consumer, and Simulator images from source.
2. Start `postgres` (with a named volume `stadium-pgdata`) and `rabbitmq`, and wait for both to report healthy.
3. Start the `api`, `consumer`, and `simulator`. The API and Consumer each acquire a Postgres advisory lock on boot and apply EF migrations safely, even if both start at the same time.
4. The simulator immediately begins publishing events; the consumer drains them into Postgres; the API exposes them.

Open:

- Swagger UI: [http://localhost:8080/swagger](http://localhost:8080/swagger)
- RabbitMQ Management: [http://localhost:15672](http://localhost:15672) (guest / guest)

> All app services use `restart: unless-stopped`, so any transient first-boot connection race (e.g. RabbitMQ accepting AMQP a moment after its healthcheck passes) self-heals without intervention.

Postgres data is persisted in the `stadium-pgdata` named volume, so `docker compose down` keeps your data; only `docker compose down -v` wipes it.

### Try the API

```bash
# All groups (no filter — empty body)
curl -X POST http://localhost:8080/api/sensor-results/query \
     -H "Content-Type: application/json" -d '{}'

# Filter by gate
curl -X POST http://localhost:8080/api/sensor-results/query \
     -H "Content-Type: application/json" -d '{ "gate": "Gate A" }'

# Filter by movement type
curl -X POST http://localhost:8080/api/sensor-results/query \
     -H "Content-Type: application/json" -d '{ "type": "Enter" }'

# Date range (UTC or any ISO-8601 offset)
curl -X POST http://localhost:8080/api/sensor-results/query \
     -H "Content-Type: application/json" \
     -d '{ "startTimeUtc": "2025-01-01T00:00:00Z", "endTimeUtc": "2025-12-31T23:59:59Z" }'
```

The endpoint is `POST /api/sensor-results/query` taking an optional JSON body with `{ gate?, type?, startTimeUtc?, endTimeUtc? }`. Modeled as POST so dashboards / Swagger UI can send the JSON filter body — the browser Fetch API forbids bodies on GET. The action is read-only and idempotent regardless of HTTP method.

### Useful operational commands

```bash
docker compose ps                    # see what's running
docker compose logs -f api           # tail API logs
docker compose logs -f consumer      # tail Consumer logs
docker compose restart consumer      # restart a single service
docker compose down                  # stop everything (Postgres data preserved)
docker compose down -v               # stop everything and wipe Postgres data
```

## Run Locally

Prerequisites: .NET 8 SDK, RabbitMQ running on localhost:5672.

```bash
# Three terminals:
dotnet run --project src/services/StadiumAnalytics.Api
dotnet run --project src/services/StadiumAnalytics.Consumer
dotnet run --project src/services/StadiumAnalytics.Simulator
```

## Run Tests

```bash
dotnet test
```

## Design Choices

- **One process per concern** — the API (reader), the Consumer (queue→DB writer), and the Simulator (event producer) are all separate deployable workers. Each can be scaled, restarted, or replaced independently. Adding a second event type later just means adding another consumer.
- **RabbitMQ durable + persistent + publisher confirms + manual ACK** — at-least-once delivery. Messages survive broker restarts; confirms prevent silent publish failures; manual ACK ensures an event is only acknowledged after it has been successfully persisted.
- **Raw events stored; aggregation on read** — every individual sensor reading is kept. Aggregation (GROUP BY gate + type, SUM people) is done at query time via EF Core / SQL. Simple write path, flexible queries.
- **Composite index `IX_SensorEvents_Gate_Type_TimestampUtc`** — covers the most common filter and sort patterns used by the query service.
- **Clean Architecture-lite** — Domain has zero deps; Application defines abstractions; Infrastructure implements them; each host (Api/Consumer/Simulator) is its own composition root.
- **`IEventBus` abstraction** — the broker is swappable. Tests use a `NullEventBus`.

## Deliberate Trade-offs

- **PostgreSQL in its own container with a named volume (`stadium-pgdata`)** — the API and Consumer share one database; data survives `docker compose down` and is only wiped by `docker compose down -v`.
- **No idempotency / inbox table** — with at-least-once delivery a transient crash between persist and ACK can produce a duplicate row. An inbox pattern would prevent this.
- **No authentication** — endpoints are intentionally public for this exercise. Add a real identity provider (Entra, Auth0, IdentityServer) plus rate limiting before exposing.

## What I'd Add with More Time

- **Inbox / outbox pattern** for exactly-once processing
- **Projection consumer** that maintains a pre-aggregated read model (full CQRS)
- **Cursor-based pagination** on `/api/sensor-results`
- **OpenTelemetry** traces and metrics (spans per message, DB queries, HTTP requests)
- **Rate limiting + authentication**
