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
- [x] **B2 — Application layer (CQRS/MediatR/FluentValidation)**
- [x] **B3 — API + Angular Admin Panel screen (master data)**

**Phase C — POS using warehouse barcodes**
- [x] **C1 — Domain/Application/Infrastructure (Sale, checkout)**
- [x] **C2 — Sync call to Warehouse (barcode + stock check)**
- [x] **C3 — Async `SaleCompleted` event + saga (stock decrement, compensation)**
- [x] **C4 — Angular POS screen**
- [x] **C5 — Selling price history + promotions (POS pricing rules)**

**Phase D — Reporting**
- [x] **D1 — Event-driven read models**
- [x] **D2 — Reports + Angular dashboards**

**Phase E — Notifications / Mailing**
- [x] **E1 — In-app notifications (SignalR)**
- [x] **E2 — Mailing system (SMTP/MailKit)**

**Phase F — Hardening**
- [x] **F1 — Performance (Redis caching, pagination, compression, health checks)**
- [x] **F2 — Security hardening (role-based policies, gateway rate limiting, input validation review)**
- [x] **F3 — Localization (English/Arabic, RTL)**
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
  deployment/config discipline. This originally shipped as a literal
  copy-pasted into every service's own `appsettings.json`, flagged here
  as Phase F2's job to close — but as every later phase added one more
  service repeating the same copy-paste, the gap only got wider, so it
  was closed out of turn rather than left to compound further:
  `src/SharedSettings/jwt.settings.json` is now the ONE physical file
  that value lives in, loaded by every service's (and the gateway's own)
  `Program.cs` via `builder.Configuration.AddJsonFile(...)` before
  `AddJwtAuthentication` (or, for Notifications.API, its own hand-rolled
  equivalent) ever reads the `JwtSettings` section — nothing about how
  that section is READ changed, only where its values physically live.
  A real production deployment would likely source this file's values
  (or the whole file) from an actual secrets manager rather than a
  committed JSON file with a `CHANGE_ME` placeholder — that hardening
  step is still F2's to do; this fix only closes the "N copies that can
  drift" problem, not the "committed secret" one.
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

**A second revision: `Item.ParentItemId`, from a supermarket-specific
design review.** A follow-up review of this schema for supermarket use
raised a real fork `ItemUnit` doesn't cover: is a retail pack (e.g. "Water
500ml – Pack of 6") the *same sellable thing* as the single bottle, just
counted differently — or a genuinely separate product with its own shelf
price and barcode, where the pack price isn't simply 6× the single price?
The two cases need different models:
```
Same item, different counting unit    -> ItemUnit conversion (Cola/BOX,
 (one price, receive-by-carton,          already built above)
  sell-by-piece)

Independently priced/shelved pack     -> a SEPARATE Item, linked back via
 (its own barcode, its own price,        the new nullable ParentItemId
  its own StockLevel)                    (self-referencing FK, Restrict
                                          on delete)
```
Seeded as a concrete example: `BEV-WATER-500-PACK6` is its own `Item`
(own `Sku`, own barcode, priced at 6.50 rather than 6× the 1.20
single-bottle price) with `ParentItemId` pointing at the base
`BEV-WATER-500` item — and, correctly, its own independent `StockLevel`,
not a share of the single bottle's. `IItemRepository.GetVariants(id)`
answers "what pack variants exist for this base product." Verified with a
second focused SQLite test, 10/10 checks passing, including that scanning
the pack's barcode resolves to the pack `Item` (not the single bottle),
and that the two items' `StockLevel` rows are genuinely separate.

That same review listed several other real supermarket concepts — batch
numbers, expiry dates, purchase price vs. selling price, price history,
promotions, supplier tracking. Rather than absorbing all of it into B1,
only **selling price history + promotions** got filed as an explicit
near-term item (Phase C, POS/pricing — not Warehouse, since price and
promotion rules are a selling-side concern, not a stock-shape one). The
rest stays an unscoped list for now rather than half-built tables nothing
yet needs.

## B2 — Warehouse application layer

**What it does:** the business operations on top of B1's schema —
creating an item (with its first barcode), adding an extra barcode or an
alternate unit to an existing item, receiving stock, adjusting stock, and
the read side (item detail/list, barcode lookup, variants, per-location
stock, master-data dropdowns). Same CQRS/MediatR/FluentValidation shape as
Identity's Auth commands (A1) — `Features/<Area>/{Commands,Queries}/<Name>/`,
one command or query per folder, a `ValidationBehaviour` running
FluentValidation ahead of every handler, an `UnhandledExceptionBehaviour`
logging anything that isn't an expected `IHasStatusCode` failure.

**The one real addition to that pattern: `IUnitOfWork`.** B1 flagged this
gap explicitly — receiving or adjusting stock has to write a `StockLevel`
change *and* the `StockTransaction` that explains it, together, or the
ledger stops matching the balance. Identity's repositories never needed
this (`RegisterCommandHandler` only ever adds one `User`), so they call
`SaveChangesAsync()` themselves, inside `AddAsync`. Warehouse's
repositories don't — every `AddAsync`/`UpdateAsync` now only stages a
change on the tracked `DbContext`; a handler that needs several staged
changes to succeed or fail together calls `IUnitOfWork.SaveChangesAsync()`
itself, exactly once, at the end:
```
ReceiveStockCommandHandler:
  stage: StockLevel created-or-updated (+baseQuantity)
  stage: StockTransaction inserted (Reason = Received)
  IUnitOfWork.SaveChangesAsync()   <- both commit in ONE transaction
```
The same mechanism solves a second problem for free: `CreateItemCommand`
stages a new `Item` and its first `ItemBarcode` together, but the
barcode's `ItemId` can't be set directly — the `Item`'s `Id` is
database-generated and doesn't exist until it's saved. Setting
`itemBarcode.Item = item` (the navigation, not the FK value) and staging
both lets EF Core's change tracker fix up the real `ItemId` once
`SaveChanges` resolves the new `Item`'s key — both rows are created or
neither is.

**Unit conversion happens once, in the write path, never on read.**
`ReceiveStockCommand` accepts a quantity in *whatever unit the goods
arrived in* (`UnitOfMeasureId`) — receive 2 `CARTON`, say. The handler
resolves the item's `ItemUnit.ConversionFactor` for that unit (or treats
it as 1:1 if it's already the item's base unit) and converts to the base
unit *before* touching `StockLevel`/`StockTransaction`, which — per B1's
invariant — only ever speak the item's base unit. A conversion that
doesn't land on a whole number (`StockLevel.QuantityOnHand` is an `int`)
is rejected loudly (`ConflictException`) rather than silently rounded —
that would quietly corrupt a stock count.

**`AdjustStockCommand` and `ReceiveStockCommand` look similar but encode
different intent on purpose:** Receive may *create* a `StockLevel` (goods
can arrive somewhere for the first time); Adjust never does — adjusting a
balance that doesn't exist yet doesn't mean anything, so it's a
`NotFoundException`. Receive is always positive and always
`StockTransactionReason.Received`; Adjust is signed either direction and
always `StockTransactionReason.Adjustment` — the *command* the caller
chose to call carries that intent, so neither command exposes a `Reason`
parameter a caller could set inconsistently (e.g. an "Adjustment" that's
actually a sale — that's Phase C/C3's own event-driven path, not this).

**`InsufficientStockException`, filling in a prediction from B1.** B1's
`IHasStatusCode` comment predicted this almost by name: *"a future service
(Warehouse's 'InsufficientStockException', say) can opt into the same
handling just by implementing this interface — it never has to be added
to this shared library."* `AdjustStockCommand` rejecting a change that
would take `QuantityOnHand` negative is exactly that case — the exception
lives in `Warehouse.Application`, implements `Common.Exceptions.IHasStatusCode`
(409), and `Common.Exceptions` never had to change.

**Two DTO shapes for `Item`, not one.** `ItemSummaryDto` (list rows —
`GetAllItemsQuery`, `GetItemVariantsQuery`) carries no barcodes or unit
conversions; `ItemDetailDto` (`GetItemByIdQuery`, `ResolveBarcodeQuery`)
carries both plus the item's pack variants. Fetching those three
collections is a real per-item cost that a list of many items shouldn't
pay for every row it isn't going to show — the same list-vs-detail split
a REST API would make, just expressed as two DTO types instead of one
endpoint with an optional `?expand=` parameter.

**A query can return `null` instead of throwing.** Every command and
most queries throw `NotFoundException` on a missing id — but
`ResolveBarcodeQuery` (what a POS scan, Phase C, or an admin "look up by
barcode" box actually calls) returns `ItemDetailDto?` and hands back
`null` for an unknown barcode. Scanning something that isn't in the
catalog is an ordinary, expected outcome of using a barcode scanner, not
an exceptional one — the caller decides what a "not found" scan means
(show "unknown item," most likely), rather than having that decision
forced on it by a thrown exception.

**Concepts introduced:**
- **Manual DTO mapping, deliberately.** No AutoMapper — every DTO has a
  static `FromEntity(...)` method, same reasoning FluentValidation and
  MediatR get used explicitly rather than hidden behind a mapping
  convention: a reader can see exactly which entity fields end up where,
  and a mapper that needs a related entity already loaded (e.g.
  `ItemUnitDto.FromEntity` needs `itemUnit.UnitOfMeasure` `Include`d) says
  so in a comment instead of failing silently at runtime.
- **Business/existence checks live in the handler, not the validator.**
  FluentValidation validators here only ever check input *shape* (is the
  string empty, is the number positive) — whether a `Sku` is already
  taken, whether a `CategoryId` refers to a real `Category`, needs a
  database round trip, which is exactly why `RegisterCommandHandler` (A1)
  does its `UserNameExists` check in the handler rather than the
  validator. Same split here, applied consistently across five commands
  instead of one.

**Verified with a focused runtime test (SQLite, deleted after) run
through actual MediatR dispatch — not calling handlers directly, so the
`ValidationBehaviour`/`UnhandledExceptionBehaviour` pipeline is exercised
exactly as `Warehouse.API` (B3) will call it:** 25 checks, all passing.
Among them: creating an item and reading back its Id (proving the EF
navigation fixup actually worked, not a leftover temporary key);
promoting a new barcode to primary and confirming the *old* primary was
demoted in the same call, leaving exactly one primary; receiving 10 `PCS`
then 2 `CARTON` (2×24) on the same item and getting a `StockLevel` of 58;
adjusting -8 to reach 50, then confirming an adjustment that would go
negative throws `InsufficientStockException` while a location with no
existing `StockLevel` throws `NotFoundException` instead; rejecting a
duplicate `Sku`, a duplicate barcode, a duplicate `ItemUnit` conversion,
and a base-unit-as-conversion attempt, each with `ConflictException`; an
empty `Sku` rejected by `ValidationException` before the handler ever
runs; `GetItemVariantsQuery` finding B1's seeded pack-of-6 from its base
item; and `ResolveBarcodeQuery` returning the right item for a known
barcode and `null` — no exception — for an unknown one.

## B3 — Warehouse.API + gateway route + Angular Admin Panel

**What it does:** the outward-facing surface for everything B1/B2 built —
`Warehouse.API` (three thin controllers dispatching straight to MediatR,
same shape as Identity's `AuthController`), thirteen new Ocelot routes
under `/Warehouse/...`, and a real Angular screen replacing A4's
placeholder: create an item, browse the catalog, and manage one selected
item's barcodes, alternate units, and stock — all the way through the
actual JWT-protected HTTP pipeline, not a bypass.

```
ItemsController        GET/POST /Items, GET /Items/{id}, GET /Items/{id}/variants,
                        GET /Items/barcodes/{barcode}, POST /Items/{id}/barcodes,
                        POST /Items/{id}/units
StockController        GET /Stock/{itemId}, POST /Stock/receive, POST /Stock/adjust
MasterDataController   GET /MasterData/{categories,locations,units-of-measure}
```
Every route is `[Authorize]` — unlike Identity, there's no anonymous
Warehouse route at all, so it sits once on each controller rather than
repeated per action.

**A gap in `WebApplicationFactory`-based testing that doesn't show up in a
tutorial.** Verifying this step meant, for the first time, actually
booting the real ASP.NET Core pipeline (routing, JWT auth, MediatR,
exception handling) rather than calling handlers directly against EF Core
— B1/B2's tests never exercised any of that. Two real problems came up
doing it properly:
- `WebApplicationFactory<T>` locates its target's content root by walking
  up from the *test* assembly looking for a `.sln` — a throwaway test
  project living outside this repo's solution tree has nothing for that
  search to find, and just fails outright (`UseContentRoot` alone doesn't
  fix it; the factory's own internal `ConfigureWebHost` call runs the
  search regardless). The actual fix was placing the test project as a
  sibling *inside* `Services/Warehouse/` — deleted afterward, never
  committed — so the walk-up naturally reaches `WarehousePos.sln`.
- `Program.cs`'s own startup code calls `context.Database.Migrate()` —
  and `Migrate()` replays the *already-compiled* migration's literal DDL
  text, which is SQL Server dialect (`nvarchar(max)`, `datetime2`) because
  `WarehouseContextFactory` targets SqlServer at `dotnet ef migrations
  add` time. Swapping the DbContext to SQLite for the test doesn't help —
  that compiled text can't run against SQLite no matter which provider
  the *context* is configured for. The fix mirrors what real EF Core
  tooling does: build the schema fresh from the live model via
  `EnsureCreated()` (correct for whichever provider is actually active),
  then pre-seed the `__EFMigrationsHistory` table with this migration's
  Id so `Migrate()` sees nothing pending and skips straight through,
  rather than trying to replay incompatible SQL. `public partial class
  Program { }` was added to the end of `Program.cs` for this — top-level
  statements normally generate an invisible `Program` class;
  `WebApplicationFactory<Program>` needs a real type to target.

**The URL is authoritative over the body, not merely checked against
it.** `AddItemBarcodeCommand`/`AddItemUnitCommand` both carry `ItemId` as
a property (B2), and their controller routes are nested under a specific
item (`POST /Items/{id}/barcodes`). Rather than validating that the
route's `{id}` matches whatever `ItemId` the client put in the request
body — and rejecting a mismatch — the controller action just overwrites
`command.ItemId = id` unconditionally after binding. A route-says-item-5,
body-says-item-9 mismatch becomes structurally impossible rather than a
validation error to catch; verified directly by sending a deliberately
wrong `ItemId` in the body and confirming the barcode still landed on the
item named in the URL.

**A query returning `null` becomes an HTTP 404 at this layer, not
before.** B2 made `ResolveBarcodeQuery` return `ItemDetailDto?` rather
than throw `NotFoundException`, because an unknown scan is an ordinary
outcome of using a barcode scanner, not an exceptional one — and that
reasoning doesn't change here. What the controller adds is the HTTP
translation: `return result is null ? NotFound() : Ok(result);`. 404 *is*
the correct status for "this resource doesn't exist" — the controller
isn't overriding B2's decision, it's just the seam where an ordinary
domain-level `null` becomes the HTTP vocabulary a caller actually expects.

**Angular: replacing A4's placeholder for real.** `AdminShellComponent`
used to be the whole point of A4 — proving the auth chain (login → token
stored → guard lets you in → `currentUser` signal populated) worked end
to end, with a placeholder note saying master-data management "lands here
in Step B3." It still shows who's signed in; it now also hosts
`ItemsAdminComponent`, one screen (no lazy-loaded sub-routes — nothing
else in this app has that precedent yet, and one screen with a selection
panel doesn't need it) covering:
- **Create item** — a reactive form (`Sku`, `Name`, `Description`,
  `UnitPrice`, `Category`/`BaseUnitOfMeasure` selects populated from
  `MasterDataController`, an optional `ParentItem` select for pack
  variants, and the required first `Barcode`).
- **Items table** — every item, with a "Manage" action per row.
- **Selected item panel** — barcodes as chips (the primary one visually
  highlighted), alternate units, pack variants, current stock per
  location, and three mini-forms: add a barcode, receive stock (any unit
  the item supports, converted server-side per B2), and adjust stock
  (signed, only against a location that already has a `StockLevel`).

`WarehouseService` mirrors `AuthService`'s own shape exactly — one
`Injectable` wrapping `HttpClient`, every call built on
`${environment.apiBaseUrl}/Warehouse/...` (the gateway's upstream path,
never Warehouse.API's own port directly), no extra "API service"
abstraction layer, since none exists anywhere else in this app either.
`shared/models/warehouse.models.ts` mirrors every backend DTO and command
shape 1:1, same convention as `auth.models.ts`.

**Verified two ways.** The backend: a `WebApplicationFactory`-based test
(SQLite, deleted after) sending real HTTP requests through the real
pipeline — 21 checks, all passing, including a request with no token
(401), an expired token (401), a token signed with the wrong secret
(401), the URL-overrides-body barcode case above, the null→404 barcode
resolution, a validation failure surfacing as an actual 400
`ProblemDetails` body over HTTP (not just inside the Application layer),
and `InsufficientStockException` surfacing as a real 409. The frontend:
since this sandbox has no SQL Server, there's no live backend to
actually sign in against — `ng build` was verified clean, then the dev
server was driven with Playwright: first with a session token injected
directly into `localStorage` (bypassing the login call, matching exactly
what `AuthService` itself reads) to confirm the screen renders and the
create-item form is interactive with real network failures surfacing as
the expected toast (proving `errorInterceptor` still works for this new
feature) rather than a broken page; then again with the gateway's
`/Warehouse/...` routes mocked via Playwright's request interception,
returning realistic `ItemDetailDto`/`StockLevelDto` payloads, to actually
render and screenshot the selected-item panel — barcode chips, the
primary one highlighted, the BOX=12 PCS conversion, the stock table, and
all three mini-forms — confirming the whole template renders correctly
with real data, something `ng build` alone can't prove. No Angular
console errors in either run beyond the expected network failures.

**Try it locally** (needs SQL Server, which this sandbox doesn't have):
```bash
# From src/, in three terminals:
dotnet run --project Services/Identity/Identity.API
dotnet run --project Services/Warehouse/Warehouse.API
dotnet run --project ApiGateways/Gateway.Ocelot

# From client/:
npm start
# → http://localhost:4300, sign in as admin / Admin@12345
```

## C1 — POS domain + application + infrastructure

**What it does:** the third and final "layer" pattern in this codebase,
using the one Identity chose (A1) — Domain, Application, and
Infrastructure built together in a single step, rather than spread across
three like Warehouse (B1/B2/B3). POS's footprint is closer to Identity's
(two entities, a handful of commands) than to Warehouse's, so it gets
Identity's shape: `Sale`/`SaleLine`, five commands (`StartSale`,
`AddSaleLine`, `RemoveSaleLine`, `Checkout`, `CancelSale`), one query
(`GetSaleById`), all wired through the same `ValidationBehaviour`/
`UnhandledExceptionBehaviour` pipeline every other service uses. No
`POS.API` yet — same reasoning as B1 not having a `Warehouse.API`: there's
nothing for `dotnet ef` to point at, so `PosContextFactory` fills in at
design time, and this step is verified at the MediatR/EF Core level, not
over HTTP.

**A completed sale never touches Warehouse's database — on purpose.**
POS and Warehouse are separate services with separate databases;
`CheckoutCommand` only ever changes `Sale.Status`/`CompletedAt` in POS's
*own* database. Decrementing Warehouse's stock for what was just sold has
to happen as a *reaction* to this sale completing, not as a second write
folded into the same local transaction — there is no transaction that can
span two different databases here. `CheckoutCommandHandler` has a comment
marking exactly where that reaction plugs in once it exists: a
`SaleCompleted` event, fired from right after this commit, consumed by a
saga on Warehouse's side. That's Step C3, entirely unbuilt right now —
today, checking out a sale genuinely does nothing to Warehouse's stock.

**`SaleLine` snapshots `Sku`/`ItemName`/`UnitPrice` instead of reading
them live from Warehouse.** This is the same instinct as
`StockTransaction.Reference` (B1) applied to a different problem: a
completed sale is a *historical record* — a receipt printed today has to
keep reading the same tomorrow even if Warehouse's price or name for that
item changes next week. Only `ItemId` is kept as a live reference,
because C3's stock decrement genuinely needs to know which Warehouse item
this line refers to; everything a customer would see printed on a
receipt is captured once, at the moment the line was added, and never
re-read afterward.

**`AddSaleLineCommand` trusts its caller completely, and that's
deliberate, temporary scope, not an oversight.** It takes `Sku`/
`ItemName`/`UnitPrice` as plain input rather than resolving them itself —
nothing in this command reaches out to Warehouse to verify any of it.
That's exactly what makes Step C2 ("sync call to Warehouse: barcode
validation + stock check") a real, necessary next step rather than
already-done-by-accident: C2 is where a genuine cross-service call gets
inserted *in front of* this command, so the values it receives become
verified instead of merely assumed. Building that trust boundary
backwards — having `AddSaleLineCommand` itself reach into Warehouse right
now — would tangle a cross-service HTTP/gRPC call into a step that's
supposed to be about POS's own domain shape only.

**The same `IUnitOfWork` lesson from B2, recurring for the same reason.**
`AddSaleLineCommand` inserts a new `SaleLine` *and* updates its parent
`Sale.Total` — two writes that have to commit together, or the running
total stops matching the sum of its lines, exactly the
`StockLevel`/`StockTransaction` problem B2 solved for Warehouse.
`POS.Infrastructure` repositories only ever stage changes; `IUnitOfWork`
commits them. Identity never needed this (no Identity command touches
more than one entity) — POS does, the same way Warehouse did, which is
why this pattern keeps showing up in exactly the services whose commands
have to keep two rows in sync and never in the services that don't.

**Concepts introduced:**
- **A richer, three-state lifecycle enforced entirely by handlers, not
  by the entity.** `SaleStatus` (`InProgress` → `Completed`/`Cancelled`)
  has real transition rules — lines can only be added/removed while
  `InProgress`; only an `InProgress` sale can be checked out or
  cancelled; checkout refuses an empty sale. Every one of those checks
  lives in a command handler (`ConflictException`), not in `Sale` itself
  — `Sale`/`SaleLine` stay exactly as anemic as `Item`/`StockLevel` did in
  Warehouse. Consistency over introducing a richer domain-model style
  just because POS's lifecycle happens to be a good fit for one.
- **A deliberately narrow `Cancelled`.** It means "abandoned before
  payment," nothing more. A *post-completion* return/refund is a
  materially different feature — it needs a compensating stock increase
  on Warehouse's side, not just a status flip in POS — and the enum's own
  comment said so explicitly, so a future "add returns" step would find a
  clear, named gap instead of ambiguous room to misuse `Cancelled` for
  something it was never designed to mean.

  **Update:** that step has since landed — see the "Sale returns/refunds"
  section near the end of this document for `ReturnSaleCommand`, the new
  `Returned` status this bullet anticipated, and the compensating
  Warehouse/Reporting/Notifications flow it triggers.

**Verified with a focused runtime test (SQLite, deleted after — same
approach as B1/B2), dispatched through actual MediatR, not calling
handlers directly:** 16 checks, all passing. Among them: starting a sale
and adding two lines gives the exact expected running `Total` (3.00, then
8.00); removing a line brings `Total` back down correctly (5.00) and a
second attempt to remove the *same* line is rejected as not found;
checkout refuses an empty sale but succeeds on one with a line,
correctly stamping `CompletedAt`; a completed sale rejects *both* another
line being added to it and being checked out a second time; cancelling
an in-progress sale works but cancelling an already-completed one is
rejected; and `GetSaleByIdQuery` returns the right sale with the right
remaining line, or `NotFoundException` for an unknown one.

## C2 — POS → Warehouse sync call

**What it does:** replaces the trust `AddSaleLineCommand` (C1) was built
on with an actual cross-service call. C1's own README section named this
exact gap: the command took `Sku`/`ItemName`/`UnitPrice` as plain input,
verified by nothing. It now takes `Barcode`/`Quantity` instead — the
handler resolves the barcode against Warehouse's real catalog and checks
real stock at the sale's location *before* a `SaleLine` is ever written.
REST over HTTP, not gRPC: everything else in this system is already
HTTP/JSON (Ocelot, every controller), and introducing a second
serialization/transport stack for one call would be new machinery this
codebase doesn't otherwise need.

```
AddSaleLineCommandHandler
  1. Sale exists and is InProgress?                      (C1, unchanged)
  2. IWarehouseCatalogClient.ResolveBarcodeAsync(barcode) → item or null
     null → NotFoundException  (barcode validation)
  3. IWarehouseCatalogClient.GetAvailableQuantityAsync(item.Id, sale.LocationId)
     < requested → InsufficientStockException  (stock check)
  4. Snapshot item.Sku/ItemName/UnitPrice onto a new SaleLine (still C1's
     own reasoning — see SaleLine.cs — just fed VERIFIED data now)
```

**This is a service call, not a browser call — so it doesn't go through
the gateway.** Every Angular request goes through Ocelot (A3) because
that's the boundary between an untrusted browser and the system; POS
calling Warehouse is two backend services talking to each other, the kind
of call a service mesh or internal DNS entry would route directly in a
real deployment. Neither exists here, so `WarehouseApiOptions.BaseUrl`
just points straight at Warehouse.API's own port.

**Warehouse.API's routes are all `[Authorize]` (B3) — so this call still
needs a valid token, from a caller with no signed-in user behind it.**
`ServiceAuthHandler` mints one representing `pos-service` itself, signed
with the *same shared secret* every service already reads
(`JwtSettings:Secret`, `Common.Security`), attached to every outgoing
request via `AddHttpMessageHandler`. That's the simplest form of
service-to-service auth that works without adding anything new — no
service-account database, no OAuth2 client-credentials flow — and it
comes with an honest, named tradeoff: any service that can read this
shared secret can mint a token claiming to be *any* service, including
`pos-service`. A real deployment would want one dedicated token-issuing
endpoint (Identity.API, most naturally) so only one place ever signs a
token — that's Phase F2 (security hardening) territory, flagged here
rather than fixed here, the same way A4 flagged `localStorage` token
storage without fixing it on the spot.

**A duplicate about to happen got extracted instead.** Minting that
service token needs the exact same `SymmetricSecurityKey`/
`SigningCredentials`/`JwtSecurityToken` construction
`Identity.Infrastructure.JwtTokenGenerator` (A1) already had inline for
issuing user tokens. Copy-pasting it a second time is exactly the kind of
security-sensitive duplication that drifts — one call site changes the
signing algorithm or a claim convention and the other quietly doesn't.
`Common.Security.JwtTokenFactory.CreateToken(settings, claims, expiry)`
is now the one implementation; `JwtTokenGenerator` was refactored to call
it instead of constructing its own token, verified by minting through the
factory and validating the result against the exact
`TokenValidationParameters` shape `AddJwtAuthentication` builds — the
refactor didn't just compile, it produces a token every existing
validator still genuinely accepts.

**Concepts introduced:**
- **`IWarehouseCatalogClient` — a contract for something that isn't
  persistence, same shape as `IJwtTokenGenerator`/`IPasswordHasher`
  (Identity, A1).** `AddSaleLineCommandHandler` depends on the interface
  only; it has no idea the real implementation makes an HTTP call, hits a
  specific URL, or that Warehouse.API even exists. Swapping in a gRPC
  client later — or a stub for a test — changes zero lines in the handler.
- **A hand-rolled `HttpMessageHandler` stub instead of a real HTTP
  server.** Verifying `WarehouseCatalogClient` doesn't need a live
  Warehouse.API (no SQL Server here anyway) — a `DelegatingHandler`
  chain (`ServiceAuthHandler` → a stub returning canned JSON keyed by
  request path) exercises the *real* client code (JSON parsing,
  404-means-null, non-2xx-means-`WarehouseUnavailableException`) with zero
  network I/O and zero dependence on Warehouse actually running.
- **"No stock record" and "stock is genuinely zero" are the same
  answer, on purpose.** `GetAvailableQuantityAsync` returns `0` either
  way — an item Warehouse has simply never received at a given location
  isn't a special case worth a different code path from one that's been
  sold down to nothing; both mean "you can't sell this here right now."
- **A distinct exception for "the dependency itself is down."**
  `WarehouseUnavailableException` (503) is deliberately not the same as
  `NotFoundException` or `InsufficientStockException` — a barcode that
  doesn't resolve and a barcode that *can't be checked because Warehouse
  didn't answer* are different failures a cashier needs to react to
  differently, and conflating them into a generic 500 would erase that
  distinction right when it matters most (mid-sale, at a register).

**Verified with a focused runtime test (deleted after), two parts:** (1)
the `JwtTokenFactory` refactor — a token it mints validates against the
exact `TokenValidationParameters` shape `AddJwtAuthentication` builds, and
is correctly rejected when checked against a different secret; (2) the
full sync-call flow through actual MediatR dispatch, against the stubbed
Warehouse handler above — 10 checks total, all passing. Among them: a
known barcode resolves and the resulting `SaleLine`'s `Sku`/`ItemName`
come from Warehouse's (stubbed) response, not anything the test itself
supplied, and the running `Total` (7.50 = 2.50 × 3) is computed from that
resolved price; an unknown barcode throws `NotFoundException`; a
quantity exceeding what's on hand throws `InsufficientStockException`;
an item with no stock record at all correctly resolves to 0 available
rather than erroring; a simulated network failure surfaces as
`WarehouseUnavailableException`, not a raw `HttpRequestException`; and
every captured outgoing request carried a validly-signed Bearer token
identifying the caller as `pos-service`.

## C3 — SaleCompleted event + saga

**What it does:** decouples "the sale completed" from "Warehouse's stock
is now correct" — C2's sync call happens *before* checkout, to stop an
impossible sale from being rung up in the first place; this step is
about what happens *after* checkout, when the actual stock decrement
shouldn't block the cashier's request/response cycle or risk losing the
sale-vs-stock relationship if Warehouse is briefly unreachable.

```
CheckoutCommandHandler (POS)
  1. Sale is InProgress, has lines?             (C1, unchanged)
  2. Sale.Status = Completed, StockSyncStatus = Pending
  3. Write a SaleCompletedOutboxEntry (Pending) — SAME SaveChanges call as #2
  4. Return — the cashier's screen doesn't wait for Warehouse at all

SaleCompletedOutboxDispatcher (POS, polled every 10s)
  1. Load all Pending outbox entries
  2. POST to Warehouse.API /api/v1/StockEvents/sale-completed
     success → entry.Status = Sent, Sale.StockSyncStatus = Synced
     failure → entry.Attempts++; Attempts >= 5 → entry.Status = Failed,
               Sale.StockSyncStatus = Failed  (the compensating signal)

ApplySaleCommandHandler (Warehouse)
  1. ProcessedSaleEvent already exists for this SaleId? → no-op, return
     AlreadyProcessed = true   (idempotent receiver — safe against
     at-least-once delivery/retries)
  2. For every line: stage a stock decrement via StockAdjustmentStager
     (same "fetch → check → mutate" the direct AdjustStock command uses)
  3. Record a ProcessedSaleEvent, SaveChanges ONCE for the whole sale
```

**The outbox pattern, not a direct HTTP call from inside checkout.** A
POST to Warehouse straight out of `CheckoutCommandHandler` would create a
real failure window: if POS crashes (or Warehouse is down) between
committing the sale and making that call, the sale is Completed and
Warehouse never finds out — money taken, stock never adjusted, with no
record anything went wrong. Writing `SaleCompletedOutboxEntry` in the
*same* `SaveChangesAsync()` that completes the sale makes "sale
completed" and "an event was queued" atomic by construction — either
both happen or neither does, because they're one database transaction.
Actually delivering that event is a separate concern, handled by
`SaleCompletedOutboxDispatcher` on its own poll loop, so checkout's
response time never depends on Warehouse's.

**Idempotent receiver on Warehouse's side, because an outbox implies
at-least-once, not exactly-once, delivery.** A dispatcher that crashes
after POST-ing successfully but before marking the entry `Sent` will
retry it — Warehouse has to be able to see the same `SaleId` twice and
apply it exactly once. `ProcessedSaleEvent` (unique index on `SaleId`) is
the inbox side of the same idea `ProcessedSaleEvent` sounds like it
should have on the sending side: check-then-insert, `ApplySaleCommand`'s
very first statement, before any stock is touched.

**Atomicity across a multi-line sale doesn't need a hand-rolled
compensating transaction — because every line lands in the same
database.** A "saga" is machinery for coordinating a transaction that
spans *multiple* databases/services, each with its own local commit,
where a failure partway through means manually undoing whatever already
committed elsewhere. That's not this. All of a sale's lines decrement
stock in Warehouse's *one* database, so simply deferring
`SaveChangesAsync()` until every line has been staged gets atomicity for
free from the database's own transaction — if line 2 throws
`InsufficientStockException`, line 1's in-memory stock mutation was never
saved, so there's nothing to undo. Verified directly: a two-line sale
where the second line is short-stock leaves the *first* line's item
stock unchanged and records no `ProcessedSaleEvent`, so a retried
delivery starts clean rather than double-applying half a sale.

**The real compensating action only exists at the one boundary that's
actually cross-service: the outbox dispatcher giving up.** After
`MaxAttempts` (5) failed delivery attempts — Warehouse.API down for an
extended stretch — `SaleCompletedOutboxDispatcher` does not try to
un-complete the sale (the cashier already gave the customer a receipt;
the money is already taken) or spin forever. It sets
`Sale.StockSyncStatus = Failed` and dead-letters the outbox entry
(`OutboxStatus.Failed`) — a durable, queryable flag meaning "this sale's
effect on stock needs a human to reconcile," rather than an automatic
reversal that could itself go wrong in a different way.

**Concepts introduced:**
- **Outbox pattern** (`SaleCompletedOutboxEntry`) — write the "event to
  send" as an ordinary row in the same transaction as the state change
  it describes, then deliver it out-of-band. Named specifically for one
  event type rather than a generic `Type`/`Payload` envelope — the
  natural next step is generalizing it the moment a *second* event type
  shows up (D1's reporting events, E1's notifications), not before.
- **Inbox / idempotent receiver** (`ProcessedSaleEvent`) — the
  receiving-side counterpart to at-least-once delivery: a unique
  constraint that turns "processed this exact event already" into a
  cheap existence check instead of a re-derived side effect.
- **`StockAdjustmentStager`** — extracted the moment `ApplySaleCommand`
  needed the exact "fetch item/location/stock, validate, mutate,
  record a StockTransaction, don't save yet" sequence `AdjustStockCommand`
  (B2) already had; both commands now share one implementation of what
  a stock change *is*, and only differ in who's asking and why
  (`StockTransactionReason.Sale` vs. `.Adjustment`).
- **A background dispatcher that exists without a host to run it.**
  `SaleCompletedOutboxBackgroundService` is written and ready
  (`AddHostedService` away from running for real) but deliberately not
  registered anywhere — there's no `POS.API` yet, the same "built ahead
  of the API that will use it" situation `WarehouseContextFactory` was
  in during B1. This step's verification calls
  `SaleCompletedOutboxDispatcher.DispatchPendingAsync()` directly instead
  of through the background service, since exercising the poll loop
  itself would need a real host and add nothing the direct call doesn't
  already prove.

**A real bug this step's own verification caught:** `CheckoutCommandHandler`
originally serialized each outbox line as an anonymous type with
lowercase property names (`new { itemId, quantity }`), while the
dispatcher on the read side deserialized into `List<SaleCompletedLine>`
(`ItemId`/`Quantity`, PascalCase) using default, case-sensitive
`System.Text.Json` options. That mismatch doesn't throw — it silently
produces `ItemId = 0, Quantity = 0` for every line, which then failed
`ApplySaleCommandValidator`'s checks and surfaced as a generic "publish
failed" with no obvious cause. Fixed by serializing the *same* shared
`SaleCompletedLine` type on both the write and read sides, so the two
ends of the round trip can't drift out of casing sync again — caught
only because the verification asserted the actual resulting
`QuantityOnHand`, not just "the dispatch call didn't throw."

**Verified with a runtime test against two separate SQLite databases
(one per service, deleted after), 16 checks, all passing:** on the
Warehouse side alone — a first `ApplySaleCommand` for a `SaleId` applies
and records the correct `StockTransactionReason.Sale`; a *repeated*
delivery of that same `SaleId` returns `AlreadyProcessed = true` without
touching stock again; a two-line sale with one line short on stock
throws `InsufficientStockException` and leaves *both* lines' stock
untouched, with no `ProcessedSaleEvent` recorded. End to end — a real
`CheckoutCommand` against POS's database writes the outbox entry
atomically with completing the sale; dispatching it routes into
Warehouse's *actual* `ApplySaleCommandHandler` (via a publisher stub
that calls Warehouse's own mediator in-process instead of over HTTP,
each call through its own fresh DI scope so it mirrors the request-scoped
`DbContext` lifetime a real HTTP-hosted request would get) and Warehouse's
stock genuinely drops by the sold quantity; the outbox entry flips to
`Sent` and the `Sale.StockSyncStatus` flips to `Synced`. And the
compensating path — a publisher that always fails, dispatched 5 times,
dead-letters the outbox entry, flips `Sale.StockSyncStatus` to `Failed`
while `Sale.Status` stays `Completed`, and leaves Warehouse's stock
completely untouched for that sale.

## C4 — Angular POS screen

**What it does:** the register a cashier actually uses — start a sale at
a location, scan barcodes into a running cart, remove a mis-scanned line,
checkout, and see the receipt. Everything C1–C3 built (checkout, the
barcode/stock sync call, the outbox) had no UI in front of it yet; this
step is the first time a person can run a sale end to end without going
through `IMediator.Send` by hand.

**POS.API didn't exist before this step, and had to be built first.**
C1–C3 deliberately built POS.Domain/Application/Infrastructure ahead of
any host — `SaleCompletedOutboxBackgroundService` (C3) was written and
directly exercised by that step's own test, but had nowhere to actually
run on a poll loop until something registered it. That's the exact
situation `WarehouseContextFactory` was in before Warehouse.API showed up
in B3, and the resolution is the same: stand up the API. `POS.API` is a
line-for-line clone of Warehouse.API's own shape — same `Program.cs`
structure (`AddApplicationServices`/`AddInfrastructureServices`/
`AddCommonExceptionHandling`/`AddJwtAuthentication`, `Database.Migrate()`
on startup, Swagger with a Bearer scheme, `public partial class Program`
for testability) — with one addition Warehouse.API doesn't have:
`builder.Services.AddHostedService<SaleCompletedOutboxBackgroundService>()`,
finally giving C3's dispatcher a host to actually poll from.

```
SalesController (all actions [Authorize], no anonymous route — same as Warehouse.API)
  POST   /api/v1/Sales                    → StartSaleCommand
  GET    /api/v1/Sales/{id}                → GetSaleByIdQuery
  POST   /api/v1/Sales/{id}/lines          → AddSaleLineCommand
  DELETE /api/v1/Sales/{id}/lines/{lineId} → RemoveSaleLineCommand
  POST   /api/v1/Sales/{id}/checkout       → CheckoutCommand
  POST   /api/v1/Sales/{id}/cancel         → CancelSaleCommand
```

**`CashierUserId` is read from the caller's own JWT, not the request
body.** `StartSaleCommand`'s own comment (C1) flagged `CashierUserId` as
"trusted as given" input — nothing before this step ever closed that gap.
`SalesController.Start` now does: `command.CashierUserId =
int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!)`, overwriting
whatever the body claims, the same "context is authoritative over the
body" idiom `ItemsController.AddBarcode`/`AddUnit` (B3) established for
route-supplied ids. A cashier can no longer open a sale claiming to be a
different cashier just by editing the request payload — verified
directly: a request body claiming `cashierUserId: 999` still produces a
sale whose `CashierUserId` is the caller's real id from their token.
`LocationId` still isn't validated against Warehouse's real location
list; that's an open gap this step doesn't close either, same as before.

**No new "receipt" concept — `SaleDto` already was one.** `Checkout` and
`GetSaleById` both return the same `SaleDto` (with its `Lines`), so the
Angular receipt view is just that DTO rendered once `Status` is
`Completed` — there was never a reason to invent a second shape for what
is, at the data level, the exact same sale before and after payment.

**One Angular component drives all three states a sale moves through.**
`PosRegisterComponent` (`no sale` → `InProgress` → `Completed`) is a
single component with three template branches on `sale()`, the same
single-screen-with-a-selection-panel reasoning `items-admin.component.ts`
(B3) used rather than several routed sub-pages — a register's three
states are sequential, not independently navigable, so routing between
them would be fighting the domain rather than modeling it. It sits
directly on the `/pos` route with no `AdminShellComponent`-style wrapper:
that shell only exists because it was A4's own placeholder before B3's
real content landed in it, and there's no equivalent placeholder history
for POS to inherit.

**The barcode field IS the scan target — there's no separate "scan"
button.** A physical barcode scanner types the barcode's characters into
whatever text input has focus, followed by an Enter keystroke, completely
indistinguishable to the DOM from a cashier typing the digits by hand and
pressing Enter themselves. Binding `(keyup.enter)` on the barcode
`<input>` to the same `submitScan()` the Add button calls means the field
is scanner-ready with zero scanner-specific code — and after every
successful add, `focusBarcodeInput()` returns focus to it so the very
next scan doesn't need a click first, the way a real register keeps the
scanner "hot" between items.

**A real timing bug the frontend's own Playwright verification caught:**
the first call to `focusBarcodeInput()` — right after `StartSale`
succeeds — used a bare `setTimeout(() => this.barcodeInput?.nativeElement.focus())`,
reasoning that deferring to the next macrotask would give Angular's
change detection enough time to render the newly-`InProgress` branch
before the callback ran. On every *later* call (after adding a line) that
held; on this *first* transition specifically, `this.barcodeInput` was
still `undefined` when the macrotask fired — a bare `setTimeout` is not
actually a guarantee that Angular has committed a signal-triggered DOM
update, just a guess that it probably has by then. Fixed by switching to
`afterNextRender(callback, { injector })` (Angular's own primitive for
"run this once the pending render has actually happened"), which doesn't
guess. Caught only because the verification checked which element
actually had focus, rather than assuming a `setTimeout` had been enough
time.

**Concepts introduced:**
- **`POS.API`** — the fourth ASP.NET Core host in this system, and the
  reason `SaleCompletedOutboxBackgroundService` (C3) finally runs for
  real rather than only under a test harness calling
  `DispatchPendingAsync()` directly.
- **Reading identity from the token instead of the request body** — the
  first place in this codebase a controller pulls a *user* id
  (`ClaimTypes.NameIdentifier`) out of the caller's own JWT to override
  client-supplied input, rather than only doing this for route-path ids
  (B3's `{id}`-overrides-body pattern). Same idiom, applied to "who is
  this?" rather than "which resource?".
- **`afterNextRender`** — Angular's guarantee for "run this after the
  pending change-detection-triggered render has actually committed,"
  distinct from `setTimeout`'s "run this on some later tick and hope."
- **A scanner-ready input with no scanner-specific code** — `(keyup.enter)`
  plus autofocus-after-every-add is the entire "barcode scanning" feature
  from the frontend's point of view; the actual barcode *lookup* logic
  already existed in `AddSaleLineCommandHandler` since C2.

**Verified two ways.** Backend: a real ASP.NET Core pipeline (routing,
JWT auth, MediatR, exception handling) hosted via `TestServer` against
POS.API's own registration code — not `WebApplicationFactory<Program>`,
which insists on finding a `.sln` next to whatever test project points at
it and has none to find here — with `PosContext` swapped to SQLite and
`IWarehouseCatalogClient`/`ISaleCompletedPublisher` swapped for stubs, 15
checks, all passing: a request with no token (401), `CashierUserId`
overridden from the JWT claim even when the body claims a different one,
an unknown barcode (404) and over-stock quantity (409) both surfacing
correctly through real HTTP rather than a 500, a line's `Sku`/`ItemName`/
`UnitPrice` resolved from the (stubbed) Warehouse catalog rather than
anything the request supplied, removing the only line dropping the total
back to zero, checkout on an empty sale (409) and on an already-Completed
sale (409) both rejected, the outbox entry existing immediately after
checkout, and `GET /Sales/{id}` reflecting the same persisted total after
the fact. Frontend: `ng build` verified clean, then the dev server driven
with Playwright — a session token injected directly into `localStorage`
(bypassing login, matching exactly what `AuthService` reads) with the
gateway's `/Warehouse/MasterData/locations` and `/Pos/Sales...` routes
mocked via request interception, walking the full screen through all
three states: the start-sale card renders and lists the seeded location;
starting a sale renders the register with the barcode field already
focused; scanning an unknown barcode toasts an error without crashing;
scanning a real one renders it in the cart with the correct running total
and clears the field for the next scan; checkout renders the receipt with
a matching total; "New sale" returns to the start card. No unexpected
console errors in either run (an `HttpErrorResponse` logged after the
unknown-barcode scan is RxJS's own default behavior for an unhandled
observable error, not a bug — this component's `subscribe()` calls only
supply a `next` handler, the same discipline `items-admin.component.ts`
follows, on the reasoning that `errorInterceptor` already toasted it).

**Try it locally** (needs SQL Server, which this sandbox doesn't have):
```bash
# From src/, in four terminals:
dotnet run --project Services/Identity/Identity.API
dotnet run --project Services/Warehouse/Warehouse.API
dotnet run --project Services/POS/POS.API
dotnet run --project ApiGateways/Gateway.Ocelot

# From client/:
npm start
# → http://localhost:4300, sign in as a Cashier (see AuthController.Register), then /pos
```

## C5 — Selling price history + promotions

**What it does:** two closely related pricing features on the Warehouse
side — an audit trail of every real change to an item's list price
(`ItemPriceHistory`), and time-boxed markdowns on a single item
(`Promotion`, percentage or fixed-amount off) that automatically apply
the moment a cashier scans that item at POS, with zero POS-side
awareness that a discount exists at all.

```
UpdateItemPriceCommand (Warehouse)
  1. NewPrice == Item.UnitPrice already? → no-op, nothing recorded
  2. Otherwise: record ItemPriceHistory{OldPrice, NewPrice}, set
     Item.UnitPrice = NewPrice, one SaveChanges for both

EffectivePriceResolver.Resolve(item, nowUtc) (Warehouse)
  1. Any Promotion for this item with StartsAtUtc <= now <= EndsAtUtc?
     none → { UnitPrice = item.UnitPrice }  (no discount)
  2. PercentageOff  → item.UnitPrice * (1 - value/100)
     FixedAmountOff → item.UnitPrice - value
  3. Floor at 0 either way, round to 2 decimals
  4. → { UnitPrice (discounted), OriginalUnitPrice, PromotionId }

ResolveBarcodeQueryHandler / GetItemByIdQueryHandler (Warehouse)
  → call the resolver, hand the result into ItemDetailDto — the ONE
    place a real sale's price comes from (AddSaleLineCommandHandler,
    via IWarehouseCatalogClient) already goes through this

AddSaleLineCommandHandler (POS, unchanged control flow)
  → snapshots whatever UnitPrice/OriginalUnitPrice/PromotionId
    Warehouse handed back onto the new SaleLine, same as
    Sku/ItemName always have been (C1)
```

**Warehouse resolves the discount; POS never computes one.** The
alternative — POS asking "is there a promotion for this item?" as a
separate call, then doing the percentage/fixed-amount math itself —
would duplicate pricing logic across two services and create exactly
the kind of drift C2 already reasoned about for stock availability:
POS has to trust Warehouse's answer for "what does this cost," not
re-derive it. `IWarehouseCatalogClient.WarehouseItemLookup.UnitPrice`
is already the price to charge; `OriginalUnitPrice`/`PromotionId` ride
along purely so the receipt can show "was $15.00, now $7.50" without
POS needing to know anything about *why*.

**`UpdateItemPriceCommand` is the only sanctioned way to change
`Item.UnitPrice`.** There's deliberately no generic "edit item" command
that happens to let price through unnoticed — every price change goes
through this one command specifically so an `ItemPriceHistory` row is
guaranteed alongside it. Re-submitting the *same* price is treated as
"nothing happened," not a change worth recording — an audit trail that
logged every no-op price check would bury the changes that actually
matter under noise.

**`Promotion` is scoped to a single Item, not a Category or the whole
store.** The smallest slice that's still genuinely useful — a real
markdown on a real product — rather than guessing at a category-wide or
storewide shape nothing has asked for yet. The same "extract on second
use" discipline `StockAdjustmentStager`/`JwtTokenFactory` apply to code
in this codebase applies here to scope: build the narrow thing, widen it
the moment a second, different need actually shows up.

**A promotion floors at zero rather than letting a sale line go
negative.** `CreatePromotionCommand` has no way to check a
`FixedAmountOff` value against the item's current price at creation
time (nothing stops someone from lowering the price *after* the
promotion exists), so `EffectivePriceResolver` treats "discount bigger
than the price" as a data problem to contain, not something worth a
500 over — `Math.Max(discounted, 0m)`.

**Concepts introduced:**
- **`ItemPriceHistory`** — an append-only log of actual changes, not a
  running log of every price *check*; the "did it really change"
  filter lives in `UpdateItemPriceCommandHandler`, not in the entity.
- **`EffectivePriceResolver`** — the one place "an Item plus whatever
  Promotion is active" turns into a single number to charge, the same
  "one implementation of what a stock change is" reasoning
  `StockAdjustmentStager` applies to inventory, just applied to price.
- **A DTO field that means two different things depending on context.**
  `ItemDetailDto.UnitPrice` is the base price everywhere EXCEPT
  `ResolveBarcodeQuery`/`GetItemByIdQuery`'s results, where it's
  whatever `EffectivePriceResolver` computed — deliberate, since every
  consumer of "what does this cost" (a sale, a detail screen) wants the
  charge-able price, not the raw column; `OriginalUnitPrice` is what
  lets a caller reconstruct the raw price when it needs to (the admin
  panel's price-edit form does exactly this, since editing the
  *discounted* number would silently corrupt the real list price).

**Update:** the gap this section originally flagged here — no
"list every promotion for this item" query, only "what's active right
now" via price resolution, so an admin who created a promotion couldn't
later browse or cancel it — has since been closed. `GetPromotionsForItemQuery`
(returns every promotion, active or not) and `CancelPromotionCommand`
(adds a `Promotion.IsCancelled` flag; `GetActiveForItem` excludes
cancelled rows, so a cancelled promotion stops discounting immediately —
the original `StartsAtUtc`/`EndsAtUtc` window stays as historical
record, same reasoning `SaleLine`'s own snapshot fields already follow)
now back the Admin Panel's promotions table, with a Cancel button per
still-live row. Verified with a 9-check runtime test (create two
promotions, browse both, cancel the active one, confirm
`EffectivePriceResolver` stops applying it, confirm cancelling twice or
cancelling via the wrong item both throw) and an 8-check Playwright pass
against the built Angular app.

**Verified with a runtime test against an in-memory SQLite database
(Warehouse) and a second, separate one (POS), 16 checks, all passing:**
a genuine price change records exactly one `ItemPriceHistory` row and
updates `Item.UnitPrice`; re-submitting the identical price adds no
second row; a second genuine change adds a second row, most-recent
first. A `PercentageOff` promotion that's currently active correctly
halves the resolved price while surfacing the original price and the
promotion's id; a promotion that hasn't started yet, and one that
already ended, both leave the price untouched; a `FixedAmountOff`
promotion subtracts a flat amount; a fixed discount larger than the
item's own price floors the result at zero rather than going negative.
End to end — `AddSaleLineCommand` against a stub resolving to a
discounted price snapshots the discounted `UnitPrice` AND the
`OriginalUnitPrice`/`PromotionId` onto the persisted `SaleLine`, and
`LineTotal` is computed off the discounted price, not the original.
The frontend: `ng build` verified clean, then the dev server driven
with Playwright, gateway routes mocked — the admin panel's new pricing
section renders and pre-fills the list price from `OriginalUnitPrice`
(not the possibly-discounted `UnitPrice`); submitting an updated price
calls the backend and shows a success toast; the price-history table
renders the change; creating a promotion calls the backend and the
"promotion active" note appears showing both prices; on the POS
register, both the cart and the completed receipt render the original
price struck through next to the discounted one. No unexpected console
errors.

## D1 — Event-driven read models

**What it does:** a fourth service, Reporting, that builds its own
denormalized copy of "what happened" — completed sales and current stock
levels — by consuming events POS and Warehouse publish, rather than
querying either service's database directly. Nothing in D1 aggregates or
displays that data yet (no "sales by day," no dashboard) — that's D2's
job, on top of the read models this step gets right.

```
POS Checkout                          Warehouse StockAdjustmentStager
  → OutboxMessage(SaleCompleted)        → OutboxMessage(StockLevelChanged)
  → OutboxDelivery(Warehouse)           → OutboxDelivery(Reporting)
  → OutboxDelivery(Reporting)                    ↓
         ↓                              Reporting.API /Events/stock-level-changed
POS OutboxDispatcher                          ↓
  → WarehouseEventPublisher (C3,      IngestStockLevelChangedCommand
     unchanged) → Warehouse.API         → upsert StockLevelRecord(ItemId, LocationId)
  → ReportingEventPublisher (new)
     → Reporting.API /Events/sale-completed
              ↓
     IngestSaleCompletedCommand
       → SaleRecord (once per SaleId)
       → SaleLineRecord × N
```

**The outbox generalizes from "one event, one consumer" to "one event,
many deliveries" — exactly the next step C3's own README named.** Before
D1, `SaleCompletedOutboxEntry` was one row per sale, delivered to exactly
one place (Warehouse). Reporting needing the SAME event broke that
1:1 assumption, so POS's outbox split into `OutboxMessage` (the event
itself — `EventType`/`PayloadJson`, written once) and `OutboxDelivery`
(one row per `(message, consumer)` pair, each with its own
Pending/Sent/Failed status and retry count). `CheckoutCommandHandler` now
writes one message and fans it out to `["Warehouse", "Reporting"]` in the
same transaction that completes the sale; `OutboxDispatcher` resolves
each delivery's `IEventPublisher` by `ConsumerName` and retries each one
independently. The payoff shows up directly in the one piece of legacy
behavior that couldn't fully generalize: `Sale.StockSyncStatus` only
means "did Warehouse confirm the stock decrement" — Reporting failing to
ingest a sale (for whatever reason) leaves that status completely alone,
because the two deliveries now genuinely don't share fate. Warehouse got
the identical message/delivery shape for its own first-ever outbox
(`StockLevelChanged`, one consumer today — Reporting) rather than
inventing something simpler for a single consumer, on the same
reasoning: the shape is already right the moment a second Warehouse-side
consumer shows up (E1's notifications, most likely).

**Warehouse making its first OUTBOUND service call ever is a bigger deal
than it looks.** Every previous Warehouse interaction was inbound —
`StockEventsController` (C3) receiving POS's calls, `ItemsController`
(B3) receiving the admin panel's. `ReportingEventPublisher` is the first
time Warehouse calls out to anything, which is why
`Warehouse.Infrastructure` needed a `ServiceAuthHandler` and a
`Common.Security` reference for the first time too — the exact
dual-use POS.Infrastructure already had since C2 (mint a token to call
someone else, validate tokens presented to you, two different jobs, same
package). And because Warehouse.API already exists (unlike POS's own C3
outbox, which had to wait for POS.API to exist before its background
service could be hosted anywhere), `OutboxBackgroundService` gets
registered via `AddHostedService` in the very same step that wrote it —
no deferred wiring this time.

**The event's payload is forwarded to Reporting verbatim, not
re-serialized.** `ReportingEventPublisher` (both POS's and Warehouse's)
posts `OutboxMessage.PayloadJson` as raw `StringContent`, because
`SaleCompletedMessage`/`StockLevelChangedMessage`'s own property names
already match `IngestSaleCompletedCommand`/`IngestStockLevelChangedCommand`
exactly — there's no shape translation to do, unlike
`WarehouseEventPublisher`, which DOES re-map (it only wants `ItemId`/
`Quantity` per line, ignoring the richer fields Reporting needs). ASP.NET
Core's default model binding is case-insensitive, which is what makes
forwarding a raw PascalCase-serialized string straight into a controller
action safe — but this step's own runtime test checks the ACTUAL
resulting `SaleRecord`/`StockLevelRecord` values over real HTTP rather
than assuming that, precisely because a casing mismatch across a JSON
boundary silently produced wrong data once already (C3's own bug).

**Two different idempotency strategies, because the two events are
different shapes of fact.** `SaleRecord` is idempotent via a dedup
check — `ExistsForSale(saleId)` before inserting, the same
existence-check idiom `ApplySaleCommand`'s `ProcessedSaleEvent` (C3)
used, because a sale is an immutable, one-time fact: applying it twice
would double-count reporting totals. `StockLevelRecord` is idempotent
via upsert instead — no dedup check at all, because a stock level is a
continuously-changing current snapshot (like `StockLevel` itself,
B1): applying the "same" `StockLevelChanged` event twice just writes the
same `QuantityOnHand` twice, which is harmless by construction. Neither
needed a generic inbox table the way a naive "idempotent receiver"
pattern might suggest — the right dedup strategy depends on what kind of
fact the event actually represents.

**Concepts introduced:**
- **Outbox message/delivery split** — one event, independently-tracked
  deliveries per consumer. See above.
- **`Reporting` service** — the fourth ASP.NET Core host, and the first
  one whose entire job is projections built from other services' events
  rather than owning any transactional data of its own.
- **`ReadModelsController` named apart from a future `ReportsController`**
  — `GET /Reporting/sales` and `/Reporting/stock-levels` are raw dumps of
  what's been ingested, proving the read model is correct and queryable;
  they are deliberately NOT the aggregated sales-by-day/top-selling/
  low-stock reports D2 will build. Routed through the gateway (unlike
  `EventsController`, which is service-to-service and isn't), since a
  future Angular dashboard will eventually call something at this same
  layer.
- **Upsert-based idempotency** as a second, equally valid alternative to
  dedup-based idempotency (C3) — see above.

**Update:** the gap this section originally flagged here — `ReceiveStockCommand`
didn't go through `StockAdjustmentStager` (it needs unit conversion
first), so receiving stock via a purchase order never emitted a
`StockLevelChanged` event, only `AdjustStockCommand`/`ApplySaleCommand`
did — has since been closed. `Stage()` grew a `createIfMissing` parameter
(a PO receipt can be the FIRST stock this item has ever had at this
location, unlike an adjustment or sale, which both require a balance to
already exist), and `ReceiveStockCommandHandler` now calls it after its
own unit conversion. Received stock shows up in Reporting (and, once E1
exists, Notifications) exactly like every other stock change.

**Verified with a 19-check runtime test spanning three separate SQLite
databases, one per service — Reporting hosted via a real ASP.NET Core
pipeline (`TestServer`, the same approach C4 established for POS.API),
Warehouse and POS as direct DI containers, all wired together with the
ACTUAL production `IEventPublisher` implementations pointed at
Reporting's real `HttpClient`, not stand-ins:** `AdjustStockCommand`
stages exactly one Pending delivery for Reporting, dispatching it lands
a real `StockLevelRecord` in Reporting's database with the correct
post-adjustment quantity; a multi-line `ApplySaleCommand` where one line
is short on stock stages NO outbox message at all (not even for the
valid line) — the event is exactly as atomic as the stock change it
describes; a successful multi-line sale emits one event per line, and
dispatching it upserts the SAME `StockLevelRecord` row rather than
creating a second one. End to end on the POS side — a real `Checkout`
fans out to exactly two deliveries, dispatching both marks them Sent,
`Sale.StockSyncStatus` reflects only the Warehouse delivery's outcome,
and Reporting's real ingestion endpoint — reached over actual HTTP, not
an in-process shortcut — produces a `SaleRecord`/`SaleLineRecord` with
the real resolved Sku/quantity/line total. And a repeated delivery of the
same sale (simulating a dispatcher retry after an already-successful
delivery) inserts no second `SaleRecord`, confirming the dedup check
actually works, not just the upsert.

## D2 — Reports + Angular dashboards

**What it does:** the real, aggregated reports D1 deliberately stopped
short of — sales by day, top-selling items, and a low-stock list — built
as genuine `GROUP BY` queries over D1's read models, exposed through a
new `ReportsController`, and rendered on a new `/reports` Angular route
with three visualizations. `ReadModelsController` (D1) still exists
unchanged — `GET /Reporting/sales` and `/stock-levels` are raw dumps,
`ReportsController` is the aggregated view built on top of them; the two
were named apart from day one specifically so this step wouldn't need to
rename anything.

**`StockLevelChanged` gained the fields Reporting actually needed to
build a human-readable report, not just a queryable one.** D1's version
of the event carried only `ItemId`/`LocationId`/`QuantityOnHand` —
enough to upsert a row, not enough to show anyone a low-stock table
without a name. Reporting has no live reference back to Warehouse's
catalog (that's the entire point of the read-model pattern — no
cross-service joins), so `Sku`/`ItemName`/`LocationCode`/`LocationName`/
`ReorderThreshold` are now snapshotted onto the event itself and
re-snapshotted on every subsequent event for the same item — a Warehouse-
side rename eventually catches up rather than freezing whatever the
first-ever event happened to say. `StockAdjustmentStager` is the only
place that changed on the Warehouse side; nothing about `StockLevel`
itself, or how it's persisted, moved.

**Two EF Core `GROUP BY` queries, and a portability gap the runtime test
actually caught.** `GetSalesByDay()` groups `SaleRecord` by
`CompletedAtUtc.Date`; `GetTopSellingItems(take)` groups `SaleLineRecord`
by `ItemId`, using `MAX(Sku)`/`MAX(ItemName)` for the two dimension
attributes rather than reaching for `g.First()` — a `GROUP BY` row has no
inherent order to pick a "first" line from, and `MAX` over a string is a
real, universally-translatable SQL aggregate every provider agrees on.
The measures (`SUM(Total)`, `SUM(LineTotal)`) are where SQL Server and
SQLite genuinely disagree: SQL Server sums a `decimal` column natively,
but SQLite — which has no native decimal type — has no translation for
`SUM(decimal)` at all, and the very first run of this step's own runtime
test threw `NotSupportedException` proving it. Both queries now sum as
`double` in SQL and cast back to `decimal` after materializing, a
documented precision-for-portability tradeoff (ample for a reporting
total, wrong for a ledger) rather than special-casing the SQLite
substitute — the same query runs unmodified against the real SQL Server
this project targets.

**The Angular dashboard adds no charting library.** Consistent with this
project's minimal-dependency habit (Ocelot instead of a heavier gateway,
manual mapping instead of AutoMapper's runtime cost until B2 proved it
justified), `/reports` is built from an inline SVG bar chart and a plain
HTML/CSS bar list rather than pulling in a chart package for three fairly
simple visualizations. The two chart forms were picked for what each
axis actually is: sales-by-day is a chronological x-axis, where SVG's
precise coordinate math earns its keep (bars are drawn as rounded-top
paths, not a plain `rect` with `rx` — an SVG rect rounds all four
corners, and the mark spec calls for square at the baseline, rounded only
at the data-end); top-selling is a ranked list of named items, where
ordinary HTML/CSS handles text truncation and layout far more simply than
SVG `<text>` would. Both stay single-series (categorical slot 1 blue,
`#2a78d6`) and skip a legend box on purpose — a legend restates what the
card title already says for one series. Hover is part of the deliverable
rather than an afterthought: each bar/row lifts (a brightness bump, never
a border — an outline is ink that isn't data) and the sales-by-day chart
shows a value+date tooltip, all reachable on keyboard focus the same as
on hover, not hover-only.

**Low stock's status color never carries meaning alone.** Every row
`GetLowStock` returns is already at-or-below its own threshold by
definition, so the table adds one more split on top: zero on hand
(`critical`, red `#d03b3b`) versus merely below threshold (`warning`,
amber). Both always ship with an icon *and* a text label ("Out of stock"
/ "Low stock") next to the colored badge — never the color by itself —
per the same status-palette rule the reference palette documents (three
of its four status steps are sub-3:1 contrast on a light surface by
design; the icon+label pairing is what makes that acceptable, not
optional polish).

**Concepts introduced:**
- **Cross-service event enrichment for a consumer's own future need** —
  `StockLevelChanged` grew fields Warehouse's own domain has no use for,
  purely because Reporting (a different service, with no other way to
  get them) needed them to render a report. See above.
- **SQL provider portability for decimal aggregates** — `SUM` over
  `decimal` has no SQLite translation; sum as `double`, cast back after
  materializing. A tradeoff worth naming, not a workaround to hide.
- **`MAX()` as the correct way to pick a stable dimension attribute out of
  a `GROUP BY`** when there's no real "first" row to reach for.
- **A hand-rolled chart built to a written methodology (mark specs, a
  validated color assignment, a hover layer that's part of the
  deliverable) instead of either an ad hoc div-with-a-width-percentage or
  a new dependency.** The three visualizations here don't need a charting
  library; they do need the same care one would bring to using one.

**Update:** the gap this section originally flagged here — a purchase-
order receipt keeping the low-stock table stale until an unrelated
`AdjustStockCommand`/sale next touched the same `(ItemId, LocationId)`,
because `ReceiveStockCommand` never emitted `StockLevelChanged` — has
since been closed (see D1's own updated note for the fix itself). A
receipt now updates the low-stock report the moment it happens, same as
every other stock change.

**Verified with an 11-check runtime test (three SQLite databases in one
process — Warehouse and Reporting, same discipline as D1) plus a 9-check
Playwright pass against the built Angular app:** the backend test proved
the enriched event payload actually carries `Sku`/`ItemName`/
`LocationCode`/`ReorderThreshold` (not just ids), that the low-stock
report both enters *and exits* correctly across an upsert (not a second
row), that `GetSalesByDay` groups two sales landing on two different
calendar days into two rows rather than one, and that `GetTopSellingItems`
sums quantity and revenue correctly across multiple sales of the same
item, orders by revenue rather than quantity (a single 100.00 sale
outranks five 4.00 units totaling 20.00), and respects `Take`. This is
also the run where the SQLite `SUM(decimal)` gap above was caught, before
it could reach the frontend. The Playwright pass (mocked gateway routes, a token seeded into
`localStorage`, the built app served and driven in a real Chromium)
confirmed the dashboard renders the right bar/row/table counts, orders
top-selling by revenue, shows the critical/warning split correctly, lifts
and tooltips a bar on hover, reloads top-selling when the take-selector
changes, and raises no unexpected console errors.

**Run it locally (requires the gateway + Identity/Warehouse/POS/Reporting
APIs running, per D1):**
```bash
cd client
npm start   # ng serve — http://localhost:4200
# → sign in, then /reports
```

## E1 — In-app notifications (SignalR)

**What it does:** a fifth service, Notifications, that consumes the same
`SaleCompleted`/`StockLevelChanged` events Reporting already does (D1/D2)
— fanned out from POS's and Warehouse's existing outbox, not a new event
source — and turns qualifying ones into short, human-readable messages,
pushed live to every connected browser over a SignalR hub and persisted
so a bell dropdown has something to show on page load, before any push
has happened yet.

**Both outbox producers generalize to a third consumer with zero changes
to the dispatcher itself — exactly the point of the message/delivery
split D1 made.** POS's `CheckoutCommandHandler` already fanned
`SaleCompleted` out over an array of consumer names (its own comment
literally named this step as the anticipated third); adding
`Notifications` there was a one-line change. Warehouse's
`StockAdjustmentStager` had never needed more than one consumer for
`StockLevelChanged`, so it staged a single hardcoded delivery — this
step turns that into the same array-loop idiom POS already used, for
Warehouse's second-ever consumer. `OutboxDispatcher` (both services)
needed no changes at all: it already resolves `IEnumerable<IEventPublisher>`
by `ConsumerName`, so a new `NotificationsEventPublisher` in each
service's own Infrastructure project (same typed-HttpClient-plus-
`ServiceAuthHandler` shape as their existing `ReportingEventPublisher`)
was the only new code the producer side needed.

**The ingestion commands bind only what a notification message actually
needs, and let ASP.NET Core's model binding quietly ignore the rest.**
`IngestSaleCompletedCommand` declares just `SaleId`/`Total` — POS's
`NotificationsEventPublisher` still forwards the FULL `SaleCompletedMessage`
JSON verbatim (the same "no shape translation, no re-serialization"
idiom D1 established), and the extra fields (`Lines`, `CashierUserId`,
...) are simply never bound. `IngestStockLevelChangedCommand` mirrors
Reporting's own version field-for-field since a low-stock message needs
the same denormalized `Sku`/`ItemName`/`LocationName`/`ReorderThreshold`
D2 already added to that event.

**Two different idempotency shapes, because the two events answer
different questions.** `SaleCompleted` gets a real dedup key —
`Notification.SourceSaleId`, unique-indexed, checked via `ExistsForSale`
before inserting — the exact existence-check idiom D1's `SaleRecord`/C3's
`ProcessedSaleEvent` already established for a one-time, immutable fact:
"did we already tell someone about sale #123." `LowStock` answers a
different question — "did this item just CROSS INTO low stock, or was
it already there" — which existence-checking can't answer on its own,
since the same item can legitimately re-cross that line many times.
Notifications keeps its own tiny `StockLevelSnapshot` per `(ItemId, LocationId)`
(the last-known `QuantityOnHand`/`ReorderThreshold` it saw), upserted on
every event regardless of outcome, purely to compute "was it low
BEFORE this event." A notification fires only when that comparison
flips from not-low to low; a brand-new pair with no snapshot yet counts
as "wasn't low," so an item that arrives already low on its very first
event still notifies. This is deliberately NOT a second copy of
Reporting's own `StockLevelRecord` (D1/D2) — it carries none of the
denormalized display fields that table has, only the two numbers needed
to answer one yes/no question.

**The SignalR hub and its `INotificationPusher` implementation live in
the API layer, not Infrastructure — a placement decision, not a broken
rule.** Every other cross-service concern in this system (outbound HTTP
via `IEventPublisher`, `ServiceAuthHandler`) sits in Infrastructure
because it's genuinely persistence/outbound-call plumbing. A SignalR
`Hub` is different: it's inseparable from THIS service's own ASP.NET
Core request pipeline and hosting model — mapping it, authenticating
its connections, and pushing through it are all things only the API
layer's `Program.cs` can actually wire up. `Notifications.Application`
still only knows about the transport-agnostic `INotificationPusher`
interface; it has no idea SignalR exists.

**A WebSocket handshake can't carry an `Authorization` header, so the
token travels as a query-string parameter instead — and that couldn't be
wired into the shared `AddJwtAuthentication` extension without affecting
every other service's normal header-based validation too.**
Notifications.API hand-rolls its own `AddJwtBearer` call (same
`TokenValidationParameters` every service already uses) with one
addition: `JwtBearerEvents.OnMessageReceived` pulls `?access_token=...`
out of the query string, but ONLY for requests under `/hubs/notifications`
— every controller's ordinary `Authorization: Bearer ...` header check is
completely untouched, and `Common.Security` itself needed no changes.

**The SignalR hub is the one thing in this whole system that does NOT go
through the gateway — and that decision comes with its own new,
previously-unneeded piece of infrastructure: CORS.** Ocelot's HTTP-
forwarding model doesn't reliably proxy a WebSocket upgrade handshake,
so rather than build and debug that against a reverse proxy with no
live SQL Server or docker-compose stack to validate it against anyway,
the Angular client connects to Notifications.API's own port directly for
the hub connection only — `NotificationsController`'s plain REST
endpoints (`GetRecent`, mark-as-read) still go through the gateway like
every other feature. A direct browser-to-service connection is a
cross-origin request the gateway never had to mediate, which is why this
is also the first and only place in this project CORS gets configured at
all: every other feature's browser traffic has only ever gone through
Ocelot in a mocked-gateway Playwright run, never a real one, so whether
the REST side would need CORS too has simply never been exercised. That
gap is real and wider than this step — flagged here rather than solved,
left for F4's actual docker-compose pass to be the first time anything
in this system talks to a truly separate origin for real.

**Concepts introduced:**
- **A live-push transport as a third delivery mechanism**, alongside
  "plain HTTP through the gateway" (every REST call so far) and
  "service-to-service ingestion" (`EventsController`, D1) — with its own
  auth wiring and its own gateway-bypass tradeoff, both named above.
- **API-layer-owned real-time transport vs. Infrastructure-layer-owned
  persistence** as a considered placement, not a violation of the
  Domain→Application→Infrastructure→API layering every other service
  follows — see `INotificationPusher`'s own split above.
- **Crossing-edge detection via a purpose-built snapshot** as a third
  idempotency shape alongside dedup-by-existence-check (D1) and
  upsert-without-dedup (D1's own `StockLevelRecord`) — some questions
  ("is this the same fact twice") existence-checking answers directly;
  others ("did a value just cross a line") need the PREVIOUS value on
  hand to answer at all, which is what `StockLevelSnapshot` exists for.
- **`NotificationFeedService`, kept deliberately distinct from the
  pre-existing `NotificationService`** (A4's MatSnackBar toast wrapper) —
  one is stateless and transient, the other persists a feed and a live
  connection; the new one calls the old one on every push, so a live
  event is both remembered and immediately seen without either service
  knowing much about the other.

**Update:** two of the three gaps this section originally flagged have
since been closed. `ReceiveStockCommand` now routes through
`StockAdjustmentStager` (see D1's updated note) — a purchase-order
receipt reaches Reporting AND Notifications the moment it happens, the
same as every other stock change. And LowStock notifications no longer
fire on every qualifying event: `Notifications` now keeps its own tiny
`StockLevelSnapshot` per `(ItemId, LocationId)` — exactly the "second
copy of Reporting's read model, purely to suppress its own noise" this
section originally said it would need to build — and only notifies on
the actual transition into low stock (a brand-new item/location with no
prior snapshot that arrives already low still notifies; leaving low
stock never does; crossing into low stock a second time notifies again,
so suppression is per-transition, not permanent). The CORS-everywhere
question above is the one gap that remains open here — still F4's to
actually confront with a real docker-compose stack.

**Verified with a 19-check runtime test — three SQLite databases, one
per service, Notifications hosted via a real ASP.NET Core pipeline
(`TestServer`, the same approach D1 established for Reporting) with a
REAL `Microsoft.AspNetCore.SignalR.Client` connection, not a stand-in —
plus a 9-check Playwright pass against the built Angular app:** the
backend test proves Warehouse's `AdjustStockCommand` now stages
deliveries for exactly `{Reporting, Notifications}` and POS's
`CheckoutCommand` for exactly `{Warehouse, Reporting, Notifications}`;
that an unauthenticated SignalR connection attempt is actually rejected
(`[Authorize]` on `NotificationsHub` isn't a no-op); that a real,
token-bearing connection receives a REAL push — not a mocked one — the
instant `EventsController` ingests a qualifying event, with the right
message text; that a repeated delivery of the same sale produces no
second push; that the identical low-stock event redelivered DOES push
again (the named gap, proven real); that a normal, non-low stock change
pushes nothing; and that mark-one/mark-all-as-read both work and are
reflected in a subsequent `GetRecent`. The Playwright pass mocks every
gateway REST route as usual but — because the hub bypasses the gateway —
runs a REAL throwaway `Notifications.API` host on its real port for the
hub connection alone: a real login token, a real WebSocket, and a real
server-side `IHubContext` push land in a real Chromium browser, updating
the bell's unread badge and firing a toast with no page reload, which is
the one thing a fully mocked gateway could never have actually proven.

**Run it locally (a sixth terminal, alongside
Identity/Warehouse/POS/Reporting/Gateway):**
```bash
cd src/Services/Notifications/Notifications.API
dotnet run   # http://localhost:5298

cd client
npm start   # ng serve — http://localhost:4200
# → sign in; the bell icon in the toolbar shows the live notification feed
```

## E2 — Mailing system (SMTP/MailKit)

**What it does:** a second delivery channel for the exact same LowStock
crossing-edge decision E1 already computes — alongside the in-app
SignalR toast, `IngestStockLevelChangedCommandHandler` now also emails a
fixed list of configured alert recipients through a real SMTP relay
(MailKit's `SmtpClient`), the moment an item/location pair crosses INTO
low stock. No new event, no new ingestion endpoint, no new command:
this is entirely a second consumer of a decision E1 already made.

**`IEmailSender` mirrors `INotificationPusher`'s own split exactly — a
transport-agnostic Application-layer interface, a concrete Infrastructure-
layer implementation — for the same reason.** Neither interface takes a
"who" parameter: just as `SignalRNotificationPusher` broadcasts to every
connected client because there's no per-user targeting concept anywhere
in this system yet, `IEmailSender.SendAsync(subject, body, ct)` sends to
a FIXED recipient list (`SmtpSettings.Recipients`, bound from
configuration) rather than an address the caller picks. Splitting by role
or letting a user opt in/out of alert emails is the same kind of
"natural F-phase follow-up, not solved here" gap E1's own
`SignalRNotificationPusher` comment already named for its own broadcast
model — this step inherits it rather than re-solving it differently for
one channel and not the other.

**Unlike `INotificationPusher`, `IEmailSender`'s real implementation lives
in Infrastructure, not the API layer — and that's not an inconsistency,
it's the same placement rule correctly applied.** `SignalRNotificationPusher`
had to live in `Notifications.API` because it's inseparable from THIS
host's own ASP.NET Core pipeline (`IHubContext`, hub mapping). MailKit's
`SmtpClient` has no such dependency — it's generic outbound-network
plumbing, exactly like every `IEventPublisher` HTTP client already
registered in every other service's own Infrastructure project. Two
different transports, two different correct answers to "which layer owns
this," from the same one rule.

**Sending is deliberately best-effort, with no retry queue — a NAMED
tradeoff, not an oversight.** Every inter-SERVICE event in this system
(`SaleCompleted`, `StockLevelChanged`, …) goes through the outbox pattern
specifically because losing one silently would be wrong — C3/D1 built
real retry/redelivery machinery for that reason. An alert EMAIL is
different: the notification itself (the DB row, the SignalR push) already
happened and already succeeded independently of whether the SMTP relay
answers. `IngestStockLevelChangedCommandHandler` wraps the `SendAsync`
call in its own `try`/`catch`, logs a warning on failure, and returns the
same success response either way — an unreachable mail relay degrades
this ONE channel, not the ingestion pipeline underneath it. Building a
second outbox just for email would be solving a problem this system
doesn't have: nothing downstream is waiting on the email to have been
sent, the way Warehouse's stock decrement genuinely can't be skipped.

**Concepts introduced:**
- **Two consumers of one decision, reusing the exact same crossing-edge
  computation.** E1's `StockLevelSnapshot`-based "was it low before this
  event" logic didn't change at all; this step only adds a second `await`
  after the existing SignalR push, inside the same `if` branch.
- **Best-effort vs. reliable delivery as a considered choice per
  channel**, not a system-wide policy. The outbox pattern is for facts
  another SERVICE needs to eventually know; a plain try/catch is for a
  supplementary notification channel whose failure doesn't strand any
  other state.
- **No per-call "to" address, mirrored from `INotificationPusher` into a
  brand-new interface** — proof the "audience is a deployment concern,
  not a domain decision" idiom generalizes across transports, not just
  something SignalR needed.

**Verified with a 13-check runtime test — no live SQL Server needed
(SQLite, same as every other Application-layer test in this project), but
a REAL fake SMTP server this time (`netDumbster`, a scratchpad-only test
dependency, never added to the actual project) instead of a mocked
`IEmailSender` for the wire-format half:** the first half proves the
HANDLER's own decision — a brand-new item/location pair that arrives
already low emails once; staying low sends nothing further; leaving low
stock sends nothing; crossing into low stock a SECOND time emails again
(per-transition, not permanently suppressed, the same behavior E1's own
SignalR test already proved for the push side); and a normal,
never-low pair never emails at all. The second half proves
`SmtpEmailSender` ITSELF — not a fake standing in for it — actually
connects to a real SMTP listener and sends a correctly-addressed message:
the fake server receives exactly one email, with the right `From`
address, the right `Subject`, BOTH configured recipients on the envelope,
and the low-stock message text in the body; and that an unconfigured
recipient list logs a warning and returns cleanly rather than throwing or
silently attempting to connect anywhere.

**Run it locally (a local SMTP catcher instead of a real mail server —
smtp4dev, MailHog, or any SMTP-accepting relay on the configured host/port
works, since `Smtp:Host`/`Smtp:Port` in `appsettings.json` point at
`localhost:2525` by default):**
```bash
# any local SMTP catcher that listens on 2525, e.g.:
docker run --rm -it -p 2525:25 -p 5080:80 rnwood/smtp4dev

cd src/Services/Notifications/Notifications.API
dotnet run   # http://localhost:5298
# → trigger a LowStock event (Admin Panel: adjust an item's stock below
#   its reorder threshold) and check the catcher's inbox for the alert
```

## Business gap — Sale returns/refunds

**What it does:** closes the gap C1's own `SaleStatus` comment named
explicitly — a completed sale could never be reversed. A new `Returned`
status and `ReturnSaleCommand` let a cashier return a `Completed` sale;
POS fans a `SaleReturned` event out to the same three consumers
`SaleCompleted` already reaches (Warehouse, Reporting, Notifications),
each applying the reversal in its own way: Warehouse restocks the
quantity, Reporting stops counting the sale toward revenue, Notifications
pushes a second, independent "returned" toast.

**`SaleReturned` reuses `SaleCompletedMessage`'s exact shape — only the
`EventType` string and the downstream URL path change.** A return moves
the same `{ SaleId, LocationId, Lines[...] }` data as a completion; the
only thing that differs is which direction each consumer applies it, and
that's a routing decision each `IEventPublisher` already had to make
per-event-type anyway. Every producer-side publisher
(`WarehouseEventPublisher`, `ReportingEventPublisher`,
`NotificationsEventPublisher`, all in `POS.Infrastructure`) went from a
single hardcoded `if` guard to an `if`/`else if` on `eventType`, picking a
different downstream path — no new message class, no new outbox
machinery, and `OutboxDispatcher` itself needed zero changes: it already
resolves publishers by `ConsumerName`, not by event type.

**Warehouse's restock is `ApplySaleCommand`'s mirror image, sharing its
`StockAdjustmentStager` — but with its own idempotency table, not a
reused one.** `ApplySaleReturnCommand` stages a positive
`StockAdjustmentStager.Stage(...)` per line (`StockTransactionReason.Return`,
a new value distinct from `Adjustment` so the audit trail stays
filterable by reason) instead of `ApplySaleCommand`'s negative one, and
commits once at the end — the same all-or-nothing atomicity
`ApplySaleCommand` already relies on. The idempotency check is a
**separate** table, `ProcessedSaleReturnEvent`, not a second column
tacked onto the existing `ProcessedSaleEvent`: a given `SaleId`
legitimately appears in both tables at once — once when the original sale
decremented stock, once when its return restocked it — and a single
table keyed by `SaleId` couldn't dedupe those two distinct facts
independently. Notifications' `Notification.SourceSaleReturnId` (kept
separate from `SourceSaleId` for the identical reason) and the two
tables' own unique indexes are the same pattern, twice.

**Reporting doesn't get a second table either — a return is a mutation of
the existing `SaleRecord`, not a new fact alongside it.**
`IngestSaleReturnedCommand` finds the `SaleRecord` by `SaleId` and stamps
`ReturnedAtUtc`, rather than inserting a new row the way `SaleCompleted`
ingestion does — there's already exactly one row per sale to hold that
flag on. `GetSalesByDay` and `GetTopSellingItems` both now filter it out;
the second one has no `SaleLineRecord → SaleRecord` navigation property to
`Include` through (see that entity's own comment on why lines are split
out), so the exclusion is a plain subquery against `SaleRecords.SaleId`
instead.

**If `SaleReturned` is delivered before `SaleCompleted` has been ingested
yet, `IngestSaleReturnedCommandHandler` throws `NotFoundException` — and
that's the self-healing path, not a bug.** At-least-once delivery across
two *separate* outbox messages gives no ordering guarantee between them;
a `NotFoundException` becomes a failed HTTP response, which POS's
`OutboxDispatcher` already treats as retryable (`MaxAttempts = 5`) for
every other consumer. No new retry mechanism was needed — just reusing
the one that already existed correctly.

**Concepts introduced:**
- **Reusing one message shape for two semantically opposite events.**
  `EventType` alone (`SaleCompleted` vs `SaleReturned`) carries the
  distinction; the payload shape staying identical is what let every
  consumer's routing collapse to a single `if`/`else if` rather than a
  parallel set of return-flavored DTOs.
- **Independent dedup keys for facts that share a natural key.** The same
  `SaleId` needs to be "have I applied this sale" and "have I reversed
  this sale" *at the same time*, in the same row's neighborhood
  (`ProcessedSaleEvent`/`ProcessedSaleReturnEvent`,
  `SourceSaleId`/`SourceSaleReturnId`) — one column or table per question,
  never one shared between two.
- **A mutation-in-place read model vs. an append-only one**, in the same
  service. `SaleRecord` gets a nullable `ReturnedAtUtc` mutated after the
  fact (there's only ever one row per sale); Warehouse's `StockTransaction`
  ledger, by contrast, never mutates a past row for a return — it appends
  a new one with `Reason = Return`. Which shape fits depends on whether
  the entity already models "the current state of one thing" or "an
  immutable history of events."

**Verified with a 30-check runtime test — four SQLite databases, one per
service, dispatched through actual MediatR pipelines (validators and all),
not calling handlers directly — plus a Playwright pass against the built
Angular app:** the backend test proves `ApplySaleCommand` then
`ApplySaleReturnCommand` round-trips a stock level back to its original
quantity, with a real `StockTransaction` row stamped `Reason = Return` and
`Reference = "Return of Sale {id}"`; that redelivering either command a
second time is a no-op (`AlreadyProcessed = true`) with no double
decrement/restock; that `ProcessedSaleEvent` and `ProcessedSaleReturnEvent`
both hold exactly one row for the *same* `SaleId` at the same time, proving
the separate-table decision actually avoids the collision a shared one
would hit; that `GetSalesByDay`/`GetTopSellingItems` include a sale's
revenue before its return and exclude it after; that an unknown `SaleId`
delivered to `IngestSaleReturnedCommand` throws `NotFoundException`
rather than silently no-op'ing; that the same `SaleId` produces two
independently-dedup'd Notifications (`SaleCompleted` and `SaleReturned`);
and that `ReturnSaleCommand` transitions a `Completed` sale to `Returned`,
fans exactly 3 pending `OutboxDelivery` rows out
(Warehouse/Reporting/Notifications), and rejects returning a sale that's
already `Returned` or still `InProgress`. The Playwright pass mocks the
gateway's REST routes as usual, drives the register component straight to
its `Completed` receipt state, clicks "Return sale," and confirms the
button disappears, a "this sale was returned" note appears, and the real
`POST .../return` call actually fired.

**Run it:** no new terminal — `ReturnSaleCommand` is a new endpoint on the
existing POS.API (`POST /api/v1/Sales/{id}/return`, gatewayed as
`POST /Pos/Sales/{id}/return`); the register screen's receipt card shows a
"Return sale" button once a sale is `Completed`.

## Business gap — Location-to-location stock transfer

**What it does:** closes the second gap the returns/refunds work above
surfaced while grepping for it — `StockTransactionReason.TransferIn` and
`TransferOut` had been declared since B1 but no command anywhere ever
constructed a `StockTransaction` with either value. `TransferStockCommand`
is their first (and only) caller: move a quantity of one item from one
Warehouse location to another, in one atomic operation, through a new
Admin Panel form.

**One command, two calls to the exact same `StockAdjustmentStager.Stage(...)`
every other stock command already uses — a negative one at the source,
a positive one at the destination — committed once.** No new staging
logic was needed: `AdjustStockCommand` already proved `Stage(...)` handles
the negative-balance guard, the `StockTransaction` audit row, and the
`StockLevelChanged` outbox event for a single location; a transfer is
just that, called twice with `TransferOut`/`TransferIn` instead of
`Adjustment`, before one shared `SaveChangesAsync()`. The destination call
passes `createIfMissing: true` (the same flag `ReceiveStockCommandHandler`
already needed) — a transfer can legitimately be the first stock an item
has ever had at that location.

**Atomicity here is free, not hand-rolled — the same reasoning
`ApplySaleCommand`'s own comment already gives for a multi-line sale.**
`Stage()` only ever stages; it never calls `SaveChangesAsync` itself. The
source is staged FIRST: if it doesn't have enough stock,
`InsufficientStockException` throws right there, before the destination
is ever touched, and nothing has been saved yet — there is no
half-transfer state where stock vanished from the source without landing
at the destination, and no compensating rollback needed to prevent one.
An unknown source `(item, location)` pair throws `NotFoundException` the
same way `AdjustStockCommand` already does, for the same reason: a
transfer's source, unlike a transfer's destination, has to already have a
balance to move.

**A same-location "transfer" is rejected by validation, not left to net
out to a no-op.** `FromLocationId == ToLocationId` would still stage a
`-Q` and a `+Q` `StockTransaction` at the same `(item, location)` pair —
netting to zero real stock change but writing two meaningless audit rows
— so `TransferStockCommandValidator` rejects it outright with a
`ValidationException`, the same "reject malformed input before the
handler ever runs" job every other command's validator already does.

**Concepts introduced:**
- **A years-old dead enum value finally getting its first real caller.**
  `TransferIn`/`TransferOut` are proof that a domain model can carry
  intent ahead of the feature that uses it without that being a design
  smell — the enum's own comment (added alongside this step) now says so
  explicitly, the same "name the gap so a future step finds it named"
  discipline C1's `SaleStatus` comment already modeled for returns/refunds.
- **Free atomicity from deferred `SaveChangesAsync`, applied to a
  TWO-location operation** rather than the multi-LINE operation
  `ApplySaleCommand` originally demonstrated it for — same mechanism,
  a different shape of "more than one thing has to succeed together."

**Verified with a 14-check runtime test, extending the same SQLite
Warehouse database and MediatR pipeline the returns/refunds test above
already set up — plus a Playwright pass against the built Angular app:**
the backend test proves a normal transfer decrements the source to the
expected quantity, creates the destination `StockLevel` row with the
transferred quantity, and records a `TransferOut`/`TransferIn` pair of
`StockTransaction` rows with the right signs; that two `StockLevelChanged`
events are staged (one per location) and each fans out to both Reporting
and Notifications (4 deliveries total); that a same-location transfer is
rejected by validation before touching the database; that transferring an
item with no stock at all at the source throws `NotFoundException` and
leaves NO destination row behind (proving the "source fails first, before
the destination is touched" ordering actually holds); and that
transferring MORE than the source has throws `InsufficientStockException`
and leaves both locations' quantities completely unchanged. The Playwright
pass mocks the gateway's REST routes, opens the Admin Panel, selects an
item with existing stock, submits the new "Transfer stock" form, and
confirms the real `POST .../Stock/transfer` call fired with the right
body and that the stock-on-hand table refreshes to show both the
destination's new row and the source's reduced quantity.

**Run it:** no new terminal — `TransferStockCommand` is a new endpoint on
the existing Warehouse.API (`POST /api/v1/Stock/transfer`, gatewayed as
`POST /Warehouse/Stock/transfer`); the Admin Panel's item detail view
shows a "Transfer stock" form alongside the existing "Receive stock"/
"Adjust stock" ones once an item with at least one stock level is
selected.

## F1 — Performance (Redis caching, pagination, compression, health checks)

**What it does:** four independent performance concerns, each solved once
at its own natural, highest-leverage point rather than mechanically
everywhere it could theoretically apply — real dependency health checks
on every service, response compression at the one place ALL browser
traffic passes through, real pagination on the two rawest unbounded list
endpoints, and a Redis-backed cache for Warehouse's read-heavy,
effectively-immutable master data.

**Health checks: a real DB-connectivity probe on all 5 services, not just
the gateway's existing bare liveness check.** A3 already gave the
gateway a `/hc` endpoint, but a deliberately bare one — no downstream
service had one of its own at all, and the gateway's own check never
verified anything past "the process is running." Every service now calls
`AddHealthChecks().AddDbContextCheck<TContext>()` + `MapHealthChecks("/hc")`
in its own `Program.cs` — the exact same two-line idiom five times, so
each service's own `/hc` genuinely answers "can this service reach its
database," not just "is Kestrel listening." None of these are routed
through Ocelot — same "service-to-service/infra tooling, not a
browser-facing feature" reasoning `EventsController`/`StockEventsController`
already established: a real deployment's orchestrator (docker-compose's
own `healthcheck:` directive, F4) hits each container's `/hc` directly,
the same way it would never ask the gateway "is Identity healthy?" on
Identity's behalf. The gateway's own `/hc` stays a bare liveness check on
purpose — aggregating five downstream health checks INTO the gateway
would make the gateway's own health depend on every service it fronts,
which is exactly backwards for a reverse proxy that should stay reporting
healthy even while one backend is degraded.

**Compression: one `AddResponseCompression` call in the gateway covers
every browser-facing response in the system.** Every REST call Angular
makes goes through Ocelot's upstream routes — the one carve-out
(Notifications' SignalR hub, E1) connects directly and was never
gatewayed at all, so it was never in scope here either. Rather than add
compression middleware to five separate downstream `Program.cs` files,
one `AddResponseCompression`/`UseResponseCompression` pair in
`Gateway.Ocelot`, placed as the FIRST middleware (before `UseOcelot()`
wraps the response stream itself), compresses every proxied JSON payload
on its way out. Brotli first, gzip as the fallback for clients that don't
advertise it — negotiated automatically via the request's own
`Accept-Encoding` header, exactly like a browser already does without
Angular needing to know compression exists at all.

**Pagination: a new shared `Common.Pagination` building block, and real
paging on the two rawest, most obviously-unbounded list endpoints.**
`PagedResult<T>` lives in `BuildingBlocks/Common.Pagination` — shared
across services the same way `Common.Exceptions` already is, and for the
identical reason: it's not domain data (no service's own `EntityBase`/
`DbContext` lives there, so the "no shared domain assemblies" rule this
project otherwise follows everywhere else stays intact), it's a generic
wire-format envelope, the same category as `Common.ExceptionHandling`'s
`ProblemDetails` shape. `GetAllItemsQuery` (Warehouse's item catalog) and
`GetSalesQuery` (Reporting's raw sale-record dump) both moved from an
unbounded `IEnumerable<T>` to `PagedResult<T>`, each repository gaining a
`GetPaged(page, pageSize)` that returns the total row count alongside the
page (one `CountAsync()` plus one `Skip().Take()`, not one clever query
trying to do both at once — EF Core has no way to project "the page" and
"the total" out of a single `SELECT` without materializing every row
first). `GetTopSellingItemsQuery`/`GetRecentNotificationsQuery` keep
their existing flat "top N" shape rather than being converted too — a
ranked top-N list and a raw table dump answer different questions, and
the former was never the unbounded-growth problem this step exists to
solve.

**The Angular "parent item" picker exposed the real tension in paginating
a list that's ALSO used as a lookup — solved by giving it its own,
separate, unpaged-ish request rather than reusing the paged one.**
`items-admin.component.ts`'s create-item form offers every existing item
as a possible parent via the same `items` signal the browsable list
below used to populate from — once that signal only holds one page,
reusing it would mean the picker only ever offers whichever items happen
to be on the CURRENTLY VIEWED page. The fix is a second signal,
`parentCandidates`, populated by its own call to the identical
`GetAllItemsQuery` endpoint with `pageSize=100` (the max
`GetAllItemsQueryValidator` allows) — a real, named limitation for a
catalog bigger than 100 items (the picker would then miss some), left as
a future "replace with a searchable autocomplete" improvement rather than
solved here, the same "name the gap explicitly" discipline this project
uses throughout rather than silently capping something and moving on.

**Redis caching: `MasterDataCache`, a cache-aside helper extracted the
moment a THIRD caller needed the identical shape — same reasoning
`StockAdjustmentStager` was extracted for a SECOND.** Categories,
Locations, and UnitsOfMeasure are all seeded reference data with no
mutating command anywhere in this system (confirmed by grepping for one
before writing a single line of caching code) — read on nearly every
Admin Panel and POS screen load, changed never. `MasterDataCache` wraps
`IDistributedCache` (a framework ABSTRACTION from
`Microsoft.Extensions.Caching.Abstractions`, not a concrete Infrastructure
detail — the same category `ILogger<T>` already falls into, which
Application-layer handlers elsewhere in this project, like Notifications'
`IngestStockLevelChangedCommandHandler`, already inject directly) behind
a `GetOrSetAsync<T>(key, factory, ct)` cache-aside shape: check Redis,
deserialize on a hit, fall through to the real repository and repopulate
on a miss. The three `GetCategories`/`GetLocations`/`GetUnitsOfMeasure`
query handlers each shrink to a five-line cache-key-plus-factory call. The
concrete Redis implementation
(`AddStackExchangeRedisCache`, bound from a plain `ConnectionStrings:Redis`
value — no dedicated options class needed since there's nothing else to
configure) is registered in `Warehouse.Infrastructure`, same as every
other concrete Infrastructure detail; `MasterDataCache` itself, like
`StockAdjustmentStager`, has no idea Redis exists. The 10-minute TTL
exists purely as a safety net, not because this data is expected to
change — if a future step ever adds the first command that mutates a
Category/Location/UnitOfMeasure, THAT step will need to add cache
invalidation here too; until then, a page that's up to 10 minutes stale
is indistinguishable from a fresh one, because the underlying data never
actually moves.

**Concepts introduced:**
- **A shared BuildingBlocks project for a wire-format shape, not domain
  data.** `Common.Pagination` joins `Common.Exceptions`/
  `Common.Security`/`Common.ExceptionHandling` as the fourth building
  block every service is allowed to reference directly — the dividing
  line was never "nothing shared," it was always "nothing DOMAIN shared,"
  and a generic paged-response envelope was always on the allowed side of
  that line, same as `ProblemDetails` already was.
- **Extracting a cache-aside helper the moment a THIRD caller needs the
  identical shape** — the same "extract on the Nth real need, not
  preemptively" discipline `StockAdjustmentStager`/`EffectivePriceResolver`
  already modeled, applied to a caching concern instead of a stock or
  pricing one.
- **A TTL as a pure safety net, with no invalidation logic, justified by
  an actual grep proving no mutation path exists** — not "cache
  everything and hope," a specific, checked precondition (`Category`/
  `Location`/`UnitOfMeasure` are genuinely immutable at runtime today)
  that the next person to violate it (by adding a mutating command) is
  now on notice to also revisit.
- **Two independent scoped-vs-unpaged-view needs on the same underlying
  list**, solved with two separate signals/requests rather than
  contorting one paged response to serve both — the same "don't force one
  shape to answer two different questions" instinct that produced
  `ProcessedSaleEvent`/`ProcessedSaleReturnEvent` as separate tables
  earlier in this project.

**Verified with a 24-check runtime test — real infrastructure throughout,
not mocks: a real Kestrel HTTP listener for the health-check mechanism (a
genuinely-reachable SQLite connection proving `/hc` returns 200 Healthy,
and a genuinely-unreachable one proving it returns 503 Unhealthy), real
MediatR-dispatched pagination proving page boundaries/counts/ordering are
correct and out-of-range `Page`/`PageSize` values are rejected by
validation, and a real local `redis-server` instance (not a mocked
`IDistributedCache`) proving a cache miss reaches the repository exactly
once, a cache hit does NOT reach it again, and an evicted/expired entry
transparently falls back and repopulates — plus a Playwright pass
confirming the Admin Panel's item list actually paginates (a second page
of results, no row overlap with the first) and that the "parent item"
picker keeps offering every item regardless of which page the browsable
list is currently showing.** Response compression was verified directly
against a running gateway process with `curl -H "Accept-Encoding: gzip"`
(and `br`), confirming `Content-Encoding`/`Vary` headers appear only when
the client actually advertises support for them.

**Run it locally:**
```bash
# Any local Redis works — the default connection string points at
# localhost:6379.
redis-server

# Every service's own /hc:
curl http://localhost:5218/hc   # Identity.API
curl http://localhost:5238/hc   # Warehouse.API
curl http://localhost:5258/hc   # POS.API
curl http://localhost:5278/hc   # Reporting.API
curl http://localhost:5298/hc   # Notifications.API

# Compression, through the gateway:
curl -sD - -o /dev/null -H "Accept-Encoding: gzip" http://localhost:5058/Warehouse/Items?page=1

# Pagination — the Admin Panel's item list now shows a paginator; or call
# either paged endpoint directly:
curl http://localhost:5058/Warehouse/Items?page=2&pageSize=10
curl http://localhost:5278/Reporting/ReadModels/sales?page=1&pageSize=20
```

## F2 — Security hardening (role-based policies, gateway rate limiting, input validation review)

**What it does:** closes a real, self-flagged privilege-escalation hole in
registration; adds role-based `[Authorize(Roles = ...)]` restrictions to
every mutation endpoint (Warehouse catalog/stock, POS sales) and every
report endpoint, without touching a single service-to-service call path;
extends the gateway's existing rate limiter to the one other anonymous,
abuse-prone route; adds three safe-default security response headers; and
fixes a handful of concrete input-validation gaps a targeted review turned
up.

**The standout finding: anyone, with no token at all, could register as
Admin.** `RegisterCommand`'s own comment (A1) already named
this explicitly — "Only an existing Admin should be able to create
Admin/Manager/WarehouseStaff accounts — that authorization rule is
enforced at the controller (F2), not here" — and until this step, nothing
actually enforced it. `AuthController.Register` now overwrites
`command.Role` to `Cashier` unconditionally, no matter what the request
body claims, the identical "context is authoritative over the body" idiom
`SalesController.Start` already uses for `CashierUserId`. The other half
of the fix is a NEW endpoint, `POST /Auth/create-user`
(`[Authorize(Roles = Admin)]`), which reuses `RegisterCommand` as-is and
trusts its submitted `Role` — safe specifically because only a caller who
already holds a verified Admin token can reach it at all. No Angular UI
was built for it (the client never had a public registration screen to
begin with — A4 built login only); the backend capability exists and is
verified end-to-end, calling it a curl/Swagger operation for now is a
named, deliberate scope cut, not an oversight.

**Every OTHER `[Authorize(Roles = ...)]` addition had to answer one
question first: does anything call this endpoint service-to-service?**
Warehouse's `ServiceAuthHandler`/POS's own copy each mint a token with
exactly ONE claim — `ClaimTypes.Name` — and nothing else. No `Role` claim
at all. `[Authorize(Roles = "...")]` on an endpoint POS's own
`WarehouseCatalogClient` (C2) or Warehouse's `StockAdjustmentStager`
outbox events call would 403 every checkout in the system, silently,
the moment this shipped. So the actual work here was mapping every
controller action in Warehouse.API/POS.API/Reporting.API against "is this
ever reached by anything other than a human's browser request through
the gateway" BEFORE deciding where a Roles restriction is even safe to
add:
- **Warehouse's `ItemsController`/`StockController`** keep their
  class-level bare `[Authorize]` and get a SECOND, per-action
  `[Authorize(Roles = "Admin,Manager,WarehouseStaff")]` layer added only
  to the mutation actions (`Create`, `AddBarcode`, `AddUnit`,
  `UpdatePrice`, `CreatePromotion`, `CancelPromotion`, `Receive`,
  `Adjust`, `Transfer`) — both `[Authorize]` attributes on the same
  target combine (a request must satisfy ALL of them), so these actions
  now require "signed in" AND "one of these roles." Every read action —
  including `ResolveBarcode` and `GetByItem` (Stock), the two endpoints
  POS's own service-to-service catalog client actually calls — is left
  completely untouched.
- **POS's `SalesController`** gets its Roles restriction at the
  CONTROLLER level instead (`Admin,Manager,Cashier`, covering even the
  read-only `GetById`) — safe here specifically because nothing calls
  INTO `SalesController` service-to-service; POS only ever calls OUT.
- **Reporting's `ReportsController`/`ReadModelsController`** get the same
  controller-level treatment (`Admin,Manager`) for the identical reason —
  `EventsController` is a SEPARATE, still-bare controller that POS's/
  Warehouse's own outbox dispatchers actually push events to, and was
  never touched.
- **Notifications** gets no role restriction at all — a low-stock or
  sale alert is for everyone regardless of role, the same "broadcast to
  all, no per-user targeting" model E1 already established.

**A new `Common.Security.RoleNames`, alongside Identity.Domain's own
copy — not a violation of "no shared domain assemblies," a second
instance of the exact tradeoff `Common.Pagination` already made.**
`[Authorize(Roles = ...)]` arguments must be compile-time constants, and
Warehouse/POS/Reporting have no reference to Identity.Domain at all (by
design) — so a shared `const string` set living in Common.Security (which
every service already references for `AddJwtAuthentication`) is the
typo-proof alternative to hand-copying the literal strings `"Admin"`,
`"Manager"`, etc. into a dozen attribute arguments across three services.
Identity's OWN copy in `Identity.Domain.Entities.RoleNames` stays the real
source of truth for the seeded `Role` table rows; the two are duplicated
on purpose, the same way `EntityBase` is duplicated per service, for the
same underlying reason.

**Angular gets a second guard, mirroring the backend's role matrix
one-for-one — a UX guard, not a security boundary, same as `authGuard`
already was.** `roleGuard(allowedRoles)` sits alongside `authGuard` on
`/admin`, `/pos`, and `/reports`; without it, a signed-in Cashier clicking
"Admin" in the toolbar would land on a real page that immediately fails
every API call with 403, instead of being redirected before that ever
renders. The toolbar's own three nav links are now conditionally rendered
by the identical role check (`canSeeAdmin()`/`canSeePos()`/`canSeeReports()`
in `app.ts`) — a Cashier never even sees a door that leads to a 403.

**Gateway rate limiting: the login-only limiter now also covers
`/register`, partitioned independently.** It was the only other
anonymous, abuse-prone POST route in the system — every other route
needs a token an attacker doesn't have yet — and it had zero throttling
before this. The partition key became `"route:ip"` instead of just `"ip"`,
so hammering `/login` doesn't also burn a legitimate user's separate
`/register` budget; the two anonymous endpoints are throttled
independently of each other, same `PermitLimit = 5` / `Window = 30s` as
before. Every other route stays exactly as unlimited as the rate
limiter's own comment already said it was designed to be — this still
isn't a general-purpose throttle.

**Three response headers, set once at the gateway via `Response.OnStarting`
— the same "one point, every browser-facing response passes through it"
reasoning F1's compression already used.** `X-Content-Type-Options: nosniff`,
`X-Frame-Options: DENY`, `Referrer-Policy: no-referrer` — all three are
safe, context-free defaults. Deliberately NOT included: HSTS (meaningless
until HTTPS is actually enforced, which local `dotnet run` doesn't do) and
a CSP (a real one needs to know every script/style/connect-src the Angular
app legitimately uses, which is a genuine per-app policy decision this
project hasn't made yet — a wrong CSP is worse than none, so this is a
named gap, not a safe default to just add).

**Input validation: a handful of concrete gaps a targeted review
surfaced, not a blanket rewrite.** `LoginCommandValidator` had no
`MaximumLength` on `UserName`/`Password` at all (added, matching
`RegisterCommandValidator`'s existing `UserName` cap) — this isn't
re-checking password complexity (that's `RegisterCommandValidator`'s job,
once, at account creation), it's rejecting an oversized request body
before it reaches PBKDF2 hashing. `RegisterCommandValidator` gained an
`Email` length cap and a `Password` upper bound for the same reason.
`AddItemUnitCommandValidator.ConversionFactor` and
`CreatePromotionCommandValidator.DiscountValue` (the `FixedAmountOff`
case) both had a lower bound but no upper one — neither is an actual
security hole (`EffectivePriceResolver` already floors a discounted price
at zero, so an absurd flat discount was already harmless in practice),
but both now reject an obvious data-entry mistake or overflow-style input
at the validator instead of relying solely on that apply-time floor.

**What's still a named gap, not solved here:** the JWT secret
(`SharedSettings/jwt.settings.json`) and every service's SQL Server
credentials remain plaintext strings in checked-in `appsettings.json`
files — real secrets-manager integration (user-secrets locally, Key Vault
or equivalent in a real deployment) is a disproportionate detour for a
single-tenant local learning-lab and stays exactly the "F2's to actually
confront" pattern this project already flagged for itself, still
unconfronted. A meaningful Content-Security-Policy is the other
deliberately-skipped item, for the reason given above.

**Verified with a 20-check runtime test — four real ASP.NET Core hosts
(Identity/Warehouse/POS/Reporting), each with REAL JWT authentication
wired up (`AddJwtAuthentication` against an in-memory `JwtSettings`,
tokens minted by the same `JwtTokenFactory` every service already uses)
and REAL controllers (`AddApplicationPart`), not MediatR calls that would
skip the ASP.NET Core authorization pipeline entirely — plus a 16-check
Playwright pass on the Angular guard/nav-visibility changes:** the
backend test proves a Cashier token gets a genuine 403 on Warehouse's
`POST /Stock/adjust` and `POST /Items` while a WarehouseStaff token
succeeds (and the stock adjustment actually applies); a WarehouseStaff
token gets 403 on POS's `POST /Sales` while a Cashier token succeeds; a
Cashier token gets 403 on Reporting's `GET /Reports/sales-by-day` while
an Admin token succeeds; and — the check that matters most — a
service-to-service token with NO Role claim at all still succeeds on
Warehouse's `GET /Stock/{itemId}` and `GET /Items/barcodes/{barcode}` and
on Reporting's `POST /Events/sale-completed`, proving none of this broke
the actual inter-service call paths the whole system depends on. The
Identity test proves an anonymous `/register` submitting `role: "Admin"`
comes back as `Cashier` regardless, that `/create-user` rejects both no
token (401) and a non-Admin token (403), and that a real Admin token
succeeds and the created account gets the role it actually asked for. The
Playwright pass confirms a Cashier only ever sees the POS nav link and is
redirected away from `/admin`/`/reports` on direct navigation, a
WarehouseStaff sees the mirror image, and an Admin sees and can reach all
three. Rate limiting and security headers were verified directly against
a running gateway process: 6 rapid `POST /register` calls → the first 5
proxy through (502, no live Identity.API in this sandbox — expected) and
the 6th returns `429`; an immediately-following `/login` attempt still
proxies through unblocked, proving the two anonymous routes have
independent budgets; and `curl`'s response headers show
`X-Content-Type-Options`/`X-Frame-Options`/`Referrer-Policy` present on
every response.

**Run it locally:**
```bash
# Sign in as the seeded admin (Admin@12345) to get a token, then:

# The fix — role in the body is ignored for public signup:
curl -X POST http://localhost:5058/Identity/Auth/register \
  -H "Content-Type: application/json" \
  -d '{"userName":"newuser","email":"newuser@example.com","password":"Password123","role":"Admin"}'
# → response.role is "Cashier" regardless

# The Admin-only counterpart — creates a Manager/WarehouseStaff/Admin account:
curl -X POST http://localhost:5058/Identity/Auth/create-user \
  -H "Authorization: Bearer <admin token>" -H "Content-Type: application/json" \
  -d '{"userName":"newmanager","email":"newmanager@example.com","password":"Password123","role":"Manager"}'

# Rate limiting + security headers, against the gateway directly:
for i in 1 2 3 4 5 6; do curl -s -o /dev/null -w "%{http_code}\n" -X POST http://localhost:5058/Identity/Auth/register -d '{}'; done
curl -sD - -o /dev/null http://localhost:5058/hc   # X-Content-Type-Options / X-Frame-Options / Referrer-Policy

# Role enforcement, in the Angular app: sign in as a Cashier-role account
# and note the Admin/Reports nav links are simply gone; try navigating to
# /admin directly and you're bounced back to /login.
```

## F3 — Localization (English/Arabic, RTL)

**What it does:** every one of the 5 backend services now negotiates
English/Arabic per request from the `Accept-Language` header, and the
Angular client gets a real language switcher — English/Arabic UI text on
all 5 feature screens plus the toolbar, `dir="rtl"` layout mirroring, and
the same `Accept-Language` header sent on every API call so the two halves
agree on which language a given request is in.

**The backend's biggest win came almost for free: FluentValidation already
ships its own Arabic translations.** A survey of every validator in the
codebase (31 files across all 5 services) found only 6 custom
`.WithMessage()` call sites, total — the other ~150 validation rules
(`NotEmpty()`, `MaximumLength()`, `GreaterThan()`, etc.) rely entirely on
FluentValidation's built-in `LanguageManager`, which already has a
complete Arabic translation table and picks it automatically from
`CultureInfo.CurrentUICulture`. So the actual backend work wasn't "write
Arabic strings for every validator" — it was "get `CurrentUICulture` set
correctly per request," and FluentValidation's own messages started
coming back in Arabic with zero additional code.

**Two new BuildingBlocks projects, split along the exact seam
`Common.Exceptions`/`Common.ExceptionHandling` already established.**
`Common.Localization` holds `Messages.resx`/`Messages.ar.resx` plus a
hand-written `Messages` static class (a plain `ResourceManager` lookup
keyed on `CultureInfo.CurrentUICulture` — no `IStringLocalizer`/DI needed,
so a bare exception constructor can call it) — no ASP.NET Core dependency,
so `Common.Exceptions` and the two Application-layer projects with custom
`.WithMessage()` calls (Identity, Warehouse) can reference it without
pulling the framework into layers that must stay framework-agnostic.
`Common.RequestCulture` holds the actual ASP.NET Core middleware wiring
(`AddSharedRequestLocalization`/`UseSharedRequestLocalization`, restricting
`SupportedCultures` to `["en", "ar"]`) and is referenced only by the 5
`*.API` projects — the same "only Web API projects pull in the framework
reference" split `Common.ExceptionHandling` already used.

**Two message shapes got moved into `Common.Localization.Messages`, not
all three of `Common.Exceptions`' exception types.** `NotFoundException`'s
internal fixed template (`Entity "{0}" ({1}) was not found.`) and
`GlobalExceptionHandler`'s hardcoded 500-level literal both became resx
entries — both are centralized in exactly one place each, so localizing
them cost one line change apiece. `ConflictException`/`UnauthorizedException`
are deliberately NOT touched: they're pure pass-through types where every
call site across the codebase supplies its own full English string
inline, and there's no single choke point to intercept — translating
those would mean editing every throw site individually, which is exactly
the kind of open-ended, disproportionate-to-this-phase work the project's
own scoping discipline exists to name rather than silently attempt. Same
reasoning for `ProblemDetails.Title`, which stays `exception.GetType().Name`
(the CLR type name) regardless of culture — it was never meant to be
human-facing prose to begin with.

**The 6 existing custom `.WithMessage()` calls (3 in
`RegisterCommandValidator`'s password-complexity rules, 1 in
`TransferStockCommandValidator`, 2 in `CreatePromotionCommandValidator`)
now call `Common.Localization.Messages` properties instead of hardcoded
strings**, using the `Func<T, string>` overload (`.WithMessage(_ =>
Messages.X)`) rather than the plain-string overload — the string overload
would capture whatever culture was active when the validator was
*constructed*, and while every validator here is resolved per-request
through DI anyway, the `Func` form removes that fragility outright rather
than relying on a lifetime assumption holding.

**The Angular half: a real `i18next` instance (the library the roadmap
named), not a template-only stub.** `I18nService` fetches
`public/i18n/en.json`/`ar.json` once at startup (via a `provideAppInitializer`,
so nothing renders before translations are ready), wraps `i18next.t()`,
and is the only thing in the app that touches the library directly — the
same "one gatekeeper service" shape `AuthService` already uses for the
token. A standalone `TranslatePipe` (`{{ 'namespace.key' | translate }}`)
is used across app.html (toolbar) and all 5 feature templates:
login, admin-shell, pos-register, reports-dashboard, items-admin — **157
leaf translation keys total, structurally verified identical between
`en.json` and `ar.json`** (a Python key-diff found zero keys present in
only one file). A new `languageInterceptor` attaches the current UI
language as `Accept-Language` on every outgoing HTTP call, so a Cashier
who's switched to Arabic gets Arabic FluentValidation/exception messages
back from the backend too, not just Arabic static UI chrome.

**Switching language triggers a full page reload, on purpose — not a
half-working live re-render.** Angular Material's CDK `Directionality`
reads the `dir` attribute once, at each component's construction; flipping
`dir` at runtime wouldn't re-flow already-built `mat-form-field`/`mat-menu`
components to RTL. `I18nService.switchLanguage()` persists the choice to
`localStorage` (`warehousepos.lang`, the same pattern `AuthService` uses
for the auth token) and reloads; `main.ts` reads that stored value and
sets `dir`/`lang` on `<html>` *before* `bootstrapApplication` ever runs,
so there's no flash of LTR content on an Arabic-preferring reload. The 5
hardcoded LTR-only CSS declarations the initial survey found (`text-align:
left` on 3 tables, `margin-right` on 2 elements) were all converted to
logical properties (`text-align: start`, `margin-inline-end`) so they
mirror correctly under `dir="rtl"` without any RTL-specific override rules
needed.

**What's still a named gap, not solved here:** `ConflictException`/
`UnauthorizedException` call-site messages and `ProblemDetails.Title`
stay English-only, for the reasons above. Angular Material's own built-in
strings (the paginator's "Items per page"/"of" on the items-admin list)
are not localized — that needs a `MatPaginatorIntl` override, which is a
self-contained follow-up, not done here. Toast/notification strings built
dynamically in component `.ts` files (e.g. `"Sale cancelled."`,
`"Price updated to ..."`) are also untranslated — only template-authored
static copy went through the `translate` pipe, per this phase's own scope
line (`.ts` business logic wasn't touched beyond adding `TranslatePipe` to
each component's `imports` array).

**Verified with an 11-check runtime test (a real ASP.NET Core host,
SQLite, real FluentValidation/MediatR pipeline, sending `Accept-Language:
en` and `ar` against the same endpoints) plus two Playwright passes:**
the backend test proves a missing item's 404 detail switches from
`Entity "Item" (999999) was not found.` to the Arabic resx string; that
FluentValidation's OWN built-in `NotEmpty` message on an empty `Sku`
comes back as `'Sku' must not be empty.` vs. `'Sku' لا يجب أن يكون
فارغاً.` with zero custom `.WithMessage()` involved anywhere in that path;
and that the custom `CreatePromotionCommandValidator` message switches
between its English and Arabic `Common.Localization.Messages` strings.
The first Playwright pass confirms the language switcher flips
`dir`/`lang` on `<html>`, translates the login screen and toolbar, shows
Arabic `mat-error` validation text, and survives a plain page reload
(`localStorage` persistence). The second walks the Admin/POS/Reports
screens in both languages and confirms the translated headings/labels
render and — checked via `document.documentElement.scrollWidth` — that
switching to `dir="rtl"` introduces no horizontal overflow on any of the
three pages.

**Run it locally:**
```bash
# Backend — FluentValidation's built-in Arabic messages, zero custom code:
curl -X POST http://localhost:5058/Warehouse/Items \
  -H "Content-Type: application/json" -H "Accept-Language: ar" \
  -H "Authorization: Bearer <admin/manager/warehousestaff token>" \
  -d '{"sku":"","name":"","unitPrice":1,"categoryId":1,"baseUnitOfMeasureId":1,"barcode":"","barcodeType":0}'
# → errors.Sku[0] comes back in Arabic

# Backend — the same request with Accept-Language: en for comparison:
curl -X POST http://localhost:5058/Warehouse/Items \
  -H "Content-Type: application/json" -H "Accept-Language: en" \
  -H "Authorization: Bearer <admin/manager/warehousestaff token>" \
  -d '{"sku":"","name":"","unitPrice":1,"categoryId":1,"baseUnitOfMeasureId":1,"barcode":"","barcodeType":0}'

# Angular: sign in, click the "EN" button in the toolbar (top right),
# choose "العربية" — the page reloads in Arabic with dir="rtl". Every
# subsequent API call the app makes also carries Accept-Language: ar.
```
