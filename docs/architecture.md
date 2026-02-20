# Architecture

## Context
This system models incident intake and asynchronous processing for public-safety operations.

## Components
- Angular app (`web/`)
  - Incident dashboard (create + list)
  - Incident detail (generate S3 upload URL, queue processing)
- API (`PublicSafetyLab.Api`)
  - `POST /api/v1/incidents`
  - `GET /api/v1/incidents`
  - `GET /api/v1/incidents/{id}`
  - `POST /api/v1/incidents/{id}/evidence/upload-url`
  - `POST /api/v1/incidents/{id}/process`
  - `GET /healthz/live`
  - `GET /healthz/ready`
- Worker (`PublicSafetyLab.Worker`)
  - Polls SQS
  - Calls application service to mark incidents `Processed` or `Failed`
- Storage/Messaging
  - Incident store can be switched by config:
    - InMemory
    - DynamoDB
    - PostgreSQL (EF Core)
  - S3 for evidence object storage (or local evidence adapter)
  - SQS for async work dispatch (or in-memory queue adapter)

## Persistence Model
### DynamoDB
- Table: `incident_items`
- PK: `PK` = `tenantId`
- SK: `SK` = `INCIDENT#{incidentId}`
- Item payload stores serialized `IncidentSnapshot`

### PostgreSQL
- `incidents` table keyed by `id`
- `evidence_items` child table with FK cascade delete to `incidents`
- Composite indexes:
  - `(tenant_id, status, created_at DESC)`
  - `(tenant_id, created_at DESC)`

## Reliability Patterns
- Queue + worker decouples synchronous API from processing
- DLQ configured for poison messages
- Idempotent-style state transitions in domain model
- Problem-details error responses in API middleware
- Health checks:
  - `/healthz/live` returns process liveness
  - `/healthz/ready` checks dependency readiness by provider
- Structured logging:
  - Serilog JSON output in API and worker
  - Correlation IDs flow from API header (`X-Correlation-Id`) into queue messages and worker logs

## Security Model
- API key authentication via `X-Api-Key`
- API keys map to tenant claims (`tenant_id`)
- Legacy `X-Tenant-Id` fallback can be kept temporarily via `Authentication:AllowLegacyTenantHeader=true`
- IAM role on EC2 with least-privilege access to table/bucket/queue
- S3 upload via pre-signed URL generation

## Scaling Path
- Current: single EC2 + Docker Compose (free-tier focused)
- Kubernetes manifests available under `k8s/` with Kustomize base + dev/prod overlays, probes, and API HPA
- Kubernetes base uses non-root pod/container security contexts and placeholder-only secrets for safer defaults
- Next steps:
  - Run on EKS with managed ingress/secret management
  - ALB + multi-AZ
  - auth hardening (Cognito/OIDC)
  - contract + E2E test expansion
