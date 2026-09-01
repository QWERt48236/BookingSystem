# BookingSystem

Booking system with an ASP.NET Core backend and (planned) Angular frontend.

## Architecture

Backend follows Clean Architecture, split into layers with one-way dependencies:

- `src/BookingSystem.Domain` — entities (`Resource`, `Slot`, `Booking`), `Roles` constants. No project dependencies on other layers (NuGet packages are fine, e.g. none currently).
- `src/BookingSystem.Application` — interfaces only: `IAuthService`, `IJwtTokenService`, `IResourceRepository`/`IResourceService`, `IBookingRepository`/`IBookingService`. All return the generic `Result`/`Result<T>` type (`Common/Result.cs`) — a `Succeeded` flag, `ErrorType` (`None`/`Validation`/`NotFound`/`Conflict`/`Unauthorized`/`Failure`), and `Errors`. Depends on `Domain`. No Identity/EF types leak in here.
- `src/BookingSystem.Infrastructure` — `ApplicationDbContext` (EF Core + ASP.NET Core Identity, SQL Server) and its migrations, `ApplicationUser`, `AuthService`/`JwtTokenService`/`ResourceService`/`ResourceRepository`/`BookingService`/`BookingRepository` implementations, role seeding, all DI wiring (`AddInfrastructure`). Depends on `Application` and `Domain`.
- `src/BookingSystem.Api` — ASP.NET Core Web API, composition root. `AuthController`, `ResourcesController`, `BookingsController` — each depends only on its `Application`-layer service interface (`IAuthService`/`IResourceService`/`IBookingService`), never touches `UserManager`/`SignInManager`/`ApplicationUser`/`ApplicationDbContext` directly. `ResultExtensions.ToActionResult` (`Api/Common`) maps a `Result`/`Result<T>` to the right `IActionResult`/status code (`Validation`→400, `NotFound`→404, `Conflict`→409, `Unauthorized`→401). Depends on `Application` and `Infrastructure`.
- `tests/BookingSystem.Tests` — xUnit tests, including real integration tests under `Integration/` (see Testing below). References all of the above.

Solution file: `BookingSystem.slnx`.

### Domain model
`Resource` has many `Slot`s (recurring time-of-day templates, e.g. "Room A, 09:00–10:00" — no date). `Slot` has many `Booking`s, one per distinct `Date` (unique `(SlotId, Date)` index) — the same slot template can be booked on different days but not twice on the same day. All FKs (`Resource→Slot`, `Slot→Booking`, `ApplicationUser→Booking`) use `DeleteBehavior.Restrict`, so deleting a resource/slot/user with existing bookings fails instead of silently cascading away booking history.

### Auth
Custom JWT auth: `POST /api/auth/register` / `POST /api/auth/login` (`AuthController`). Login checks the password via `SignInManager` (inside `AuthService`), then `JwtTokenService` signs a JWT (`NameIdentifier`, `Email`, `Role` claims) with HS256. New users get the `Roles.User` role by default; `Roles.Admin`/`Roles.User` are seeded at startup (`RoleSeeder`). JWT Bearer auth is registered in `AddInfrastructure`, and `Jwt:Key` is fail-fast validated at startup (min 32 bytes) — it lives in user secrets locally / Azure config in other environments, **never** in `appsettings.json`. `[Authorize]` protects reads on `ResourcesController`/`BookingsController`; `[Authorize(Roles = Roles.Admin)]` protects resource/slot mutation endpoints (create/update/delete resource, add slots) on `ResourcesController`.

Frontend (Angular) is not yet added.

### Booking creation pipeline and race-condition guard

`POST /api/bookings` (`BookingsController.Create`, `[Authorize]`) takes a `BookingRequest(int SlotId, DateOnly Date)` and calls `BookingService.CreateAsync(slotId, date, userId)` (`BookingSystem.Infrastructure/Bookings/BookingService.cs`):

1. Rejects a `Date` in the past → `Result<Booking>.Validation(...)` → `400`.
2. Checks the slot exists (`BookingRepository.SlotExistsAsync`) → `Result<Booking>.Validation(...)` → `400` if not.
3. Inserts the `Booking` (`BookingRepository.CreateAsync` → `dbContext.Bookings.Add(...); SaveChangesAsync(...)`).

Step 3 is where the actual race condition is guarded, and it's a **database-level guard, not an application-level check** — there's no "check if a booking already exists, then insert" step, because that check-then-act pattern is exactly what a race would exploit (two concurrent requests could both pass the check before either inserts). Instead, `ApplicationDbContext.OnModelCreating` declares a unique index:
```csharp
builder.Entity<Booking>()
    .HasIndex(b => new { b.SlotId, b.Date })
    .IsUnique();
```
When two requests race to book the same `(SlotId, Date)`, SQL Server itself serializes the two inserts and rejects the second with a real constraint violation. `BookingService.CreateAsync` catches that specific failure and turns it into a normal API response:
```csharp
catch (DbUpdateException ex) when (ex.InnerException is SqlException { Number: 2601 or 2627 })
{
    return Result<Booking>.Conflict("This slot is already booked for the given date.");
}
```
(SQL error `2601` = unique index violation, `2627` = unique constraint violation — EF Core can produce either depending on how the index was created.) `Result.Conflict` maps to HTTP `409` via `ResultExtensions.MapError` in the API layer.

This is why the guard can only be verified against a real SQL Server engine, not EF Core's InMemory provider (doesn't enforce unique indexes) — see `BookingConcurrencyTests` below, which is the test that exercises exactly this path.

## Testing

`tests/BookingSystem.Tests` has two kinds of tests:

- Plain unit tests (xUnit).
- Real integration tests under `tests/BookingSystem.Tests/Integration/` that spin up an actual SQL Server instance via `Testcontainers.MsSql` and drive the real HTTP API via `WebApplicationFactory<Program>`. Example: `BookingConcurrencyTests` fires two truly concurrent `POST /api/bookings` for the same `SlotId`+`Date` and asserts exactly one gets `201 Created` and the other `409 Conflict` — this proves the `(SlotId, Date)` unique-index race guard in `BookingService.CreateAsync` (see Domain model above), which EF Core's InMemory provider can't verify since it doesn't enforce unique indexes.

**Requirements to run them:** Docker must be installed and running (`docker version` should succeed). No manual image pulling is needed — `Testcontainers.MsSql` pulls `mcr.microsoft.com/mssql/server:2022-latest` automatically the first time a test runs; that first pull just takes longer (a couple of minutes). Run with:
```
dotnet test tests/BookingSystem.Tests
```

**`BookingApiFactory`** (`tests/BookingSystem.Tests/Integration/BookingApiFactory.cs`) is the shared `WebApplicationFactory<Program>` + `IAsyncLifetime` fixture other integration test classes should reuse via `IClassFixture<BookingApiFactory>`. Two things about it matter for anyone adding to it:

- It configures the test host **per-instance** via `ConfigureWebHost`/`ConfigureAppConfiguration` (connection string + `Jwt:Key`), not via `Environment.SetEnvironmentVariable`. Process-wide env vars would race the moment a second `IClassFixture<BookingApiFactory>` test class exists, since xUnit runs different test classes in parallel by default.
- `AddInfrastructure` (in `DependencyInjection.cs`) reads `Jwt:Key` **eagerly** — before `Build()` — to construct `JwtBearerOptions`, so the config override above doesn't reach it in time; whatever `Jwt:Key` is already configured on the machine (e.g. real `dotnet user-secrets`) gets used for **token validation**. Meanwhile `JwtTokenService` reads the same setting **lazily** via `IOptions<JwtSettings>` at request time, so it signs tokens with the *overridden* test key. Left alone, this mismatch makes every authenticated request in tests fail with 401. `BookingApiFactory` works around it with a `PostConfigure<JwtBearerOptions>` call that forces the validation key to match the test key. Keep this in mind if `Jwt:Key`-related config plumbing in `DependencyInjection.cs` ever changes.
- `Program.cs` needs `public partial class Program;` at the bottom for `WebApplicationFactory<Program>` to reference it from the separate test project (top-level-statement apps don't expose their generated `Program` class otherwise).

## Git workflow

Never commit without explicit user confirmation — see `.claude/commands/commit.md` for the `/commit` command, which drafts a commit message but never runs `git commit` itself.
