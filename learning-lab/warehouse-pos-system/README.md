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
- [ ] C5 — Selling price history + promotions (POS pricing rules)

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
  on Warehouse's side, not just a status flip in POS — and doesn't exist
  yet. The enum's own comment says so explicitly, so a future "add
  returns" step finds a clear, named gap instead of ambiguous room to
  misuse `Cancelled` for something it was never designed to mean.

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
