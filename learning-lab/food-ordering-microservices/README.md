# Food Ordering Microservices — Learning Lab

A small, from-scratch microservices project used to learn — one concept at a
time — every technology used in the reference solution
(`run-aspnetcore-microservices`), applied to a simpler domain: a food
ordering app instead of a general e-commerce catalog.

Each service below is a deliberately simplified stand-in for a service in the
reference project, built in the same order the concepts were introduced there.

| This project | Reference equivalent | Tech being practiced |
|---|---|---|
| `MenuItems.API` | `Catalog.API` | ASP.NET Core Web API, MongoDB, Repository pattern |
| `Cart.API` | `Basket.API` | Redis as a primary store, gRPC client, MassTransit publish |
| `Promotions.Grpc` | `Discount.Grpc` | gRPC server, Protobuf, Dapper, PostgreSQL |
| `Orders.*` | `Ordering.*` | DDD, CQRS (MediatR), FluentValidation, AutoMapper, EF Core, MassTransit consumer |
| `Gateway.Ocelot` | `OcelotApiGw` | API Gateway routing, rate limiting, caching |
| `Ordering.Aggregator` | `Shopping.Aggregator` | BFF / aggregation pattern |
| `FoodOrderingWeb` | `AspnetRunBasics` | Razor Pages, HttpClientFactory, Polly resilience |
| `WebStatus` | `WebStatus` | HealthChecks UI dashboard |
| `BuildingBlocks.*` | `EventBus.Messages`, `Common.Logging` | Shared event contracts, centralized Serilog logging |

Target framework: **.NET 8 (LTS)**. The reference project uses .NET 5, so a
few things are intentionally modernized (e.g. the minimal-hosting
`Program.cs` instead of a separate `Startup.cs` — .NET 6 merged the two).

## Progress

- [x] **Step 1 — MenuItems.API**: MongoDB-backed catalog service.
- [ ] Step 2 — Dockerize MenuItems.API + docker-compose basics
- [ ] Step 3 — Cart.API (Redis)
- [ ] Step 4 — Promotions.Grpc (Postgres + Dapper + gRPC)
- [ ] Step 5 — Cart.API → Promotions.Grpc (sync gRPC call)
- [ ] Step 6 — RabbitMQ + MassTransit publish (Cart → event)
- [ ] Step 7 — Orders (DDD/CQRS/MediatR/EF Core, MassTransit consumer)
- [ ] Step 8 — Ocelot API Gateway
- [ ] Step 9 — Aggregator BFF
- [ ] Step 10 — Razor Pages WebUI + Polly
- [ ] Step 11 — Centralized logging (Serilog+ELK) + Health Checks
- [ ] Step 12 — Full docker-compose stack + end-to-end smoke test

## Step 1 — MenuItems.API

**What it does:** a REST API for browsing/managing food menu items, backed
by MongoDB. This is the simplest service in the reference project, so it's
the right first step: no CQRS, no message bus, no inter-service calls — just
ASP.NET Core talking to a database through a repository.

**Concepts introduced:**
- **ASP.NET Core Web API** — controllers, model binding, `[ApiController]`,
  `ProducesResponseType` for documenting response shapes.
- **MongoDB.Driver** — a `MongoClient`/`IMongoDatabase`/`IMongoCollection<T>`
  wraps a schemaless document store. `[BsonId]` + `[BsonRepresentation]` map
  the Mongo `ObjectId` to a plain `string` so it doesn't leak Mongo-specific
  types into the API contract.
- **Repository pattern** — `IMenuItemsRepository` sits between the
  controller and MongoDB. The controller never sees `IMongoCollection<T>`;
  it only sees domain operations (`GetMenuItems`, `CreateMenuItem`, ...).
  This is what makes the data layer swappable and unit-testable.
- **Dependency Injection lifetimes** — `IMenuItemsContext` (wraps the Mongo
  client) is registered `Singleton` because the underlying `MongoClient` is
  thread-safe and expensive to create; `IMenuItemsRepository` is `Scoped`
  because it's cheap and stateless per request.
- **Startup-time seeding** — `MenuItemsContextSeed` inserts sample data if
  the collection is empty, so the API is immediately useful after first run.

**Run it locally (requires a MongoDB instance):**
```bash
# Option A: MongoDB via Docker
docker run -d -p 27017:27017 --name menuitemsdb mongo

# Option B: point DatabaseSettings:ConnectionString in appsettings.json
# at any MongoDB instance you already have running.

cd src/Services/MenuItems/MenuItems.API
dotnet run
# Swagger UI: http://localhost:5281/swagger
```

Try it: `GET /api/v1/MenuItems` should return the 4 seeded items. `POST` a
new item, then `GET /api/v1/MenuItems/{id}` to fetch it back.

> **Note on this sandbox:** the code above was built and compiles cleanly
> (`dotnet build` — 0 errors), but this development sandbox has no Docker
> daemon and no outbound access to download a MongoDB binary directly, so
> it could not be run end-to-end here. Run it locally with Docker Desktop
> (or any MongoDB instance) to see it working — see Step 2 for the Docker
> Compose setup that removes the need to run Mongo manually.
