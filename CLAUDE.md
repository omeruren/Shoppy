# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What this is

Shoppy is an ASP.NET Core 10 e-commerce API: minimal-API endpoints (via Carter), JWT auth with refresh tokens and role/permission-based authorization, EF Core/SQL Server persistence, and the usual cross-cutting stack (Serilog, OpenTelemetry, rate limiting, API versioning).

## Solution layout

The actual solution is `src/Shoppy/Shoppy.slnx`. It references projects both under `src/Shoppy/` and under `test/` at the repo root:

```
Shoppy.Entity       → domain entities, BaseEntity (no project references)
Shoppy.DataAccess    → EF Core DbContext, entity configurations, migrations (→ Entity)
Shoppy.Business      → services, DTOs, FluentValidation validators, auth/JWT (→ DataAccess)
Shoppy.WebAPI        → Carter modules (minimal API endpoints), Program.cs composition root (→ Business)
test/Shoppy.UnitTests        → service/validator unit tests (NSubstitute, FluentAssertions, EF InMemory)
test/Shoppy.IntegrationTests → WebApplicationFactory + Testcontainers.MsSql tests
```

Dependency direction is `Business → DataAccess → Entity` (business services take a direct dependency on `ApplicationDbContext`/`DbSet<T>` — there is no repository abstraction).

`src/Shoppy/Shoppy.Tests/` is a leftover project **not referenced by the `.slnx`** — it's not part of the build; use `test/Shoppy.UnitTests` and `test/Shoppy.IntegrationTests` for real work.

## Commands

Run from `src/Shoppy/` (where `Shoppy.slnx` lives) unless noted otherwise.

```
dotnet build Shoppy.slnx                              # build everything
dotnet run --project Shoppy.WebAPI                    # run the API (Scalar UI at /scalar, OpenAPI at /openapi)

dotnet test ../../test/Shoppy.UnitTests               # unit tests
dotnet test ../../test/Shoppy.IntegrationTests        # integration tests (needs Docker for Testcontainers.MsSql)
dotnet test ../../test/Shoppy.UnitTests --filter "FullyQualifiedName~ProductServiceTests"   # single test class
dotnet test ../../test/Shoppy.UnitTests --filter "DisplayName~CreateAsync_Should_Return_Conflict"  # single test

dotnet ef migrations add <Name> --project Shoppy.DataAccess --startup-project Shoppy.WebAPI
dotnet ef database update --project Shoppy.DataAccess --startup-project Shoppy.WebAPI
```

Integration tests use Testcontainers, so Docker must be running locally / in CI.

## Architecture notes

**Endpoints (`Shoppy.WebAPI/Modules/*Module.cs`)**: one `ICarterModule` per resource. Each module builds its own `ApiVersionSet`, groups routes under `/api/v{version}/<resource>`, and applies rate limiting + `RequireAuthorization()` at the group level. Mutating endpoints attach `FluentValidationFilter<TDto>` as an endpoint filter rather than validating inside the handler.

**Result pattern**: business methods return `Shoppy.Business.BaseResult.Result<T>` (`IsSuccessful`, `StatusCode`, `ErrorMessages`, `Data`) instead of throwing for expected failures (not found, conflict, etc.). Modules map this to HTTP responses inline — mapping is currently inconsistent across modules (some return `Results.Problem`-style bodies, others `Results.StatusCode` with no body), so check sibling modules for the locally-expected convention before adding a new endpoint rather than assuming one true pattern.

**Services (`Shoppy.Business/<Resource>/`)**: each resource folder holds the service, its interface, `DataTransferObjects/`, and `Validators/`. Services inject `ApplicationDbContext` directly (no repository/unit-of-work layer) and use `Mapster` for entity↔DTO projection.

**List caching**: `ProductService`, `OrderService`, `OrderItemService`, and `CategoryService` each cache paginated `GetAllAsync` results in `IMemoryCache`, invalidated via a `static CancellationTokenSource` swapped with `Interlocked.Exchange` on writes. This pattern is duplicated per-service (not extracted into a shared abstraction) and the token is `static`, i.e. shared across all DI scopes/requests for that service type — be aware of this when touching cache invalidation or writing tests that run in parallel.

**Entities**: everything derives from `Shoppy.Entity.Abstraction.BaseEntity` (`Guid` v7 id, audit fields `CreatedAt/By`, `UpdatedAt/By`, soft-delete via `DeletedAt/By` + `IsDeleted`, and a `RowVersion` `[Timestamp]` column for optimistic concurrency). Soft delete is enforced via global EF Core query filters in `*Configuration.cs` classes, not by services filtering manually. Not all entities carry domain behavior — `User` has methods like `Create()`/`GeneratePasswordResetCode()`, but `Order`/`Product`/`Category`/`OrderItem` are plain POCOs mutated directly by services.

**Auth & permissions**: `IAuthService`/`AuthService` live together in `IAuthService.cs`. JWTs are issued by `JwtProvider` and carry permission claims. `Shoppy.Business.Permissions.Permissions` is the single source of truth for permission strings (`"<Group>.<Action>"`); `PermissionAuthorizationHandler`/`PermissionRequirement` enforce them, and `RolePermission` (EF configuration in `RolePermissionConfiguration.cs`) maps roles to permissions in the database. When adding a new protected capability, add the constant in `Permissions`, wire it into role seeding, and require it from the relevant endpoint/policy — don't hardcode role names in new authorization checks.

**Migrations**: EF Core migrations live in `Shoppy.DataAccess/Migrations/`. Generate them with the `dotnet ef` commands above, always specifying `--startup-project Shoppy.WebAPI` (that's where the connection string and DI composition live).

## Known sharp edges (don't be surprised)

- `appsettings.json` currently contains real-looking JWT/SMTP secrets and a local SQL Server connection string committed to the repo — treat these as dev-only placeholders, not values to propagate, and prefer `dotnet user-secrets`/environment variables for anything new.
- `SearchTerm` exists on `PaginationRequestDto` and is folded into cache keys, but no service currently filters by it — don't assume search is implemented just because the field is threaded through.
- CORS origins in `Program.cs` are hardcoded (`localhost:3000/5176/5226`) rather than configuration-driven.
