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
- Worker (`PublicSafetyLab.Worker`)
  - Polls SQS
  - Calls application service to mark incidents `Processed` or `Failed`
- Storage/Messaging
  - DynamoDB table for incident state
  - S3 for evidence object storage
  - SQS for async work dispatch

## Data Model
- Table: `incident_items`
- PK: `PK` = `tenantId`
- SK: `SK` = `INCIDENT#{incidentId}`
- GSI: `StatusCreatedAtIndex` (`Status`, `CreatedAt`)
- Item payload stores serialized `IncidentSnapshot`

## Reliability Patterns
- Queue + worker decouples synchronous API from processing
- DLQ configured for poison messages
- Idempotent-style state transitions in domain model
- Problem-details error responses in API middleware

## Security Model
- Tenant scoping via `X-Tenant-Id` header (demo model)
- IAM role on EC2 with least-privilege access to table/bucket/queue
- S3 upload via pre-signed URL generation

## Scaling Path
- Current: single EC2 + Docker Compose (free-tier focused)
- Next steps:
  - ECS/Fargate or EKS
  - ALB + multi-AZ
  - auth hardening (Cognito/OIDC)
  - contract + E2E test expansion
