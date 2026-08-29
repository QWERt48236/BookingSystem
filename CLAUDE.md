# BookingSystem

Booking system with an ASP.NET Core backend and (planned) Angular frontend.

## Architecture

Backend follows Clean Architecture, split into layers with one-way dependencies:

- `src/BookingSystem.Domain` — entities, value objects, domain logic. No dependencies on other layers.
- `src/BookingSystem.Application` — use cases, interfaces, business rules. Depends on `Domain`.
- `src/BookingSystem.Infrastructure` — implementations (DB, external services, etc.). Depends on `Application` and `Domain`.
- `src/BookingSystem.Api` — ASP.NET Core Web API, composition root. Depends on `Application` and `Infrastructure`.
- `tests/BookingSystem.Tests` — xUnit tests. References all of the above.

Solution file: `BookingSystem.slnx`.

Frontend (Angular) is not yet added.

## Git workflow

Never commit without explicit user confirmation — see `.claude/commands/commit.md` for the `/commit` command, which drafts a commit message but never runs `git commit` itself.
