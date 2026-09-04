# BookingSystem

A resource-booking system: users book time slots (e.g. rooms) for a given date, admins manage resources and their slots, and everyone watching a resource's schedule sees bookings appear live. ASP.NET Core (Clean Architecture) backend with SQL Server + EF Core, Angular frontend, JWT auth, SignalR for real-time updates.

## Running locally

**Backend** — `src/BookingSystem.Api`

```bash
dotnet run --project src/BookingSystem.Api
```

Requires user secrets before first run (never put these in `appsettings.json`):

```bash
dotnet user-secrets set "Jwt:Key" "<a random string, at least 32 bytes>" --project src/BookingSystem.Api
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "<your SQL Server connection string>" --project src/BookingSystem.Api
```

Migrations and role seeding run automatically at startup.

**Frontend** — `src/BookingSystem.Web`

```bash
cd src/BookingSystem.Web
npm install
npm start
```

Serves on `http://localhost:4200` and proxies to the API.

**Tests**

```bash
dotnet test tests/BookingSystem.Tests
```

Requires **Docker running** (`docker version` should succeed) — the integration tests under `Integration/` use `Testcontainers.MsSql`, which pulls `mcr.microsoft.com/mssql/server:2022-latest` and spins up a real, disposable SQL Server container automatically. No manual image pull or connection-string setup needed; the first run just takes longer (a couple of minutes) while the image downloads.

## Concurrency: how double-booking is prevented

The same slot must never be booked twice for the same date, even under concurrent requests. This is enforced with a **unique index in the database** — `(SlotId, Date)` on the `Bookings` table — rather than pessimistic locking (`SELECT ... WITH (UPDLOCK, HOLDLOCK)`) or optimistic concurrency via a `RowVersion`/`[Timestamp]` column.

Why:

- The database engine itself guarantees atomicity: the uniqueness check happens through the storage engine's own index-page locking at insert time, so it's atomic regardless of transaction isolation level or how many requests arrive concurrently.
- It's not incidental behavior of a database default — the index was added deliberately, specifically to reject duplicates, not a side effect that happens to work.
- There's no "check if free, then insert" step. `BookingService.CreateAsync` inserts immediately; if a row for that `(SlotId, Date)` already exists, SQL Server rejects the second insert with a constraint violation (`SqlException` 2627/2601), which is caught and turned into `Result.Conflict` → HTTP `409`. A check-then-insert pattern would itself be racy, since two concurrent requests could both pass the check before either inserts.
- Pessimistic locking and optimistic versioning are both workable alternatives, but add complexity (explicit lock scope, or a version column plus retry-on-conflict) without benefit for this shape of problem — one row, one possible owner.

This also drives how it's tested: EF Core's InMemory provider doesn't enforce real unique constraints, so a test running against it would pass even with unsafe, naively-checked code — proving nothing. `BookingConcurrencyTests` instead spins up a real SQL Server via Testcontainers and fires several genuinely concurrent `POST /api/bookings` requests (synchronized to start together, not just `Task.WhenAll`) at the same slot/date through the real HTTP API (`WebApplicationFactory`), asserting exactly one `201 Created` and the rest `409 Conflict`.

## Requirements → implementation → verification

| Requirement | Where in code | How to verify |
|---|---|---|
| Role-based auth (user/admin) | [AuthService.cs](src/BookingSystem.Infrastructure/Identity/AuthService.cs), `[Authorize(Roles = Roles.Admin)]` on mutation endpoints in [ResourcesController.cs](src/BookingSystem.Api/Controllers/ResourcesController.cs) | Register with "Register as admin" checked, call an admin-only endpoint (e.g. create a resource) |
| Concurrency control on booking | [BookingService.cs](src/BookingSystem.Infrastructure/Bookings/BookingService.cs), unique `(SlotId, Date)` index in `ApplicationDbContext.OnModelCreating` | `dotnet test --filter BookingConcurrencyTests` |
| Real-time booking updates | [BookingsHub.cs](src/BookingSystem.Api/Hubs/BookingsHub.cs), [SignalRBookingNotifier.cs](src/BookingSystem.Api/Hubs/SignalRBookingNotifier.cs), [resource-hub.ts](src/BookingSystem.Web/src/app/core/services/resource-hub.ts) | Open the same resource's page in two browser windows, book a slot in one, watch it update live in the other |
| Slot overlap / double-booking guard | [ResourceService.cs](src/BookingSystem.Infrastructure/Resources/ResourceService.cs) | `dotnet test --filter BookingHubTests` / try adding an overlapping slot as admin |
| Runnable, self-contained test suite | [Integration/](tests/BookingSystem.Tests/Integration) (`Testcontainers.MsSql`) | `dotnet test tests/BookingSystem.Tests` with Docker running — no manual DB setup |

## Known trade-offs / simplifications

- **"Register as admin" checkbox** on the registration form lets any user self-assign the admin role. This is a reviewer-convenience shortcut so an admin account can be created without touching the database directly — not something a production system would ever expose.
- **Migrations and role seeding run on every startup**, in every environment, not just Development. In a real deployment this would be pulled out into a separate deploy-time step so that scaling out multiple instances doesn't race to apply the same migration concurrently.
- No password reset / email verification flow — out of scope for this exercise.

## Deployment

Live deployment: https://booking-service-fyc9gdekekg7f9ez.canadacentral-01.azurewebsites.net

Application settings required to reproduce on Azure:

| Setting | Purpose |
|---|---|
| `Jwt__Key` | Signing key for issued JWTs (min 32 bytes) |
| `ConnectionStrings__DefaultConnection` | SQL Server connection string |
| `Azure__SignalR__ConnectionString` | Optional — when set, SignalR runs against Azure SignalR Service instead of the in-process default (see `Program.cs`) |

**Note:** the page may take a while to load (the Azure Web App wakes up from a cold start). If you hit a `503 Service Unavailable` error, this is temporary — wait a bit and reload the page.

**Test admin account:**

| Login | Password |
|---|---|
| `admin1234` | `2423tqetw35tw3t3D` |
