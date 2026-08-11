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
- [x] **A2 — Shared exception handling + ProblemDetails**
- [x] **A3 — Ocelot API Gateway + JWT validation at the gateway**
- [x] **A4 — Angular SPA skeleton (login, auth, toaster wiring)**

**Phase B — Warehouse (barcodes)**
- [x] **B1 — Domain + Infrastructure (Items, barcodes, stock, locations)**
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

## A2 — Shared exception handling + ProblemDetails

**What it does:** two small BuildingBlocks packages that every future
microservice's Web API project (Warehouse.API, Pos.API, ...) will reference,
so no controller anywhere in this system ever needs its own try/catch to
return a clean error response.

**Two packages, not one — and the split matters:**
```
Common.Exceptions        — pure C#. NotFoundException, ValidationException,
                            ConflictException, UnauthorizedException, and the
                            IHasStatusCode interface they all implement.
                            Zero ASP.NET Core dependency.
Common.ExceptionHandling — GlobalExceptionHandler (ASP.NET Core 8's
                            IExceptionHandler) + the DI wiring. This is the
                            only place that touches ProblemDetails/HttpContext.
```
The first draft put everything in one package — until it became clear that
an Application-layer project (which should never reference a web framework)
would end up with ASP.NET Core in its dependency graph just by referencing
shared *exception types*. Splitting them means `Identity.Application`
references only `Common.Exceptions` (plain classes, no framework), while
only `Identity.API` (a Web project already) references
`Common.ExceptionHandling`. This is the same Dependency Inversion idea from
A1 applied to a library boundary instead of an interface.

**Concepts introduced:**
- **`IExceptionHandler`** — ASP.NET Core 8's purpose-built interface for
  this (older code bases hand-roll a middleware class instead; this is the
  now-idiomatic way). Registered via `AddExceptionHandler<T>()`, dispatched
  by `app.UseExceptionHandler()`, which must sit first in the pipeline —
  it can only catch exceptions thrown by middleware registered *after* it.
- **The Open/Closed Principle, concretely.** `GlobalExceptionHandler` never
  references `NotFoundException` or `ConflictException` by name — it checks
  `is IHasStatusCode` and reads `.StatusCode`. When Warehouse later needs its
  own `InsufficientStockException`, it implements `IHasStatusCode` and gets
  full ProblemDetails handling for free, without this shared handler ever
  being modified.
- **Never leak an unexpected exception's message.** Anything *not*
  implementing `IHasStatusCode` is treated as a genuine bug (500) — logged
  server-side with the full exception via `ILogger`, but the client only
  ever sees `"An unexpected error occurred."` A raw exception message can
  contain connection strings, file paths, or other internals that have no
  business leaving the server; a 400/401/404's message is safe to return
  because those exceptions were written with a client-safe message in the
  first place.
- **`IProblemDetailsService`**, not manual JSON serialization — the built-in
  service that both `GlobalExceptionHandler` and ASP.NET Core's own
  automatic model-validation 400s go through, via `AddProblemDetails()`.
  One consistent error shape for both "our code threw" and "framework
  rejected the request before it even got to a controller."

**Verified with a focused test:** a throwaway minimal-API host (deleted
after, not part of this repo) mapped one endpoint per exception type and
hit each over real HTTP via `TestServer`. All 6 checks passed: 404/409/401
map correctly, the validation case includes a field-by-field `errors`
object, an unexpected `InvalidOperationException` returns 500 with its
message correctly *not* present in the response body, and a healthy
endpoint is unaffected by any of it.

## A3 — Ocelot API Gateway + JWT validation at the gateway

**What it does:** `Gateway.Ocelot` is the single entry point into the
system. Right now it only proxies to Identity.API (`/Identity/Auth/*`), but
every future service (Warehouse, POS, Reporting, ...) will add routes here
rather than being called directly — clients only ever need to know one
address.

**A third shared package, for the same reason as A2:** `Common.Security`
holds `JwtSettings` and one `AddJwtAuthentication()` extension method.
Every service that validates a JWT — the gateway now, every downstream
service later, as defense in depth — needs byte-identical
`TokenValidationParameters`. Writing that block out by hand in each
`Program.cs` is exactly the kind of duplication that drifts silently (one
service tweaks `ClockSkew`, the others don't, and now token expiry behaves
inconsistently depending which service you hit). `Identity.API` was
refactored to use this shared extension instead of its own inline copy.

**Concepts introduced:**
- **JWT validation moves to the edge.** `ocelot.json`'s `/Identity/Auth/me`
  route carries `"AuthenticationOptions": { "AuthenticationProviderKey": "Bearer" }`.
  A request with no token, an expired token, or a token signed with the
  wrong secret is rejected *by the gateway* — it never reaches Identity.API
  at all. `/register` and `/login` deliberately have no `AuthenticationOptions`:
  you can't present a token for an endpoint whose entire job is to give you one.
- **The JWT secret is now a genuinely shared secret.** Gateway.Ocelot's
  `appsettings.json` has to carry the exact same `JwtSettings:Secret`/`Issuer`/`Audience`
  as Identity.API's. There's no code sharing that enforces this — it's a
  deployment/config discipline, and duplicating it by hand in two
  `appsettings.json` files (as done here, for now) is a real gap Phase F2
  will close with one shared secret source instead of copy-pasted values.
- **The Authorization header passes through untouched.** Ocelot forwards
  every request header to the downstream service by default — verified
  below by having the (stubbed) downstream echo the header back. That's
  what lets Identity.API's own `[Authorize]` on `/me` still work: the
  gateway validating the token doesn't replace the service validating it
  again if it chooses to (defense in depth, not "trust the gateway blindly").

**Two real bugs found while verifying this — both worth knowing about, not just fixing quietly:**

1. **A health check that silently returned 404.** `app.MapHealthChecks("/hc")`
   was mapped before `await app.UseOcelot()`, which looked right — but
   top-level `Map*()` calls in minimal hosting are deferred to run
   *implicitly at the very end* of the middleware pipeline, after
   `UseOcelot()`'s own catch-all middleware has already handled (or
   404'd) the request. Ocelot's middleware is terminal — it never calls
   `next()` for a path it doesn't recognize. The fix is calling
   `app.UseRouting()` / `app.UseEndpoints(...)` *explicitly*, which
   dispatches endpoint matches at that exact point in the pipeline instead
   of at the implicit end.
2. **Ocelot's own `RateLimitOptions` doesn't do what you'd assume.** The
   original plan put `RateLimitOptions` directly on the `/login` route in
   `ocelot.json` to slow down credential stuffing. Testing it immediately
   returned `503` on every request — Ocelot's rate limiter identifies a
   "client" via a self-declared request header, and with no client sending
   that header, it couldn't identify *anyone*, including a legitimate
   caller. Worse: even if configured correctly, this mechanism is the wrong
   tool for the job — an actual attacker would simply omit the header to
   get an unlimited budget. It's built for per-tenant/API-key throttling,
   not abuse protection. The real fix uses ASP.NET Core's own
   `RateLimiter` middleware with a global limiter partitioned by
   `HttpContext.Connection.RemoteIpAddress` — an attacker can't opt out of
   having an IP address the way they can opt out of sending a header.

**Verified against a real gateway process, not just compiled:** with no
Docker/SQL Server available in this sandbox, Identity.API itself couldn't
run, so a stand-in stub server (mimicking its three endpoints, deleted
after) stood on its port instead — isolating the test to exactly what's new
in A3 (Ocelot's routing + auth + rate limiting), the same principle as A2's
focused test. The real `Gateway.Ocelot.dll` was run as an actual process
and hit with real HTTP requests:
- `GET /hc` → `200 Healthy`
- `POST /Identity/Auth/register` (no token) → `200`, proxied to the stub
- `GET /Identity/Auth/me` with no token → `401`, **from the gateway** (confirmed the stub never saw the request)
- `GET /Identity/Auth/me` with a JWT signed using the correct shared secret → `200`, proxied through, `Authorization` header intact
- `GET /Identity/Auth/me` with a JWT signed using the *wrong* secret → `401`
- 6 rapid `POST /Identity/Auth/login` calls → the first 5 succeeded, the 6th returned `429`

## A4 — Angular SPA skeleton

**What it does:** `client/` is a standalone Angular 20 app (kept as a sibling
of `src/`, since it's a completely different toolchain — npm/Node instead
of dotnet/NuGet) with a login page, a placeholder admin area behind a route
guard, and the two pieces of plumbing (an auth interceptor and an error
interceptor) that every future screen in this system — the Admin Panel
(B3), the POS screen (C4), Reporting dashboards (D2) — builds on top of
without re-solving.

**A dependency decision that mattered more than expected:** the scaffold
started on Angular 19 (the newest version this sandbox's Node could run).
Checking `npm audit` on the *fresh, untouched* scaffold turned up two real
high-severity XSS advisories in `@angular/core` and `@angular/compiler`
that Angular's 19.x line never received a backport for — only 20.3.27+ and
21.2.19+ are patched. Angular 20 turned out to need a Node version this
sandbox's Node (22.22.2) does satisfy, so the whole app was rescaffolded on
Angular 20 before writing any custom code on top of it — the same
"check before you build on it" instinct as the AutoMapper/MediatR calls
earlier in this project, just on the frontend side this time.

**How the pieces fit together — tracing a login:**
```
LoginComponent.submit()
  → AuthService.login(credentials)          — POST /Identity/Auth/login
    → authInterceptor                        — (no token yet, passes through unchanged)
    → [ request leaves the browser, hits Gateway.Ocelot, A3 ]
    ← response comes back
    → errorInterceptor                       — only acts on non-2xx; success flows through untouched
  → AuthService stores the AuthResponse in localStorage, sets the `currentUser` signal
  → LoginComponent navigates to /admin
    → authGuard checks AuthService.isAuthenticated() — true now — lets it through
```
And for every subsequent request to anything protected:
```
any HttpClient call
  → authInterceptor attaches `Authorization: Bearer <token>` from AuthService
  → [ request hits the gateway, which validates the JWT itself (A3) ]
  ← if the gateway (or the downstream service) rejects it with 401
  → errorInterceptor: AuthService.logout() + toast + redirect to /login
```

**Concepts introduced:**
- **Functional interceptors and guards** (`HttpInterceptorFn`, `CanActivateFn`)
  — the modern Angular 15+ style: plain functions registered in
  `app.config.ts`/`app.routes.ts`, not classes implementing an interface
  registered in an `NgModule`. No NgModule exists anywhere in this app —
  every component is standalone, which has been Angular's default shape
  since well before this app's version.
- **Signals for simple synchronous state.** `AuthService.currentUser` is a
  `signal<CurrentUser | null>`, not a `BehaviorSubject`. Nothing here needs
  RxJS's operators (the actual HTTP calls still return Observables, because
  `HttpClient` does) — a signal is simpler for "the current value, and
  re-render when it changes," which is all this state needs to be.
- **The frontend mirror of A2.** `errorInterceptor` reads the exact
  `ProblemDetails` shape `GlobalExceptionHandler` produces — `detail` for a
  single message, `errors` for the field-level validation dictionary — and
  turns it into one toast. The two pieces (A2's backend shape, A4's
  frontend reader) were designed together on purpose: change one without
  the other and error messages silently stop working.
- **A guard is UX, not security.** `authGuard` keeps a signed-out user from
  seeing a half-loaded admin screen full of failed API calls — it is not
  what stops that user from calling the API directly. The actual security
  boundary is the gateway's JWT validation (A3) and each service's own
  `[Authorize]`. Said directly in the guard's own comment so it's not
  mistaken for more than it is.
- **A known, named tradeoff: localStorage vs. an httpOnly cookie.** The
  token lives in localStorage — simple, framework-agnostic, and readable by
  any script running on the page, meaning a successful XSS attack anywhere
  in this app could steal it. An httpOnly cookie closes that hole but needs
  CSRF protection in exchange. This is Phase F2's problem to solve
  properly, not something to quietly get wrong here and forget about.

**Verified in a real browser, not just built:** Karma's default headless
Chrome launcher needs `--no-sandbox` to run as root, and wiring that up
without losing Angular's own auto-generated test configuration turned out
to be more trouble than it was worth for what it would have checked. A
Playwright-driven, real-browser end-to-end run against the actual built app
(served statically) and a stub gateway (mimicking the login endpoint's
success/failure JSON, deleted after) was more valuable anyway — it
exercises the whole chain at once instead of one unit in isolation. All 10
checks passed: the guard redirects signed-out visitors away from `/admin`;
wrong credentials produce a toast containing the backend's exact
`"Invalid username or password."` message; correct credentials navigate to
`/admin` and show the signed-in username and role; the session survives a
page reload; the token is actually in `localStorage`; and signing out both
clears storage and makes the guard block `/admin` again.

**Run it locally (requires the gateway + Identity.API running, per A1/A3):**
```bash
cd client
npm install
npm start   # ng serve — http://localhost:4200
```

## B1 — Warehouse domain + infrastructure

**What it does:** the data model behind the Warehouse Management Module —
`Item`, `Category`, `Location`, `UnitOfMeasure`, `StockLevel` (how many of
an item are at a location, right now), and `StockTransaction` (an
append-only ledger of every change to that number). No business operations
yet (no "adjust stock," no "create item" command) — that's B2. This step
is deliberately just the shape of the data and how it's stored.

**Revised after a real design question.** The first draft gave `Item` a
single `Barcode` string. That breaks the moment an item legitimately has
more than one valid barcode (a manufacturer's own vs. a relabeled supplier
variant, say) while still needing to share one stock count — a case the
single-barcode design couldn't represent at all without creating a second,
disconnected `Item` row. The fix, worked out from that question:

```
Item             — the product definition. Its identity is Sku, not a
                   barcode. Picks one UnitOfMeasure as its BaseUnitOfMeasure.
ItemBarcode      — 1-to-many. Every barcode on this table resolves to the
                   same Item (and therefore the same shared StockLevel).
                   At most one row per item may be IsPrimary = true.
UnitOfMeasure    — master data: PCS, KG, BOX, CARTON, LITER, ...
ItemUnit         — an item's ALTERNATE units, each with a ConversionFactor
                   into its base unit (e.g. "1 BOX of Cola = 12 PCS").
                   The base unit itself has no row here — it's implicitly
                   a factor of 1.
```
The rule that makes this hang together: **every inventory quantity —
`StockLevel.QuantityOnHand`, `StockTransaction.QuantityChange` — is always
expressed in the item's base unit.** A "receive 2 BOX" operation converts
through `ItemUnit.ConversionFactor` before it ever touches those two
tables; inventory never has to ask "in what unit, though?" A `Sku` is the
item's own internal identifier, kept distinct from `Barcode` (an external,
scannable identifier) on purpose — conflating the two is exactly what made
the first draft brittle.

**Same three-project layering as Identity, spread differently this time:**
`Warehouse.Domain` (entities) and `Warehouse.Infrastructure` (EF Core, the
real work of this step) exist in full. `Warehouse.Application` exists too,
but only with `Contracts/Persistence` interfaces — no MediatR, no
commands, nothing that needs FluentValidation yet. Identity built all four
layers (including the API) in one step; Warehouse has more moving parts,
so it's spread across B1/B2/B3 instead — persistence and business logic
and the outward-facing API each get their own step to actually explain,
rather than landing all at once.

**The balance-vs-ledger split, and why it exists:**
```
StockLevel        — "Item X has 50 units at Location Y," right now.
                     A maintained cache: fast to read, no aggregation
                     needed for a POS screen or barcode scan to check it.
StockTransaction   — every event that ever changed that number, signed
                     (+50 received, -1 sold), never updated or deleted.
```
Summing every `StockTransaction.QuantityChange` for an item+location
should always equal its `StockLevel.QuantityOnHand`. That invariant isn't
enforced by a database constraint — it's a rule whichever command handler
changes stock has to uphold by writing both, in the same transaction. B2 is
where that handler gets written; B1 only builds the two tables it has to
keep in sync.

**Concepts introduced:**
- **A filtered unique index.** "At most one `ItemBarcode` per item may be
  primary" isn't "exactly one row per item" (an item can have many
  non-primary barcodes) — it's a unique index on `ItemId` that only
  applies `WHERE IsPrimary = 1`. Two non-primary barcodes for the same
  item coexist fine; two primary ones for the same item are rejected by
  the database itself.
- **`IDesignTimeDbContextFactory<T>`.** Identity's migrations were
  generated with `--startup-project Identity.API`, because that's where a
  real `DbContextOptions` (with an actual connection string) got built.
  There's no `Warehouse.API` yet — it's B3 — so there's nothing to point
  `dotnet ef` at. This factory is EF Core's answer: a class `dotnet ef`
  discovers and uses *only* at design time, with a connection string that
  the real running app will never read (`Warehouse.API`'s own
  `appsettings.json` supplies the real one once it exists).
- **A repository contract can say what it *won't* do.**
  `IStockLevelRepository` deliberately has no `AdjustQuantity`/`Upsert`
  method — deciding whether a `StockLevel` needs creating vs. updating,
  and what `StockTransaction` that produces, is a business decision, not a
  persistence primitive. Putting it here now would mean guessing at B2's
  design before writing it.
- **A cross-service reference that can't be a foreign key.**
  `StockTransaction.Reference` is a plain nullable string, not a foreign
  key to anything — once POS (Phase C) exists, it'll point at a `Sale.Id`
  living in a completely different service's database. A real FK
  constraint can't span that boundary; representing the link as plain data
  instead is how a microservices system has to handle it.
- **A deliberate scope cut.** Serialized/lot-tracked inventory (one row
  per physical unit — warranty devices, lots with expiry dates) is a
  different, heavier model (`InventoryUnit`: `ItemId`, `SerialNumber`,
  `Status`, ...) than quantity-based inventory. Nothing built here needs
  it yet, so it isn't built — it's a documented extension point for if a
  future item genuinely requires per-unit tracking, not a speculative
  table sitting unused.

**Verified with a focused runtime test (SQLite, deleted after — same
approach as A1, since SQL Server isn't reachable in this sandbox):** 13
checks, all passing, aimed straight at the scenario that prompted the
revision — a seeded item (`Cola 330ml Can`) with **two** barcodes, both
resolving to the same `ItemId`, sharing **one** `StockLevel` row at 50
units (not split across barcodes); a second *primary* barcode for the same
item is genuinely rejected by the filtered unique index, while a second
*non-primary* one is accepted fine; a different item (`Sparkling Water`)
carries its own independent base unit; Cola's `ItemUnit` conversion
(1 BOX = 12 PCS) computes correctly; and `Sku` lookups are confirmed
independent of any barcode.
