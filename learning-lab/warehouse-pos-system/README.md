# Warehouse & POS System — Learning Lab

A microservices system modeling a small retail operation: barcode-based
warehouse management, point-of-sale, user management, an admin panel for
master data, and reporting — built step by step to learn every technology
involved, in depth, before moving to the next piece.

This replaces the earlier `food-ordering-microservices` lab (still in this
repo, untouched) once the domain changed from "practice e-commerce" to a
real business system shape.

Target stack: **.NET 8 (LTS)** backend, **Angular** frontend, **SQL Server**
as the primary datastore, **RabbitMQ/MassTransit** for async communication,
**Ocelot** as the API gateway.

## Why microservices here (and the tradeoff that comes with it)

Warehouse, POS, Users, and Reporting are tightly related — a POS sale has to
reduce warehouse stock, ideally atomically. In a single database that's one
transaction. Split across services, it isn't: POS and Warehouse are
different processes, different databases, no shared transaction. That's a
deliberate choice for this lab (continuing the microservices patterns from
the earlier project) — Step **C3** is where this tradeoff becomes concrete,
using an event + compensating-action approach (a simplified saga) instead of
a distributed transaction.

## Roadmap

**Phase A — Foundation**
- [x] **A1 — Identity service**: Users/Roles, JWT issuing (User Management Module)
- [ ] A2 — Shared exception handling + ProblemDetails
- [ ] A3 — Ocelot API Gateway + JWT validation at the gateway
- [ ] A4 — Angular SPA skeleton (login, auth, toaster wiring)

**Phase B — Warehouse (barcodes)**
- [ ] B1 — Domain + Infrastructure (Items, barcodes, stock, locations)
- [ ] B2 — Application layer (CQRS/MediatR/FluentValidation)
- [ ] B3 — API + Angular Admin Panel screen (master data)

**Phase C — POS using warehouse barcodes**
- [ ] C1 — Domain/Application/Infrastructure (Sale, checkout)
- [ ] C2 — Sync call to Warehouse (barcode + stock check)
- [ ] C3 — Async `SaleCompleted` event + saga (stock decrement, compensation)
- [ ] C4 — Angular POS screen

**Phase D — Reporting**
- [ ] D1 — Event-driven read models
- [ ] D2 — Reports + Angular dashboards

**Phase E — Notifications / Mailing**
- [ ] E1 — In-app notifications (SignalR)
- [ ] E2 — Mailing system

**Phase F — Hardening**
- [ ] F1 — Performance (caching, pagination, health checks)
- [ ] F2 — Security hardening (role policies, rate limiting)
- [ ] F3 — Localization (English/Arabic, RTL)
- [ ] F4 — Full docker-compose stack + end-to-end walkthrough

## A1 — Identity service

**What it does:** issues JWTs after registering/logging in a user, backed by
a `Users`/`Roles` table in SQL Server. Every other service in this system
will trust the token this service signs — nobody else re-implements
authentication.

**Layout** (Clean Architecture, same shape the earlier lab used for Ordering):
```
Identity.Domain          — User, Role entities. No dependencies on anything else.
Identity.Application     — CQRS commands (Register, Login), validation, the
                            *interfaces* for password hashing/JWT generation.
Identity.Infrastructure   — EF Core + SQL Server, the interfaces' real
                            implementations, JWT signing.
Identity.API              — Controllers, JWT Bearer middleware wiring, Program.cs.
```

**Concepts introduced:**
- **Dependency Inversion across project boundaries.** `Identity.Application`
  defines `IPasswordHasher` and `IJwtTokenGenerator` — interfaces it needs
  but doesn't implement. `Identity.Infrastructure` implements them using
  ASP.NET Core's `PasswordHasher<T>` (PBKDF2) and `System.IdentityModel.Tokens.Jwt`.
  The Application layer's `.csproj` never even references those libraries —
  it only references `Identity.Domain`. This is what makes command handlers
  unit-testable without a real database or real crypto.
- **MediatR pipeline behaviours** (`ValidationBehaviour`,
  `UnhandledExceptionBehaviour`) — every command runs through FluentValidation
  automatically before its handler executes, and every unhandled exception
  gets logged with full request context before rethrowing. Neither behaviour
  is called explicitly anywhere; MediatR wraps every `Send()` call with them.
- **JWT claims design.** The token carries `ClaimTypes.Name`,
  `ClaimTypes.NameIdentifier`, and `ClaimTypes.Role`. Every downstream
  service (Warehouse, POS, Reporting, ...) will validate the same signed
  token and read the role claim directly — no service ever calls back into
  Identity.API to ask "is this user an Admin?". That's the point of a JWT:
  the token itself is the trusted answer.
- **Uniform authentication failure messages.** Both "unknown username" and
  "wrong password" throw the same `AuthenticationException("Invalid
  username or password.")`. Distinguishing them in the response would let
  an attacker enumerate valid usernames — a real security property, not
  paranoia.
- **EF Core migration-seeded reference data vs. runtime-seeded data.** Roles
  are seeded via `HasData` in a migration (fixed reference data that should
  exist the instant the schema does). The admin *user* is seeded at startup
  instead, because creating it requires hashing a password — a runtime
  operation that can't be baked into a static migration.

**A dependency decision worth calling out:** AutoMapper was deliberately
*not* used here, even though the earlier lab used it for Ordering. Current
AutoMapper versions require a commercial license for non-trivial use, and
versions below 15.1.1 carry a real Denial-of-Service advisory
([GHSA-rvv3-g6hj-g44x](https://github.com/advisories/GHSA-rvv3-g6hj-g44x)).
Since none of the mapping here is complex, the handlers just construct
objects directly — one less dependency, no licensing question, nothing to
patch later. (MediatR 12.4.1, by contrast, is still confirmed Apache-2.0 —
its commercial licensing change came in a later major version we didn't pull in.)

**Try it (requires SQL Server):**
```bash
# SQL Server via Docker
docker run -d -p 1433:1433 -e ACCEPT_EULA=Y -e SA_PASSWORD=SwN12345678 \
  --name identitydb mcr.microsoft.com/mssql/server:2022-latest

cd src/Services/Identity/Identity.API
dotnet run
# Swagger UI: http://localhost:5xxx/swagger — use "Authorize" with a token from /login
```

`POST /api/v1/Auth/register` (creates a Cashier by default), `POST
/api/v1/Auth/login`, then `GET /api/v1/Auth/me` with `Authorization: Bearer
<token>` to see the claims come through. A seeded admin account
(`admin` / `Admin@12345`) is created automatically on first run — **change
or remove this before any real deployment**; it exists purely so there's a
way to log in as an Admin on day one.

> **Note on this sandbox:** same limitation as the earlier lab — no Docker
> daemon and no outbound access to a package mirror for SQL Server here.
> The full solution builds with 0 errors/warnings, and the EF Core model was
> verified for real: the entire register → login → wrong-password-rejected
> → validation-rejected → admin-login flow was run and passed against a
> real relational database engine (SQLite, standing in for SQL Server only
> for this verification — the actual code always targets SQL Server via
> `UseSqlServer`). Run it locally with SQL Server via Docker to see it for real.

> **Also note:** `appsettings.json`'s `JwtSettings:Secret` and the SQL
> Server password are plaintext placeholders for local development, exactly
> like the original reference project's `docker-compose.override.yml` does
> for its own SA password. Real deployments should pull these from an
> environment variable or a secret manager, never from a committed file —
> this is one of the things Phase F2 (security hardening) will come back to
> make explicit across every service.
