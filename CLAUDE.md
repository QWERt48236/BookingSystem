# BookingSystem

Booking system with an ASP.NET Core backend and (planned) Angular frontend.

## Architecture

Backend follows Clean Architecture, split into layers with one-way dependencies:

- `src/BookingSystem.Domain` — entities (`Resource`, `Slot`, `Booking`), `Roles` constants. No project dependencies on other layers (NuGet packages are fine, e.g. none currently).
- `src/BookingSystem.Application` — interfaces only (`IAuthService`, `IJwtTokenService`) and their plain result types (`AuthResult`). Depends on `Domain`. No Identity/EF types leak in here.
- `src/BookingSystem.Infrastructure` — `ApplicationDbContext` (EF Core + ASP.NET Core Identity, SQL Server), `ApplicationUser`, `AuthService`/`JwtTokenService` implementations, role seeding, all DI wiring (`AddInfrastructure`). Depends on `Application` and `Domain`.
- `src/BookingSystem.Api` — ASP.NET Core Web API, composition root. `AuthController` depends only on `IAuthService` — never touches `UserManager`/`SignInManager`/`ApplicationUser` directly. Depends on `Application` and `Infrastructure`.
- `tests/BookingSystem.Tests` — xUnit tests. References all of the above.

Solution file: `BookingSystem.slnx`.

### Domain model
`Resource` has many `Slot`s (recurring time-of-day templates, e.g. "Room A, 09:00–10:00" — no date). `Slot` has many `Booking`s, one per distinct `Date` (unique `(SlotId, Date)` index) — the same slot template can be booked on different days but not twice on the same day. All FKs (`Resource→Slot`, `Slot→Booking`, `ApplicationUser→Booking`) use `DeleteBehavior.Restrict`, so deleting a resource/slot/user with existing bookings fails instead of silently cascading away booking history.

### Auth
Custom JWT auth: `POST /api/auth/register` / `POST /api/auth/login` (`AuthController`). Login checks the password via `SignInManager` (inside `AuthService`), then `JwtTokenService` signs a JWT (`NameIdentifier`, `Email`, `Role` claims) with HS256. New users get the `Roles.User` role by default; `Roles.Admin`/`Roles.User` are seeded at startup (`RoleSeeder`). JWT Bearer auth is registered in `AddInfrastructure`, and `Jwt:Key` is fail-fast validated at startup (min 32 bytes) — it lives in user secrets locally / Azure config in other environments, **never** in `appsettings.json`. `[Authorize]`/`[Authorize(Roles = Roles.Admin)]` are not yet applied to any endpoints — no resource CRUD controllers exist yet.

Frontend (Angular) is not yet added.

## Git workflow

Never commit without explicit user confirmation — see `.claude/commands/commit.md` for the `/commit` command, which drafts a commit message but never runs `git commit` itself.
