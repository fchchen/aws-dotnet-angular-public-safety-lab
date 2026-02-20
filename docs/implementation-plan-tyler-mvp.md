# Plan: Strengthen Portfolio for Tyler Technologies Lead SE Interview

## Context

Tyler Technologies JD requires .NET Core, AWS cloud-native, relational databases (PostgreSQL), Kubernetes, distributed/HA systems, Docker, CI/CD, automated testing, and security.  
The repo already excels at .NET/AWS/DynamoDB/Docker/CI/CD but has clear gaps in PostgreSQL (required), Kubernetes (strongly preferred), health checks, and authentication. Implementation is phased to maximize interview impact while minimizing regression risk.

---

## Phase 1: Interview MVP

### 1. PostgreSQL via EF Core

Add a third `IIncidentRepository` implementation using EF Core + Npgsql. The existing Strategy pattern makes this a natural extension.

New files:
- `src/PublicSafetyLab.Infrastructure/Persistence/PublicSafetyDbContext.cs` - EF Core DbContext
- `src/PublicSafetyLab.Infrastructure/Persistence/Entities/IncidentEntity.cs`
- `src/PublicSafetyLab.Infrastructure/Persistence/Entities/EvidenceItemEntity.cs`
- `src/PublicSafetyLab.Infrastructure/Persistence/Configurations/IncidentEntityConfiguration.cs` - fluent config with composite indexes on `(tenant_id, status, created_at DESC)` and `(tenant_id, created_at DESC)` matching `ListAsync` filter patterns
- `src/PublicSafetyLab.Infrastructure/Persistence/Configurations/EvidenceItemEntityConfiguration.cs` - FK with cascade delete
- `src/PublicSafetyLab.Infrastructure/Incidents/PostgreSqlIncidentRepository.cs` - `IIncidentRepository` impl; `AsNoTracking()` for reads, `Include()` for eager-loading evidence, upsert pattern matching DynamoDB `PutItem` behavior
- `src/PublicSafetyLab.Infrastructure/Persistence/Migrations/` - generated via `dotnet ef migrations add`

Modified files:
- `src/PublicSafetyLab.Infrastructure/Configuration/AwsResourceOptions.cs` - add `StorageProvider` string (`"InMemory"`/`"DynamoDb"`/`"PostgreSql"`) and `PostgreSqlConnectionString`. Keep `UseAws` as backward-compatible fallback: when `StorageProvider` is not set and `UseAws=true`, default to `"DynamoDb"`
- `src/PublicSafetyLab.Infrastructure/DependencyInjection/ServiceCollectionExtensions.cs` - refactor to `switch (options.StorageProvider)` with three branches. Also register `IncidentService` and `IClock` here (moved from API/Worker `Program.cs`) as `Scoped` to avoid scoped-into-singleton violation with EF Core
- `src/PublicSafetyLab.Infrastructure/PublicSafetyLab.Infrastructure.csproj` - add `Microsoft.EntityFrameworkCore`, `Npgsql.EntityFrameworkCore.PostgreSQL`, `Microsoft.EntityFrameworkCore.Design`
- `src/PublicSafetyLab.Api/Program.cs` - remove `AddSingleton<IncidentService>()` and `AddSingleton<IClock, SystemClock>()` (moved into infrastructure DI)
- `src/PublicSafetyLab.Worker/Program.cs` - remove `AddSingleton<IncidentService>()` and `AddSingleton<IClock, SystemClock>()`
- `src/PublicSafetyLab.Api/appsettings.json` - add `StorageProvider` and `PostgreSqlConnectionString` under `AwsResources`

### 2. Service Lifetime and Worker Scoping Refactor

The Worker currently injects `IncidentService` directly into the constructor (singleton). With scoped EF Core, it must create a scope per message.

Modified files:
- `src/PublicSafetyLab.Worker/Worker.cs` - inject `IServiceScopeFactory` instead of `IncidentService` directly. Per message/iteration, create a scope and resolve `IncidentService` + `IIncidentQueueConsumer` from it:

```csharp
using var scope = _scopeFactory.CreateScope();
var service = scope.ServiceProvider.GetRequiredService<IncidentService>();
```

### 3. Health Checks and Readiness Probes

New files:
- `src/PublicSafetyLab.Infrastructure/HealthChecks/DynamoDbHealthCheck.cs` - calls `DescribeTableAsync`
- `src/PublicSafetyLab.Infrastructure/HealthChecks/SqsHealthCheck.cs` - calls `GetQueueAttributesAsync`
- `src/PublicSafetyLab.Infrastructure/HealthChecks/S3HealthCheck.cs` - calls `ListObjectsV2Async` with `MaxKeys=1`

Modified files:
- `src/PublicSafetyLab.Infrastructure/DependencyInjection/ServiceCollectionExtensions.cs` - add `AddPublicSafetyHealthChecks()` that conditionally registers checks based on `StorageProvider` (PostgreSQL NpgSql check when PostgreSql, DynamoDB/S3/SQS checks when AWS-backed)
- `src/PublicSafetyLab.Api/Program.cs` - map `/healthz/live` (always 200, no dependency checks) and `/healthz/ready` (checks tagged `"ready"` dependencies)

### 4. API Key Authentication

New files:
- `src/PublicSafetyLab.Api/Authentication/ApiKeyAuthenticationHandler.cs` - validates `X-Api-Key` header against configured keys, extracts tenant into claims

Modified files:
- `src/PublicSafetyLab.Api/Program.cs` - register auth scheme, `UseAuthentication()`/`UseAuthorization()`
- `src/PublicSafetyLab.Api/Controllers/IncidentsController.cs` - add `[Authorize]`, read tenant from claims. Compatibility strategy: when `Authentication:AllowLegacyTenantHeader=true` (default), fall back to `X-Tenant-Id` header if no auth is present. Phase 2 disables this fallback.
- `src/PublicSafetyLab.Api/appsettings.json` - add `Authentication:ApiKeys` array and `Authentication:AllowLegacyTenantHeader: true`
- `tests/PublicSafetyLab.Api.Tests/Incidents/IncidentApiTests.cs` - add `X-Api-Key` header to test client
- `web/src/app/core/incident-api.service.ts` - add API key header to HTTP requests

### 5. Local Docker Compose

Keep `docker-compose.ec2.yml` unchanged. Add a separate local dev compose.

New files:
- `docker-compose.local.yml` - PostgreSQL 17, LocalStack (S3+SQS), API, Worker with health-check dependencies
- `infra/localstack/init-aws.sh` - creates S3 bucket and SQS queues on startup

Modified files:
- `src/PublicSafetyLab.Api/Program.cs` - auto-apply EF Core migrations only when `Database:AutoMigrateOnStartup=true` (not tied to environment name)

### 6. Docs and Interview Narrative

Modified files:
- `docs/architecture.md` - add PostgreSQL provider, health check endpoints, auth scheme
- `docs/interview-talk-track.md` - update to reflect implemented capabilities (not "if asked" hypotheticals)
- `docs/runbook.md` - add PostgreSQL setup, provider switching, auth configuration
- `README.md` - add PostgreSQL quick-start, health check endpoints, local docker-compose instructions

## Test Strategy

Tests are written before implementation in each TDD cycle above. Key test files:
- `tests/PublicSafetyLab.Infrastructure.IntegrationTests/Incidents/PostgreSqlIncidentRepositoryTests.cs` - Testcontainers.PostgreSql for real PostgreSQL (Cycle B)
- `tests/PublicSafetyLab.Api.Tests/` - health check + auth endpoint tests (Cycles E, F)

Acceptance criteria (all verified via `dotnet test`):
- `StorageProvider=InMemory` preserves current local workflow (no regressions)
- `StorageProvider=DynamoDb` preserves existing AWS behavior
- PostgreSQL repository passes create/get/list/filter/tenant-isolation tests
- API returns 401 without API key (when strict mode enabled)
- `/healthz/live` always returns 200
- `/healthz/ready` fails when required dependency is unavailable

---

## Phase 2: After MVP

1. Kubernetes manifests - Kustomize base/overlays with deployments, services, HPA, probes wired to `/healthz/*`
2. Structured logging - Serilog with `RenderedCompactJsonFormatter`, correlation ID propagation API -> SQS -> Worker via middleware + `LogContext`
3. HPA/resource tuning guidance docs

---

## Implementation Order (Phase 1) - TDD Cycles

Per `docs/tdd-guidelines.md`: every production change starts with a failing test and ends with all tests green.

### Cycle A: Foundation (no tests needed - config/plumbing only)

1. Add NuGet packages to Infrastructure `.csproj` (EF Core, Npgsql)
2. Add `StorageProvider` + `PostgreSqlConnectionString` to `AwsResourceOptions`
3. Add config values to `appsettings.json`

### Cycle B: PostgreSQL Repository (Red -> Green -> Refactor)

1. RED: Create `PostgreSqlIncidentRepositoryTests.cs` with Testcontainers. Write tests for:
- `SaveAsync` + `GetAsync` round-trip
- `ListAsync` with status filter
- `ListAsync` with date range filter
- Evidence persistence and cascade delete
- Tenant isolation (tenant A cannot read tenant B data)
- All tests fail (no implementation exists)
2. GREEN: Implement the minimum to pass:
- Create `IncidentEntity`, `EvidenceItemEntity` (EF entities)
- Create `IncidentEntityConfiguration`, `EvidenceItemEntityConfiguration` (fluent config)
- Create `PublicSafetyDbContext`
- Create `PostgreSqlIncidentRepository` implementing `IIncidentRepository`
- Generate EF Core migration
- Tests go green
3. REFACTOR: Extract shared snapshot<->entity mapping if needed, clean up

### Cycle C: DI and Lifetime Refactor (Red -> Green -> Refactor)

1. RED: Add test in API tests asserting the app starts and responds with `StorageProvider=PostgreSql` config (`WebApplicationFactory`). Fails because DI does not know about the new provider yet.
2. GREEN: Refactor `ServiceCollectionExtensions` to three-way switch. Move `IncidentService` + `IClock` registration into infrastructure DI as scoped. Remove singleton registrations from `Api/Program.cs` and `Worker/Program.cs`. Test passes.
3. REFACTOR: Verify existing tests still pass with `StorageProvider=InMemory` (regression check). Clean up.

### Cycle D: Worker Scoping (Red -> Green -> Refactor)

1. RED: Existing Worker tests or a new test that resolves scoped `IncidentService` in a Worker-like scope fails with scoped-into-singleton error.
2. GREEN: Refactor `Worker.cs` to use `IServiceScopeFactory`, create scope per message iteration.
3. REFACTOR: Verify all existing tests still green.

### Cycle E: Health Checks (Red -> Green -> Refactor)

1. RED: Add API integration test hitting `/healthz/live` -> returns 404 (endpoint does not exist yet). Add test hitting `/healthz/ready` -> also 404.
2. GREEN: Map health check endpoints in `Program.cs`. Create `DynamoDbHealthCheck`, `SqsHealthCheck`, `S3HealthCheck`. Register via `AddPublicSafetyHealthChecks()`. Tests pass.
3. REFACTOR: Clean up, verify `/healthz/live` always returns 200 regardless of dependencies.

### Cycle F: API Key Authentication (Red -> Green -> Refactor)

1. RED: Add API test asserting `POST /api/v1/incidents` without `X-Api-Key` returns 401. Fails (currently returns 201, no auth required).
2. GREEN: Create `ApiKeyAuthenticationHandler`. Register auth in `Program.cs`. Add `[Authorize]` to controller. Add legacy tenant header fallback behind `AllowLegacyTenantHeader` flag. Test passes.
3. RED: Add test asserting valid API key + `X-Tenant-Id` returns 201 with correct tenant in response. Fails until claim extraction is wired.
4. GREEN: Wire tenant claim extraction in controller. Update existing API tests to pass `X-Api-Key` header. All tests green.
5. REFACTOR: Clean up auth handler, verify legacy fallback mode works.

### Cycle G: Integration (no new tests - wiring)

1. Create `docker-compose.local.yml` + `infra/localstack/init-aws.sh`
2. Add `Database:AutoMigrateOnStartup` flag + migration logic in `Program.cs`
3. Verify end-to-end: `docker compose -f docker-compose.local.yml up`

### Cycle H: Docs

1. Update `docs/architecture.md`, `docs/interview-talk-track.md`, `docs/runbook.md`, `README.md`
2. Update `web/src/app/core/incident-api.service.ts` with API key header

## Verification

- `dotnet build PublicSafetyLab.sln` compiles
- `dotnet test PublicSafetyLab.sln` all tests pass
- Run with `StorageProvider=InMemory` - existing behavior preserved
- `docker compose -f docker-compose.local.yml up` - PostgreSQL + LocalStack flow works
- `curl /healthz/live` returns 200; `curl /healthz/ready` returns 200 with deps up, 503 with deps down
- API returns 401 without `X-Api-Key`, 200 with valid key
